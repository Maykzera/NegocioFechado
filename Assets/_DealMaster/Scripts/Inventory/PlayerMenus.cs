using UnityEngine;

/// <summary>Atalhos de menus do jogador (Tab = mochila) e linha de status no HUD.</summary>
public class PlayerMenus : MonoBehaviour
{
    private PlayerInputReader input;

    private void Awake()
    {
        input = FindObjectOfType<PlayerInputReader>();
        HUDExtensions.StatusText += Status;
    }

    private void OnDestroy()
    {
        HUDExtensions.StatusText -= Status;
    }

    private void Update()
    {
        if (input == null || !input.InventoryPressed) return;
        if (GameUI.Current is InventoryPanel) GameUI.Close();
        else if (!GameUI.IsModalOpen) GameUI.Open(new InventoryPanel());
    }

    private static string Status()
    {
        if (!GameServices.IsReady) return null;
        var inv = GameServices.Inventory;
        return $"Mochila {inv.GetUsedUnits(StorageLocation.Backpack)}/{inv.GetCapacity(StorageLocation.Backpack)}";
    }
}
