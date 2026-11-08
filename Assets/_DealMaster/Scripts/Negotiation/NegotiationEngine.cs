using System;
using UnityEngine;

[Serializable]
public class CustomerProfile
{
    public string customerId;
    public long budgetCents;

    [Range(0, 1)]
    public float priceSensitivity = 0.5f;

    [Range(0, 1)]
    public float trust = 0.5f;

    [Range(0, 1)]
    public float productInterest = 0.5f;
}

public static class NegotiationEngine
{
    public static float CalculateAcceptance(
        CustomerProfile customer,
        long offeredPriceCents,
        long referencePriceCents)
    {
        if (customer == null ||
            offeredPriceCents <= 0 ||
            referencePriceCents <= 0 ||
            offeredPriceCents > customer.budgetCents)
            return 0f;

        float priceRatio =
            (float)offeredPriceCents /
            referencePriceCents;

        float priceScore = Mathf.Clamp01(
            1.5f - priceRatio
        );

        float utility =
            priceScore * customer.priceSensitivity +
            customer.productInterest * 0.30f +
            customer.trust * 0.20f;

        return Mathf.Clamp01(utility);
    }
}
