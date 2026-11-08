using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SupplierCatalogEntry
{
    public ProductDefinition product;

    [Min(0)] public long unitPriceCents;
    [Min(0)] public int initialStock = 10;
    [Min(0)] public int restockPerDay = 5;
}

/// <summary>Definição estática de um fornecedor (catálogo, horários e personalidade comercial).</summary>
[CreateAssetMenu(
    fileName = "NewSupplier",
    menuName = "DealMaster/Supplier"
)]
public class SupplierDefinition : ScriptableObject
{
    public string supplierId;
    public string supplierName;

    [Range(0, 23)] public int opensAtHour = 8;
    [Range(1, 24)] public int closesAtHour = 18;

    /// <summary>Desconto máximo que o vendedor aceita conceder em uma negociação (0 a 1).</summary>
    [Range(0, 0.5f)] public float maxDiscount = 0.10f;

    /// <summary>Quantidade a partir da qual aplica desconto por volume.</summary>
    [Min(1)] public int volumeDiscountMinQuantity = 10;

    [Range(0, 0.3f)] public float volumeDiscount = 0.05f;

    public List<SupplierCatalogEntry> catalog = new List<SupplierCatalogEntry>();
}
