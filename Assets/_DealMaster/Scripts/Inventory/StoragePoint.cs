using UnityEngine;

/// <summary>Ponto de armazenamento do depósito: abre a transferência entre mochila e depósito.</summary>
public class StoragePoint : MonoBehaviour, IInteractable
{
    [SerializeField] private StorageLocation location = StorageLocation.Warehouse;

    public StorageLocation Location { get => location; set => location = value; }

    public string InteractionPrompt => $"Abrir {InventoryPanel.LocationName(location).ToLower()}";

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor) => GameUI.Open(new InventoryPanel(location));
}
