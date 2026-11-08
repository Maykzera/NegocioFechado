using System;
using System.Collections.Generic;

/// <summary>Tamanho de caixa que um produto exige.</summary>
public enum BoxSize
{
    Small,
    Medium,
    Large
}

/// <summary>
/// Dados de um produto em tempo de execução (lógica pura).
/// Vem de um ProductDefinition (ScriptableObject) ou do conteúdo padrão do jogo.
/// </summary>
public sealed class ProductInfo
{
    public string Id { get; }
    public string Name { get; }
    public ProductCategory Category { get; }
    public long BaseCostCents { get; }
    public long ReferencePriceCents { get; }
    public float WeightKg { get; }

    /// <summary>0 = resistente, 1 = muito frágil.</summary>
    public float Fragility { get; }

    /// <summary>Qualidade percebida (0 a 1).</summary>
    public float Quality { get; }

    public BoxSize Size { get; }
    public int RequiredLevel { get; }

    /// <summary>Argumentos verdadeiros que o jogador pode usar na venda.</summary>
    public IReadOnlyList<string> Benefits { get; }

    /// <summary>Exageros (falsos). Funcionam às vezes, mas o cliente pode perceber e o mestre sempre vê.</summary>
    public IReadOnlyList<string> Exaggerations { get; }

    public ProductInfo(
        string id, string name, ProductCategory category,
        long baseCostCents, long referencePriceCents,
        float weightKg, float fragility, float quality, BoxSize size, int requiredLevel,
        IReadOnlyList<string> benefits, IReadOnlyList<string> exaggerations)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("id vazio");
        Id = id;
        Name = name;
        Category = category;
        BaseCostCents = baseCostCents;
        ReferencePriceCents = referencePriceCents;
        WeightKg = weightKg;
        Fragility = fragility;
        Quality = quality;
        Size = size;
        RequiredLevel = Math.Max(1, requiredLevel);
        Benefits = benefits ?? Array.Empty<string>();
        Exaggerations = exaggerations ?? Array.Empty<string>();
    }

    public ProductPitch ToPitch() => new ProductPitch(Id, Category, ReferencePriceCents, Quality);
}

/// <summary>Registro de todos os produtos conhecidos pelo jogo.</summary>
public static class ProductDatabase
{
    private static readonly Dictionary<string, ProductInfo> products = new Dictionary<string, ProductInfo>();

    public static IEnumerable<ProductInfo> All => products.Values;

    public static void Register(ProductInfo info)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        products[info.Id] = info;
    }

    public static ProductInfo Get(string id) =>
        id != null && products.TryGetValue(id, out var p) ? p : null;

    public static string NameOf(string id) => Get(id)?.Name ?? id;

    public static void Clear() => products.Clear();
}
