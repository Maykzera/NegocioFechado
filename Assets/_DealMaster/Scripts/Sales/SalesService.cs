using System;
using System.Collections.Generic;

/// <summary>Uma venda concluída.</summary>
public sealed class SaleRecord
{
    public int SaleId;
    public int Day;
    public string CustomerName;
    public string ProductId;
    public long PriceCents;
    public long FeeCents;
    public long ProductCostCents;
    public long MaterialsCostCents;
    public bool Packed;
    public float PackagingScore;
    public float DamageRisk;
    public bool Damaged;
    public bool ComplaintResolved;
    public long RefundedCents;

    /// <summary>Lucro direto da venda (preço − taxa − custo do produto − embalagem − reembolsos).</summary>
    public long ProfitCents => PriceCents - FeeCents - ProductCostCents - MaterialsCostCents - RefundedCents;
}

public enum ComplaintResolution
{
    FullRefund,
    PartialRefund,
    Deny
}

/// <summary>
/// Conclui vendas acordadas na negociação: tira o pedido do estoque, recebe o pagamento,
/// faz a entrega (com risco de avaria vindo da embalagem) e cuida do pós-venda.
/// É o único caminho pelo qual uma venda mexe em dinheiro e estoque.
/// </summary>
public sealed class SalesService
{
    /// <summary>Taxa da maquininha (2%).</summary>
    public const float PaymentFeeRate = 0.02f;

    private readonly IWallet wallet;
    private readonly InventoryService inventory;
    private readonly PackagingService packaging;
    private readonly FinanceLedger ledger;
    private readonly List<SaleRecord> sales = new List<SaleRecord>();
    private int nextSaleId = 1;

    /// <summary>Reputação do negócio (0 a 100).</summary>
    public float Reputation { get; private set; } = 50f;

    public IReadOnlyList<SaleRecord> Sales => sales;

    public Func<int> DayProvider;

    public event Action<SaleRecord> OnSaleCompleted;
    public event Action<SaleRecord> OnComplaint;

    public SalesService(IWallet wallet, InventoryService inventory, PackagingService packaging, FinanceLedger ledger)
    {
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.packaging = packaging ?? throw new ArgumentNullException(nameof(packaging));
        this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    public static long FeeFor(long priceCents) => (long)Math.Round(priceCents * PaymentFeeRate);

    /// <param name="deliveryRoll">Número de 0 a 1 que decide a avaria (vem de um gerador com semente).</param>
    public bool TryCompleteSale(string customerName, string productId, long priceCents, bool packed,
        double deliveryRoll, out SaleRecord sale, out string error)
    {
        sale = null;
        var product = ProductDatabase.Get(productId);
        if (product == null || priceCents <= 0) { error = "Venda inválida."; return false; }

        PackedOrder order = null;
        long productCost;
        long removedCost;

        if (packed)
        {
            order = packaging.FirstFor(productId);
            if (order == null || inventory.GetAvailable(StorageLocation.Order, productId) < 1)
            { error = "Você não tem esse pedido embalado."; return false; }
            if (!inventory.TryRemove(StorageLocation.Order, productId, 1, out removedCost, "Venda"))
            { error = "Não foi possível retirar o pedido."; return false; }
            packaging.Remove(order);
            productCost = order.ProductCostCents;
        }
        else
        {
            if (!inventory.TryRemove(StorageLocation.Backpack, productId, 1, out removedCost, "Venda sem embalagem"))
            { error = "Você não está com esse produto."; return false; }
            productCost = removedCost;
        }

        long fee = FeeFor(priceCents);
        if (!wallet.TryCredit(priceCents - fee))
        {
            // Devolve o item (só falha em estouro numérico).
            inventory.TryAdd(packed ? StorageLocation.Order : StorageLocation.Backpack, productId, 1, removedCost, "Estorno");
            if (order != null) packaging.Restore(new List<PackedOrder>(packaging.Orders) { order }, packaging.NextOrderNumber);
            error = "Não foi possível receber o pagamento.";
            return false;
        }

        float risk = packed ? order.Score.DamageRisk : Math.Min(0.9f, product.Fragility * 0.6f + 0.1f);
        sale = new SaleRecord
        {
            SaleId = nextSaleId++,
            Day = DayProvider?.Invoke() ?? 1,
            CustomerName = customerName,
            ProductId = productId,
            PriceCents = priceCents,
            FeeCents = fee,
            ProductCostCents = productCost,
            MaterialsCostCents = packed ? order.Score.MaterialsCostCents : 0,
            Packed = packed,
            PackagingScore = packed ? order.Score.Overall : 0f,
            DamageRisk = risk,
            Damaged = deliveryRoll < risk
        };
        sales.Add(sale);

        ledger.Record(LedgerEntryType.SaleRevenue, priceCents, $"Venda #{sale.SaleId}: {product.Name} para {customerName}");
        ledger.Record(LedgerEntryType.PaymentFee, -fee, $"Taxa da venda #{sale.SaleId}");

        if (sale.Damaged)
        {
            Reputation = Clamp(Reputation - 4f);
            OnComplaint?.Invoke(sale);
        }
        else
        {
            Reputation = Clamp(Reputation + (packed ? 2f : 1f));
        }

        OnSaleCompleted?.Invoke(sale);
        error = null;
        return true;
    }

    public IEnumerable<SaleRecord> PendingComplaints()
    {
        foreach (var s in sales) if (s.Damaged && !s.ComplaintResolved) yield return s;
    }

    /// <summary>Resolve a reclamação de um produto avariado.</summary>
    public bool ResolveComplaint(SaleRecord sale, ComplaintResolution resolution, out string error)
    {
        if (sale == null || !sale.Damaged || sale.ComplaintResolved) { error = "Nada a resolver."; return false; }

        long refund = resolution == ComplaintResolution.FullRefund ? sale.PriceCents
            : resolution == ComplaintResolution.PartialRefund ? sale.PriceCents / 2
            : 0;

        if (refund > 0 && !wallet.TryDebit(refund)) { error = "Saldo insuficiente para o reembolso."; return false; }

        sale.RefundedCents = refund;
        sale.ComplaintResolved = true;
        if (refund > 0) ledger.Record(LedgerEntryType.Refund, -refund, $"Reembolso da venda #{sale.SaleId}");

        Reputation = Clamp(Reputation + (resolution == ComplaintResolution.FullRefund ? 3f
            : resolution == ComplaintResolution.PartialRefund ? 0.5f : -8f));
        error = null;
        return true;
    }

    public void Restore(IEnumerable<SaleRecord> saved, float reputation)
    {
        sales.Clear();
        sales.AddRange(saved);
        nextSaleId = 1;
        foreach (var s in sales) nextSaleId = Math.Max(nextSaleId, s.SaleId + 1);
        Reputation = Clamp(reputation);
    }

    private static float Clamp(float v) => v < 0f ? 0f : v > 100f ? 100f : v;
}
