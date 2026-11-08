using System;

public enum PurchaseResult
{
    Success,
    InvalidRequest,
    SupplierClosed,
    ProductNotSold,
    LevelTooLow,
    OutOfStock,
    PriceBelowFloor,
    NoSpace,
    InsufficientFunds
}

public enum SupplierOfferResult
{
    Accepted,
    CounterOffer,
    Rejected
}

/// <summary>Resposta do fornecedor a uma proposta de preço.</summary>
public readonly struct SupplierOfferResponse
{
    public readonly SupplierOfferResult Result;

    /// <summary>Preço unitário aceito (Accepted) ou contraproposta (CounterOffer).</summary>
    public readonly long UnitPriceCents;

    public SupplierOfferResponse(SupplierOfferResult result, long unitPriceCents)
    {
        Result = result;
        UnitPriceCents = unitPriceCents;
    }
}

/// <summary>Registro de uma compra concluída, para histórico financeiro e para o mestre.</summary>
public readonly struct PurchaseReceipt
{
    public readonly string SupplierId;
    public readonly string ProductId;
    public readonly int Quantity;
    public readonly long UnitPriceCents;
    public readonly long TotalCents;
    public readonly StorageLocation Destination;

    public PurchaseReceipt(
        string supplierId, string productId, int quantity,
        long unitPriceCents, long totalCents, StorageLocation destination)
    {
        SupplierId = supplierId;
        ProductId = productId;
        Quantity = quantity;
        UnitPriceCents = unitPriceCents;
        TotalCents = totalCents;
        Destination = destination;
    }
}

/// <summary>
/// Valida e executa compras em fornecedores.
/// Dinheiro, estoque do fornecedor e inventário do jogador mudam juntos ou não mudam.
/// </summary>
public sealed class PurchaseService
{
    private readonly IWallet wallet;
    private readonly InventoryService inventory;

    public event Action<PurchaseReceipt> OnPurchaseCompleted;

    public PurchaseService(IWallet wallet, InventoryService inventory)
    {
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
    }

    /// <summary>
    /// Avalia uma proposta de preço unitário feita pelo jogador ao fornecedor.
    /// Determinística: a mesma proposta no mesmo estado gera sempre a mesma resposta.
    /// </summary>
    public SupplierOfferResponse EvaluateOffer(
        SupplierStock supplier, string productId, int quantity, long offeredUnitPriceCents)
    {
        if (supplier == null || !supplier.Sells(productId) || quantity <= 0 || offeredUnitPriceCents <= 0)
            return new SupplierOfferResponse(SupplierOfferResult.Rejected, 0);

        long asking = supplier.GetVolumePriceCents(productId, quantity);
        long floor = supplier.GetFloorPriceCents(productId, quantity);

        if (offeredUnitPriceCents >= asking)
            return new SupplierOfferResponse(SupplierOfferResult.Accepted, asking);

        if (offeredUnitPriceCents >= floor)
            return new SupplierOfferResponse(SupplierOfferResult.Accepted, offeredUnitPriceCents);

        // Propostas abaixo de 70% do mínimo são consideradas ofensivas e recusadas.
        if (offeredUnitPriceCents < floor * 0.7)
            return new SupplierOfferResponse(SupplierOfferResult.Rejected, 0);

        // Contraproposta: meio caminho entre o mínimo e o preço pedido.
        long counter = floor + (asking - floor) / 2;
        return new SupplierOfferResponse(SupplierOfferResult.CounterOffer, counter);
    }

    /// <summary>Confere se a compra seria aceita, sem alterar nada.</summary>
    public PurchaseResult Validate(
        SupplierStock supplier,
        string productId,
        int quantity,
        long unitPriceCents,
        int playerLevel,
        int currentHour,
        StorageLocation destination,
        out long totalCents)
    {
        totalCents = 0;

        if (supplier == null || string.IsNullOrEmpty(productId) || quantity <= 0 || unitPriceCents <= 0)
            return PurchaseResult.InvalidRequest;
        if (destination == StorageLocation.Order)
            return PurchaseResult.InvalidRequest;
        if (!supplier.IsOpen(currentHour))
            return PurchaseResult.SupplierClosed;
        if (!supplier.Sells(productId))
            return PurchaseResult.ProductNotSold;
        if (playerLevel < supplier.GetRequiredLevel(productId))
            return PurchaseResult.LevelTooLow;
        if (quantity > supplier.GetStock(productId))
            return PurchaseResult.OutOfStock;
        if (unitPriceCents < supplier.GetFloorPriceCents(productId, quantity))
            return PurchaseResult.PriceBelowFloor;
        if (!inventory.CanAdd(destination, quantity))
            return PurchaseResult.NoSpace;

        try
        {
            totalCents = checked(unitPriceCents * quantity);
        }
        catch (OverflowException)
        {
            return PurchaseResult.InvalidRequest;
        }

        if (totalCents > wallet.BalanceCents)
            return PurchaseResult.InsufficientFunds;

        return PurchaseResult.Success;
    }

    /// <summary>Executa a compra pelo preço unitário combinado.</summary>
    public PurchaseResult TryPurchase(
        SupplierStock supplier,
        string productId,
        int quantity,
        long unitPriceCents,
        int playerLevel,
        int currentHour,
        StorageLocation destination,
        out PurchaseReceipt receipt)
    {
        receipt = default;

        var validation = Validate(supplier, productId, quantity, unitPriceCents,
            playerLevel, currentHour, destination, out long total);
        if (validation != PurchaseResult.Success)
            return validation;

        if (!wallet.TryDebit(total))
            return PurchaseResult.InsufficientFunds;

        if (!inventory.TryAdd(destination, productId, quantity, unitPriceCents,
                $"Compra em {supplier.SupplierId}"))
        {
            wallet.TryCredit(total); // desfaz o débito
            return PurchaseResult.NoSpace;
        }

        if (!supplier.TryTakeStock(productId, quantity))
        {
            inventory.TryRemove(destination, productId, quantity, out _, "Estorno de compra");
            wallet.TryCredit(total);
            return PurchaseResult.OutOfStock;
        }

        supplier.ImproveRelationship(0.01f * Math.Min(quantity, 10));

        receipt = new PurchaseReceipt(supplier.SupplierId, productId, quantity,
            unitPriceCents, total, destination);
        OnPurchaseCompleted?.Invoke(receipt);
        return PurchaseResult.Success;
    }
}
