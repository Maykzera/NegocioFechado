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
        if (input == null) return;
        if (input.InventoryPressed) Toggle<InventoryPanel>(() => new InventoryPanel());
        if (input.MenuPressed) Toggle<FinancePanel>(() => new FinancePanel());
    }

    private static void Toggle<T>(System.Func<IModalPanel> create) where T : IModalPanel
    {
        if (GameUI.Current is T) GameUI.Close();
        else if (!GameUI.IsModalOpen) GameUI.Open(create());
    }

    private static string Status()
    {
        if (!GameServices.IsReady) return null;
        var inv = GameServices.Inventory;
        string line = $"Mochila {inv.GetUsedUnits(StorageLocation.Backpack)}/{inv.GetCapacity(StorageLocation.Backpack)}  ·  Pedidos prontos {inv.GetUsedUnits(StorageLocation.Order)}";
        if (GameServices.Sales != null)
        {
            line += $"\nReputação {GameServices.Sales.Reputation:0}";
            int complaints = System.Linq.Enumerable.Count(GameServices.Sales.PendingComplaints());
            if (complaints > 0) line += $"  ·  <color=#ff8a80>{complaints} reclamação(ões) [F]</color>";
        }
        return line;
    }
}
