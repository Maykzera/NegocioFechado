using UnityEngine;

/// <summary>Mesa da supervisora.</summary>
public class SupervisorDesk : MonoBehaviour, IInteractable
{
    public string InteractionPrompt
    {
        get
        {
            int n = GameServices.Game?.PendingReviews.Count ?? 0;
            return n > 0 ? $"Falar com a supervisora ({n} ligação(ões) para avaliar)" : "Falar com a supervisora";
        }
    }

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor) => GameUI.Open(new SupervisorPanel());
}
