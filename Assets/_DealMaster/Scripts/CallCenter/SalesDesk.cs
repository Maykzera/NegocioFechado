using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Venda registrada no CRM.</summary>
[Serializable]
public sealed class SaleRecord
{
    public int SaleId;
    public int Day;
    public string CallId;
    public string LeadName;
    public string ServiceId;
    public long PriceCents;
    public long CommissionCents;
    public bool ChargedBack;
    public string ChargebackReason;
}

/// <summary>
/// Registro de vendas no CRM: só aceita venda que o cliente realmente aceitou na ligação,
/// pelo preço combinado e dentro do desconto autorizado. Paga a comissão na hora.
/// </summary>
public sealed class SalesDesk
{
    private readonly IWallet wallet;
    private readonly FinanceLedger ledger;
    private readonly List<SaleRecord> sales = new List<SaleRecord>();
    private int nextId = 1;

    public IReadOnlyList<SaleRecord> Sales => sales;
    public Func<int> DayProvider;

    public event Action<SaleRecord> OnSale;
    public event Action<SaleRecord> OnChargeback;

    public SalesDesk(IWallet wallet, FinanceLedger ledger)
    {
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
    }

    public static int QuotaFor(int level) => 2 + (level - 1) / 2;

    public int SalesOnDay(int day) => sales.Count(s => s.Day == day && !s.ChargedBack);

    public bool TryRegister(CallSession call, string serviceId, long priceCents, int playerLevel,
        out SaleRecord sale, out string error)
    {
        sale = null;
        var service = ServiceCatalog.Get(serviceId);
        if (call == null || service == null) { error = "Escolha o serviço vendido."; return false; }
        if (call.SaleRegistered) { error = "Esta venda já foi registrada."; return false; }
        if (service.RequiredLevel > playerLevel) { error = $"Você só pode vender {service.Name} a partir do nível {service.RequiredLevel}."; return false; }

        var agreement = call.Agreement;
        if (agreement == null) { error = "O cliente ainda não aceitou comprar. Só registre depois que ele disser sim."; return false; }
        if (agreement.AcceptedServiceId != null && agreement.AcceptedServiceId != serviceId)
        {
            error = $"O cliente aceitou {ServiceCatalog.Get(agreement.AcceptedServiceId)?.Name ?? "outro serviço"}, não {service.Name}.";
            return false;
        }
        if (agreement.AcceptedPriceCents.HasValue && Math.Abs(agreement.AcceptedPriceCents.Value - priceCents) > 100)
        {
            error = $"O valor combinado na ligação foi {Money.Format(agreement.AcceptedPriceCents.Value)}.";
            return false;
        }
        if (priceCents < service.MinPriceCents)
        {
            error = $"Abaixo do mínimo autorizado ({Money.Format(service.MinPriceCents)}). Você não pode vender por esse valor.";
            return false;
        }
        if (priceCents > service.MonthlyPriceCents)
        {
            error = $"Acima do preço de tabela ({Money.Format(service.MonthlyPriceCents)}).";
            return false;
        }

        long commission = service.CommissionFor(priceCents);
        if (!wallet.TryCredit(commission)) { error = "Não foi possível creditar a comissão."; return false; }

        sale = new SaleRecord
        {
            SaleId = nextId++,
            Day = DayProvider?.Invoke() ?? call.Day,
            CallId = call.CallId,
            LeadName = call.Lead.Name,
            ServiceId = serviceId,
            PriceCents = priceCents,
            CommissionCents = commission
        };
        sales.Add(sale);
        call.MarkSale();
        ledger.Record(LedgerEntryType.Commission, commission, $"Comissão: {service.Name} para {call.Lead.Name}");
        OnSale?.Invoke(sale);
        error = null;
        return true;
    }

    /// <summary>Cancela a venda (ex.: o supervisor descobriu uma mentira) e estorna a comissão.</summary>
    public long Chargeback(SaleRecord sale, string reason)
    {
        if (sale == null || sale.ChargedBack) return 0;
        sale.ChargedBack = true;
        sale.ChargebackReason = reason;
        long taken = Math.Min(sale.CommissionCents, wallet.BalanceCents);
        if (taken > 0) wallet.TryDebit(taken);
        ledger.Record(LedgerEntryType.Chargeback, -taken, $"Estorno da venda #{sale.SaleId}: {reason}");
        OnChargeback?.Invoke(sale);
        return taken;
    }

    public SaleRecord ForCall(string callId) => sales.FirstOrDefault(s => s.CallId == callId);

    public void Restore(IEnumerable<SaleRecord> saved)
    {
        sales.Clear();
        sales.AddRange(saved);
        nextId = sales.Count == 0 ? 1 : sales.Max(s => s.SaleId) + 1;
    }
}
