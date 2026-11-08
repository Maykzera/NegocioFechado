using System;
using System.Collections.Generic;

/// <summary>Locais onde o jogador pode guardar mercadorias.</summary>
public enum StorageLocation
{
    Backpack,
    Warehouse,
    PackingBench,
    Order
}

public enum InventoryMovementType
{
    Added,
    Removed,
    Reserved,
    Released,
    TransferredOut,
    TransferredIn
}

/// <summary>Registro imutável de uma movimentação de estoque.</summary>
public readonly struct InventoryMovement
{
    public readonly InventoryMovementType Type;
    public readonly string ProductId;
    public readonly StorageLocation Location;
    public readonly int Quantity;
    public readonly long UnitCostCents;
    public readonly string Reason;

    public InventoryMovement(
        InventoryMovementType type,
        string productId,
        StorageLocation location,
        int quantity,
        long unitCostCents,
        string reason)
    {
        Type = type;
        ProductId = productId;
        Location = location;
        Quantity = quantity;
        UnitCostCents = unitCostCents;
        Reason = reason;
    }
}

/// <summary>Pilha de um mesmo produto em um local.</summary>
public sealed class InventoryStack
{
    public string ProductId { get; }
    public int Quantity { get; internal set; }
    public int Reserved { get; internal set; }

    /// <summary>Custo total de aquisição das unidades da pilha (custo médio ponderado).</summary>
    public long TotalCostCents { get; internal set; }

    public int Available => Quantity - Reserved;

    public long AverageUnitCostCents =>
        Quantity == 0 ? 0 : TotalCostCents / Quantity;

    internal InventoryStack(string productId)
    {
        ProductId = productId;
    }
}

/// <summary>
/// Estoque do jogador. Lógica pura (sem dependência de cena), para ser testável.
/// Toda operação valida antes de alterar o estado: ou ela acontece por completo, ou nada muda.
/// </summary>
public sealed class InventoryService
{
    private readonly Dictionary<StorageLocation, int> capacities =
        new Dictionary<StorageLocation, int>();

    private readonly Dictionary<StorageLocation, Dictionary<string, InventoryStack>> stacks =
        new Dictionary<StorageLocation, Dictionary<string, InventoryStack>>();

    private readonly List<InventoryMovement> history = new List<InventoryMovement>();

    public event Action<StorageLocation, string> OnStackChanged;

    public IReadOnlyList<InventoryMovement> History => history;

    public InventoryService(
        int backpackCapacity = 10,
        int warehouseCapacity = 100,
        int benchCapacity = 4,
        int orderCapacity = 20)
    {
        SetCapacity(StorageLocation.Backpack, backpackCapacity);
        SetCapacity(StorageLocation.Warehouse, warehouseCapacity);
        SetCapacity(StorageLocation.PackingBench, benchCapacity);
        SetCapacity(StorageLocation.Order, orderCapacity);
    }

    /// <summary>Altera a capacidade (ex.: ao desbloquear um depósito maior).</summary>
    public void SetCapacity(StorageLocation location, int units)
    {
        if (units < 0) throw new ArgumentOutOfRangeException(nameof(units));
        capacities[location] = units;
        if (!stacks.ContainsKey(location))
            stacks[location] = new Dictionary<string, InventoryStack>();
    }

    public int GetCapacity(StorageLocation location) => capacities[location];

    public int GetUsedUnits(StorageLocation location)
    {
        int total = 0;
        foreach (var s in stacks[location].Values) total += s.Quantity;
        return total;
    }

    public int GetFreeUnits(StorageLocation location) =>
        capacities[location] - GetUsedUnits(location);

    public InventoryStack GetStack(StorageLocation location, string productId)
    {
        stacks[location].TryGetValue(productId ?? string.Empty, out var stack);
        return stack;
    }

    public int GetQuantity(StorageLocation location, string productId) =>
        GetStack(location, productId)?.Quantity ?? 0;

    public int GetAvailable(StorageLocation location, string productId) =>
        GetStack(location, productId)?.Available ?? 0;

    public IEnumerable<InventoryStack> GetStacks(StorageLocation location) =>
        stacks[location].Values;

    /// <summary>Verifica se cabem <paramref name="quantity"/> unidades no local.</summary>
    public bool CanAdd(StorageLocation location, int quantity) =>
        quantity > 0 && quantity <= GetFreeUnits(location);

    public bool TryAdd(
        StorageLocation location,
        string productId,
        int quantity,
        long unitCostCents,
        string reason = "")
    {
        if (string.IsNullOrEmpty(productId) ||
            unitCostCents < 0 ||
            !CanAdd(location, quantity))
            return false;

        long addedCost;
        try
        {
            addedCost = checked(unitCostCents * quantity);
        }
        catch (OverflowException)
        {
            return false;
        }

        var stack = GetOrCreate(location, productId);
        if (addedCost > long.MaxValue - stack.TotalCostCents)
            return false;

        stack.Quantity += quantity;
        stack.TotalCostCents += addedCost;

        Record(InventoryMovementType.Added, productId, location, quantity, unitCostCents, reason);
        return true;
    }

