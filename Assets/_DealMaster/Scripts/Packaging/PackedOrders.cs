using System;
using System.Collections.Generic;

/// <summary>Um pedido embalado, pronto para venda e entrega.</summary>
public sealed class PackedOrder
{
    public string OrderId { get; }
    public string ProductId { get; }
    public long ProductCostCents { get; }
    public PackagingScore Score { get; }

    public long TotalCostCents => ProductCostCents + Score.MaterialsCostCents;

    public PackedOrder(string orderId, string productId, long productCostCents, PackagingScore score)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductCostCents = productCostCents;
        Score = score;
    }
}

/// <summary>
/// Conclui embalagens: cobra os materiais, move a unidade da bancada para "pedidos prontos"
/// e registra a qualidade da embalagem. Tudo ou nada.
/// </summary>
public sealed class PackagingService
{
    private readonly IWallet wallet;
    private readonly InventoryService inventory;
    private readonly List<PackedOrder> orders = new List<PackedOrder>();
    private int nextId = 1;

    public IReadOnlyList<PackedOrder> Orders => orders;

    public event Action<PackedOrder> OnPacked;

    public PackagingService(IWallet wallet, InventoryService inventory)
    {
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    public bool TryFinalize(PackagingInput input, out PackedOrder order, out string error)
    {
        order = null;
        var score = PackagingEvaluator.Evaluate(input);
        if (!score.Valid) { error = score.Problem; return false; }

        string productId = input.Product.Id;
        if (inventory.GetAvailable(StorageLocation.PackingBench, productId) < 1)
        { error = "O produto não está na bancada."; return false; }
        if (!inventory.CanAdd(StorageLocation.Order, 1))
        { error = "Limite de pedidos prontos atingido."; return false; }
        if (score.MaterialsCostCents > wallet.BalanceCents)
        { error = "Dinheiro insuficiente para os materiais."; return false; }

        long unitCost = inventory.GetStack(StorageLocation.PackingBench, productId).AverageUnitCostCents;

        if (!wallet.TryDebit(score.MaterialsCostCents))
        { error = "Dinheiro insuficiente para os materiais."; return false; }

        if (!inventory.TryTransfer(StorageLocation.PackingBench, StorageLocation.Order, productId, 1, "Embalado"))
        {
            wallet.TryCredit(score.MaterialsCostCents);
            error = "Não foi possível mover o pedido.";
            return false;
        }

        order = new PackedOrder($"PED-{nextId++:000}", productId, unitCost, score);
        orders.Add(order);
        OnPacked?.Invoke(order);
        error = null;
        return true;
    }

    /// <summary>Remove um pedido (vendido). Não mexe no inventário: o SalesService faz isso.</summary>
    public bool Remove(PackedOrder order) => orders.Remove(order);

    public PackedOrder FirstFor(string productId) => orders.Find(o => o.ProductId == productId);

    /// <summary>Restaura pedidos de um jogo salvo.</summary>
    public void Restore(IEnumerable<PackedOrder> saved, int nextOrderNumber)
    {
        orders.Clear();
        orders.AddRange(saved);
        nextId = Math.Max(1, nextOrderNumber);
    }

    public int NextOrderNumber => nextId;
}
