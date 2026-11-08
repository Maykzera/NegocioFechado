using UnityEngine;

/// <summary>Mesa do mestre no escritório. Também recebe os registros das negociações.</summary>
public class MentorDesk : MonoBehaviour, IInteractable
{
    public static MentorService Service { get; private set; } = new MentorService();

    public string InteractionPrompt =>
        Service.Pending.Count > 0 ? $"Falar com o Mestre ({Service.Pending.Count} para analisar)" : "Falar com o Mestre";

    private void Awake()
    {
        Service = new MentorService();
        NegotiationHooks.OnNegotiationEnded += OnNegotiationEnded;
        HUDExtensions.StatusText += Status;
    }

    private void OnDestroy()
    {
        NegotiationHooks.OnNegotiationEnded -= OnNegotiationEnded;
        HUDExtensions.StatusText -= Status;
    }

    public bool CanInteract(InteractionController interactor) => true;

    public void Interact(InteractionController interactor) => GameUI.Open(new MentorPanel(Service));

    private static void OnNegotiationEnded(NegotiationOutcome outcome)
    {
        if (outcome.ProductId == null && (outcome.Brain == null || outcome.Brain.Memory.Count < 2)) return;

        SaleRecord sale = null;
        if (outcome.SaleClosed && GameServices.Sales != null)
            sale = System.Linq.Enumerable.LastOrDefault(GameServices.Sales.Sales,
                s => s.CustomerName == outcome.CustomerName && s.ProductId == outcome.ProductId);

        int day = GameServices.Ledger?.DayProvider?.Invoke() ?? 1;
        Service.Submit(MentorService.BuildRecord(outcome, sale, day));
    }

    private static string Status() =>
        Service.Pending.Count > 0 ? $"Mestre: {Service.Pending.Count} negociação(ões) para analisar" : null;
}
