using UnityEngine;

/// <summary>Cafeteira da copa: recupera energia.</summary>
public class CoffeeMachine : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => $"Tomar um café ({Money.Format(CallCenterGame.CoffeePriceCents)}) · energia {GameServices.Game?.Energy ?? 0}%";

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor)
    {
        if (GameServices.Game.BuyCoffee(out var error)) { GameUI.Toast("Café quentinho. Energia recuperada."); CallAudio.Sale(); }
        else GameUI.Toast(error);
    }
}
