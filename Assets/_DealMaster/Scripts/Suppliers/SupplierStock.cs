using System;
using System.Collections.Generic;

/// <summary>
/// Estado em tempo de execução de um fornecedor: estoque atual, preços e relacionamento.
/// Lógica pura, sem dependência de cena.
/// </summary>
public sealed class SupplierStock
{
    private sealed class Item
    {
        public long UnitPriceCents;
        public int Stock;
        public int MaxStock;
        public int RestockPerDay;
        public int RequiredLevel;
    }

    private readonly Dictionary<string, Item> items = new Dictionary<string, Item>();

    public string SupplierId { get; }
    public int OpensAtHour { get; }
    public int ClosesAtHour { get; }
    public float MaxDiscount { get; }
    public int VolumeDiscountMinQuantity { get; }
    public float VolumeDiscount { get; }

    /// <summary>Relacionamento com o jogador (0 a 1). Cresce com compras e melhora os descontos.</summary>
    public float Relationship { get; private set; }

    public SupplierStock(
        string supplierId,
        int opensAtHour = 8,
        int closesAtHour = 18,
        float maxDiscount = 0.10f,
        int volumeDiscountMinQuantity = 10,
        float volumeDiscount = 0.05f,
        float relationship = 0f)
    {
        if (string.IsNullOrEmpty(supplierId)) throw new ArgumentException("supplierId vazio");
        SupplierId = supplierId;
        OpensAtHour = opensAtHour;
        ClosesAtHour = closesAtHour;
        MaxDiscount = Clamp01(maxDiscount);
        VolumeDiscountMinQuantity = Math.Max(1, volumeDiscountMinQuantity);
        VolumeDiscount = Clamp01(volumeDiscount);
        Relationship = Clamp01(relationship);
    }

    public static SupplierStock FromDefinition(SupplierDefinition definition)
    {
        var stock = new SupplierStock(
            definition.supplierId,
            definition.opensAtHour,
            definition.closesAtHour,
            definition.maxDiscount,
            definition.volumeDiscountMinQuantity,
            definition.volumeDiscount);

        foreach (var entry in definition.catalog)
        {
            if (entry?.product == null) continue;
            stock.AddCatalogItem(
                entry.product.productId,
                entry.unitPriceCents,
                entry.initialStock,
                entry.restockPerDay,
                entry.product.requiredLevel);
        }
        return stock;
    }

    public void AddCatalogItem(
        string productId,
        long unitPriceCents,
        int stock,
        int restockPerDay = 0,
        int requiredLevel = 1)
    {
        if (string.IsNullOrEmpty(productId)) throw new ArgumentException("productId vazio");
        if (unitPriceCents < 0 || stock < 0 || restockPerDay < 0)
            throw new ArgumentOutOfRangeException();

        items[productId] = new Item
        {
            UnitPriceCents = unitPriceCents,
            Stock = stock,
            MaxStock = stock,
            RestockPerDay = restockPerDay,
            RequiredLevel = Math.Max(1, requiredLevel)
        };
    }

    public bool Sells(string productId) => productId != null && items.ContainsKey(productId);

    public IEnumerable<string> ProductIds => items.Keys;

    public int GetStock(string productId) =>
        productId != null && items.TryGetValue(productId, out var i) ? i.Stock : 0;

    public long GetListPriceCents(string productId) =>
        productId != null && items.TryGetValue(productId, out var i) ? i.UnitPriceCents : 0;

    public int GetRequiredLevel(string productId) =>
        productId != null && items.TryGetValue(productId, out var i) ? i.RequiredLevel : int.MaxValue;

    public bool IsOpen(int hour)
    {
        if (OpensAtHour == ClosesAtHour) return true;
        return OpensAtHour < ClosesAtHour
            ? hour >= OpensAtHour && hour < ClosesAtHour
            : hour >= OpensAtHour || hour < ClosesAtHour; // atravessa a meia-noite
    }

    /// <summary>
    /// Menor preço unitário que o fornecedor aceita para essa quantidade:
    /// preço de lista com desconto por volume e o desconto máximo de negociação,
    /// que aumenta até 50% com um bom relacionamento.
    /// </summary>
    public long GetFloorPriceCents(string productId, int quantity)
    {
        long list = GetVolumePriceCents(productId, quantity);
        float discount = MaxDiscount * (1f + 0.5f * Relationship);
        return (long)Math.Ceiling(list * (1.0 - Math.Min(discount, 0.9f)));
    }

    /// <summary>Preço unitário de lista já com desconto por volume.</summary>
    public long GetVolumePriceCents(string productId, int quantity)
    {
        long list = GetListPriceCents(productId);
        if (quantity >= VolumeDiscountMinQuantity)
            return (long)Math.Ceiling(list * (1.0 - VolumeDiscount));
        return list;
    }

    internal bool TryTakeStock(string productId, int quantity)
    {
        if (!items.TryGetValue(productId, out var i) || quantity <= 0 || quantity > i.Stock)
            return false;
        i.Stock -= quantity;
        return true;
    }

    internal void ImproveRelationship(float amount)
    {
        Relationship = Clamp01(Relationship + amount);
    }

    /// <summary>Restaura estoque e relacionamento de um jogo salvo.</summary>
    public void RestoreState(float relationship, IEnumerable<KeyValuePair<string, int>> stocks)
    {
        Relationship = Clamp01(relationship);
        foreach (var kv in stocks)
            if (items.TryGetValue(kv.Key, out var i)) i.Stock = Math.Max(0, Math.Min(i.MaxStock, kv.Value));
    }

    /// <summary>Reposição diária de estoque, até o estoque máximo de cada item.</summary>
    public void Restock()
    {
        foreach (var i in items.Values)
            i.Stock = Math.Min(i.MaxStock, i.Stock + i.RestockPerDay);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
