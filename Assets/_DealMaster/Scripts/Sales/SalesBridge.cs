using UnityEngine;

/// <summary>Liga a negociação ao SalesService e avisa o jogador sobre entregas e reclamações.</summary>
public class SalesBridge : MonoBehaviour
{
    [SerializeField] private int deliverySeed = 777;

    private System.Random rng;

    private void Start()
    {
        rng = new System.Random(deliverySeed);
        NegotiationHooks.CompleteSale = Complete;
        GameServices.Sales.OnComplaint += sale =>
        {
            CustomerSpawner.Memory.RecordComplaint(sale.CustomerName);
            GameUI.Toast($"{sale.CustomerName} reclamou: o produto chegou avariado. Abra o painel [F].", 5f);
        };
    }

    private void OnDestroy()
    {
        if (NegotiationHooks.CompleteSale == Complete) NegotiationHooks.CompleteSale = null;
    }

    private string Complete(NegotiationOutcome outcome)
    {
        if (outcome.ProductId == null) return "Nenhum produto foi apresentado.";

        bool ok = GameServices.Sales.TryCompleteSale(outcome.CustomerName, outcome.ProductId,
            outcome.AgreedPriceCents, outcome.Packed, rng.NextDouble(), out var sale, out var error);
        if (!ok) return error;

        string profit = CustomerBrain.FormatReais(sale.ProfitCents);
        GameUI.Toast($"Venda concluída: {CustomerBrain.FormatReais(sale.PriceCents)} (lucro {profit})");
        return null;
    }
}