    /// <summary>Remove unidades disponíveis (não reservadas). Retorna o custo das unidades removidas.</summary>
    public bool TryRemove(
        StorageLocation location,
        string productId,
        int quantity,
        out long removedCostCents,
        string reason = "")
    {
        removedCostCents = 0;
        var stack = GetStack(location, productId);
        if (stack == null || quantity <= 0 || quantity > stack.Available)
            return false;

        removedCostCents = TakeCost(stack, quantity);
        stack.Quantity -= quantity;
        Cleanup(location, stack);

        Record(InventoryMovementType.Removed, productId, location, quantity,
            removedCostCents / quantity, reason);
        return true;
    }

    public bool TryReserve(StorageLocation location, string productId, int quantity, string reason = "")
    {
        var stack = GetStack(location, productId);
        if (stack == null || quantity <= 0 || quantity > stack.Available)
            return false;

        stack.Reserved += quantity;
        Record(InventoryMovementType.Reserved, productId, location, quantity,
            stack.AverageUnitCostCents, reason);
        return true;
    }

    public bool TryRelease(StorageLocation location, string productId, int quantity, string reason = "")
    {
        var stack = GetStack(location, productId);
        if (stack == null || quantity <= 0 || quantity > stack.Reserved)
            return false;

        stack.Reserved -= quantity;
        Record(InventoryMovementType.Released, productId, location, quantity,
            stack.AverageUnitCostCents, reason);
        return true;
    }

    /// <summary>
    /// Move unidades disponíveis entre locais (mochila → depósito → bancada → pedido).
    /// Atômico: valida origem e espaço no destino antes de alterar qualquer coisa.
    /// </summary>
    public bool TryTransfer(
        StorageLocation from,
        StorageLocation to,
        string productId,
        int quantity,
        string reason = "")
    {
        if (from == to) return false;

        var source = GetStack(from, productId);
        if (source == null ||
            quantity <= 0 ||
            quantity > source.Available ||
            !CanAdd(to, quantity))
            return false;

        long movedCost = TakeCost(source, quantity);
        source.Quantity -= quantity;
        Cleanup(from, source);

        var target = GetOrCreate(to, productId);
        target.Quantity += quantity;
        target.TotalCostCents += movedCost;

        long unitCost = movedCost / quantity;
        Record(InventoryMovementType.TransferredOut, productId, from, quantity, unitCost, reason);
        Record(InventoryMovementType.TransferredIn, productId, to, quantity, unitCost, reason);
        return true;
    }

    /// <summary>Esvazia todos os locais (usado ao carregar um jogo salvo).</summary>
    public void Clear()
    {
        foreach (var location in stacks.Values) location.Clear();
        history.Clear();
    }

    /// <summary>Restaura uma pilha salva, com o custo total exato. Reservas não são salvas.</summary>
    public bool RestoreStack(StorageLocation location, string productId, int quantity, long totalCostCents)
    {
        if (string.IsNullOrEmpty(productId) || quantity <= 0 || totalCostCents < 0) return false;
        if (GetUsedUnits(location) + quantity > capacities[location]) capacities[location] = GetUsedUnits(location) + quantity;
        var stack = GetOrCreate(location, productId);
        stack.Quantity += quantity;
        stack.TotalCostCents += totalCostCents;
        return true;
    }

    /// <summary>Valor total do estoque (custo de aquisição) em todos os locais.</summary>
    public long GetTotalStockValueCents()
    {
        long total = 0;
        foreach (var location in stacks.Values)
            foreach (var s in location.Values)
                total += s.TotalCostCents;
        return total;
    }

    private static long TakeCost(InventoryStack stack, int quantity)
    {
        // Ao zerar a pilha, leva o custo restante inteiro para não sobrar centavo "órfão".
        if (quantity == stack.Quantity) return stack.TotalCostCents;

        long cost = stack.TotalCostCents / stack.Quantity * quantity;
        stack.TotalCostCents -= cost;
        return cost;
    }

    private InventoryStack GetOrCreate(StorageLocation location, string productId)
    {
        var dict = stacks[location];
        if (!dict.TryGetValue(productId, out var stack))
        {
            stack = new InventoryStack(productId);
            dict[productId] = stack;
        }
        return stack;
    }

    private void Cleanup(StorageLocation location, InventoryStack stack)
    {
        if (stack.Quantity == 0)
        {
            stack.TotalCostCents = 0;
            stacks[location].Remove(stack.ProductId);
        }
    }

    private void Record(
        InventoryMovementType type,
        string productId,
        StorageLocation location,
        int quantity,
        long unitCostCents,
        string reason)
    {
        history.Add(new InventoryMovement(type, productId, location, quantity, unitCostCents, reason));
        OnStackChanged?.Invoke(location, productId);
    }
}
