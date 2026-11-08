using UnityEngine;

/// <summary>Bancada física de embalagem no depósito.</summary>
public class PackingBench : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Usar bancada de embalagem";

    public bool CanInteract(InteractionController interactor) => GameServices.Packaging != null;

    public void Interact(InteractionController interactor) => GameUI.Open(new PackingBenchPanel());
}
