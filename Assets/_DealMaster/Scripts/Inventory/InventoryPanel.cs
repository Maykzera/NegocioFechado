using System.Linq;
using UnityEngine;

/// <summary>
/// Tela de inventário. Mostra a mochila; perto do depósito (ou da bancada),
/// permite transferir produtos entre os locais.
/// </summary>
public class InventoryPanel : IModalPanel
{
    private readonly StorageLocation? other;
    private Vector2 scrollA, scrollB;

    public string Title => other.HasValue ? $"Mochila  ↔  {LocationName(other.Value)}" : "Mochila";

    /// <param name="other">Segundo local para transferências (null = só ver a mochila).</param>
    public InventoryPanel(StorageLocation? other = null)
    {
        this.other = other;
    }

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        var inv = GameServices.Inventory;
        if (!other.HasValue)
        {
            DrawList(area, StorageLocation.Backpack, null, ref scrollA);
            return;
        }

        float half = (area.width - 20) / 2;
        DrawList(new Rect(area.x, area.y, half, area.height), StorageLocation.Backpack, other.Value, ref scrollA);
        DrawList(new Rect(area.x + half + 20, area.y, half, area.height), other.Value, StorageLocation.Backpack, ref scrollB);
    }

    private void DrawList(Rect r, StorageLocation location, StorageLocation? target, ref Vector2 scroll)
    {
        var inv = GameServices.Inventory;
        GUI.Label(new Rect(r.x, r.y, r.width, 28),
            $"{LocationName(location)}  ({inv.GetUsedUnits(location)}/{inv.GetCapacity(location)})", UIStyles.Header);

        var stacks = inv.GetStacks(location).OrderBy(s => ProductDatabase.NameOf(s.ProductId)).ToList();
        if (stacks.Count == 0)
        {
            GUI.Label(new Rect(r.x, r.y + 36, r.width, 30), "Vazio.", UIStyles.Small);
            return;
        }

        float rowH = 64;
        scroll = GUI.BeginScrollView(new Rect(r.x, r.y + 34, r.width, r.height - 34), scroll,
            new Rect(0, 0, r.width - 20, stacks.Count * (rowH + 6)));
        for (int i = 0; i < stacks.Count; i++)
        {
            var s = stacks[i];
            float y = i * (rowH + 6);
            GUI.Box(new Rect(0, y, r.width - 24, rowH), GUIContent.none);
            string reserved = s.Reserved > 0 ? $"  ·  {s.Reserved} reservado(s)" : "";
            GUI.Label(new Rect(10, y + 4, r.width - 180, 30), $"{s.Quantity}x {ProductDatabase.NameOf(s.ProductId)}", UIStyles.Text);
            GUI.Label(new Rect(10, y + 32, r.width - 180, 26), $"custo médio {CustomerBrain.FormatReais(s.AverageUnitCostCents)}{reserved}", UIStyles.Small);

            if (target.HasValue)
            {
                if (GUI.Button(new Rect(r.width - 160, y + 12, 60, 40), "1 →", UIStyles.Button)) Move(location, target.Value, s.ProductId, 1);
                if (GUI.Button(new Rect(r.width - 94, y + 12, 64, 40), "Tudo", UIStyles.Button)) Move(location, target.Value, s.ProductId, s.Available);
            }
        }
        GUI.EndScrollView();
    }

    private static void Move(StorageLocation from, StorageLocation to, string productId, int quantity)
    {
        if (quantity <= 0) return;
        if (!GameServices.Inventory.TryTransfer(from, to, productId, quantity, "Transferência manual"))
            GameUI.Toast($"Não cabe em {LocationName(to)}.");
    }

    public static string LocationName(StorageLocation location)
    {
        switch (location)
        {
            case StorageLocation.Backpack: return "Mochila";
            case StorageLocation.Warehouse: return "Depósito";
            case StorageLocation.PackingBench: return "Bancada";
            default: return "Pedidos prontos";
        }
    }
}
