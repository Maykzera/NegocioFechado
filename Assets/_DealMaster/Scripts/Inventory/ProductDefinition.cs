using UnityEngine;

public enum ProductCategory
{
    Electronics,
    Accessories,
    Home,
    Tools,
    Sports
}

[CreateAssetMenu(
    fileName = "NewProduct",
    menuName = "DealMaster/Product"
)]
public class ProductDefinition : ScriptableObject
{
    public string productId;
    public string productName;
    public ProductCategory category;

    [TextArea]
    public string description;

    [Min(0)] public long baseCostCents;
    [Min(0)] public long referencePriceCents;
    [Min(0)] public float weightKg;
    [Range(0, 1)] public float fragility;

    [Min(1)] public int requiredLevel = 1;

    public GameObject worldPrefab;
    public Sprite icon;
}
