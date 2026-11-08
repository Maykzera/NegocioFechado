using UnityEngine;

/// <summary>Balcão de um fornecedor. Interagir abre a tela de compra.</summary>
public class SupplierCounter : MonoBehaviour, IInteractable
{
    [SerializeField] private string supplierId = DefaultContent.ElectronicsSupplierId;
    [SerializeField] private string clerkName = "Vendedor";

    public string SupplierId { get => supplierId; set => supplierId = value; }
    public string ClerkName { get => clerkName; set => clerkName = value; }

    public string InteractionPrompt => $"Falar com {clerkName}";

    public bool CanInteract(InteractionController interactor) => GameServices.GetSupplier(supplierId) != null;

    public void Interact(InteractionController interactor)
    {
        var supplier = GameServices.GetSupplier(supplierId);
        if (supplier != null) GameUI.Open(new ShopPanel(supplier, clerkName));
    }
}
