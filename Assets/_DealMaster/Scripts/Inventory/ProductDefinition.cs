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

    [Range(0, 1)] public float quality = 0.6f;
    public BoxSize boxSize = BoxSize.Small;

    [Tooltip("Argumentos verdadeiros que o jogador pode usar na venda.")]
    public string[] benefits = new string[0];

    [Tooltip("Exageros (falsos). O mestre detecta.")]
    public string[] exaggerations = new string[0];

    public GameObject worldPrefab;
    public Sprite icon;

    public ProductInfo ToInfo() => new ProductInfo(
        productId, productName, category, baseCostCents, referencePriceCents,
        weightKg, fragility, quality, boxSize, requiredLevel, benefits, exaggerations);
}
