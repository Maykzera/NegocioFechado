using System;
using System.Collections.Generic;

/// <summary>Medições feitas durante o minigame de embalagem.</summary>
public sealed class PackagingInput
{
    public ProductInfo Product;
    public bool Inspected;
    public BoxSize Box;

    /// <summary>Folhas de plástico bolha usadas.</summary>
    public int PaddingSheets;

    /// <summary>Distância do produto ao centro da caixa (0 = centralizado, 1 = encostado na parede).</summary>
    public float PlacementOffset;

    public bool FlapsClosed;

    /// <summary>Cobertura de cada tira de fita sobre a emenda (0 a 1).</summary>
    public List<float> TapeStrips = new List<float>();

    public bool LabelApplied;

    /// <summary>Precisão da etiqueta na área indicada (0 a 1).</summary>
    public float LabelAccuracy;

    public float SecondsSpent;
}

/// <summary>Resultado da avaliação de uma embalagem.</summary>
public readonly struct PackagingScore
{
    public readonly float Protection;
    public readonly float Integrity;
    public readonly float SizeFit;
    public readonly float Presentation;
    public readonly float Efficiency;
    public readonly float Time;
    public readonly float Overall;
    public readonly float DamageRisk;
    public readonly long MaterialsCostCents;
    public readonly bool Valid;
    public readonly string Problem;

    public PackagingScore(float protection, float integrity, float sizeFit, float presentation,
        float efficiency, float time, float overall, float damageRisk, long materialsCostCents,
        bool valid, string problem)
    {
        Protection = protection;
        Integrity = integrity;
        SizeFit = sizeFit;
        Presentation = presentation;
        Efficiency = efficiency;
        Time = time;
        Overall = overall;
        DamageRisk = damageRisk;
        MaterialsCostCents = materialsCostCents;
        Valid = valid;
        Problem = problem;
    }
}

/// <summary>
/// Avalia a embalagem: proteção, integridade, tamanho da caixa, apresentação, custo dos materiais e tempo.
/// Embalagens ruins aumentam o risco de avaria na entrega.
/// </summary>
public static class PackagingEvaluator
{
    public const long SmallBoxCents = 150;
    public const long MediumBoxCents = 250;
    public const long LargeBoxCents = 400;
    public const long PaddingSheetCents = 50;
    public const long TapeStripCents = 20;
    public const long LabelCents = 30;
    public const int MaxPadding = 6;
    public const int MaxTapeStrips = 5;

    public static long BoxCost(BoxSize size) =>
        size == BoxSize.Small ? SmallBoxCents : size == BoxSize.Medium ? MediumBoxCents : LargeBoxCents;

    /// <summary>Folhas de plástico bolha recomendadas para o produto.</summary>
    public static int RecommendedPadding(ProductInfo product) =>
        (int)Math.Ceiling(product.Fragility * 4f);

    public static long MaterialsCost(PackagingInput input)
    {
        long cost = BoxCost(input.Box);
        cost += Math.Max(0, input.PaddingSheets) * PaddingSheetCents;
        cost += input.TapeStrips.Count * TapeStripCents;
        if (input.LabelApplied) cost += LabelCents;
        return cost;
    }

    public static PackagingScore Evaluate(PackagingInput input)
    {
        if (input?.Product == null) throw new ArgumentNullException(nameof(input));
        var p = input.Product;
        long materials = MaterialsCost(input);

        int sizeDiff = (int)input.Box - (int)p.Size;
        if (sizeDiff < 0)
            return Invalid("O produto não cabe nessa caixa.", materials);
        if (!input.FlapsClosed)
            return Invalid("Feche a caixa antes de passar a fita.", materials);
        if (input.TapeStrips.Count == 0)
            return Invalid("A caixa precisa de fita.", materials);
        if (!input.LabelApplied)
            return Invalid("Falta a etiqueta.", materials);

        // Tamanho: caixa certa = 1; uma maior = 0,6; duas maiores = 0,3.
        float sizeFit = sizeDiff == 0 ? 1f : sizeDiff == 1 ? 0.6f : 0.3f;

        // Proteção: caixa folgada exige mais enchimento.
        int needed = RecommendedPadding(p) + sizeDiff * 2;
        float paddingRatio = needed == 0 ? 1f : Clamp01((float)input.PaddingSheets / needed);
        float placement = 1f - Clamp01(input.PlacementOffset);
        float protection = Clamp01(paddingRatio * 0.75f + placement * 0.25f);
        if (needed == 0 && input.PaddingSheets == 0) protection = Clamp01(0.85f + placement * 0.15f);

        // Integridade: pelo menos 2 tiras bem aplicadas sobre a emenda.
        float tape = 0f;
        foreach (float strip in input.TapeStrips) tape += Clamp01(strip);
        float integrity = Clamp01(tape / 2f);

        float presentation = Clamp01(
            0.45f * (input.Inspected ? 1f : 0f) +
            0.30f * Clamp01(input.LabelAccuracy) +
            0.25f * placement);

        // Eficiência: penaliza material sobrando (enchimento e fita em excesso).
        int extraPadding = Math.Max(0, input.PaddingSheets - needed - 1);
        int extraTape = Math.Max(0, input.TapeStrips.Count - 3);
        float efficiency = Clamp01(1f - extraPadding * 0.15f - extraTape * 0.1f - (1f - sizeFit) * 0.4f);

        // Tempo: até 45 s é ótimo; acima de 150 s, mínimo.
        float time = input.SecondsSpent <= 45f ? 1f : Clamp01(1f - (input.SecondsSpent - 45f) / 105f) * 0.7f + 0.3f;

        float overall =
            protection * 0.30f +
            integrity * 0.20f +
            sizeFit * 0.15f +
            presentation * 0.15f +
            efficiency * 0.10f +
            time * 0.10f;

        float vulnerability = 1f - protection * 0.7f - integrity * 0.3f;
        float damageRisk = Math.Min(0.9f, Clamp01(p.Fragility * vulnerability * 0.9f + (input.Inspected ? 0f : 0.05f)));

        return new PackagingScore(protection, integrity, sizeFit, presentation, efficiency, time,
            Clamp01(overall), damageRisk, materials, true, null);
    }

    private static PackagingScore Invalid(string problem, long materials) =>
        new PackagingScore(0, 0, 0, 0, 0, 0, 0, 1f, materials, false, problem);

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}
