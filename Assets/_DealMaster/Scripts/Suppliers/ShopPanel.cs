using System.Linq;
using UnityEngine;

/// <summary>Tela de compra no balcão de um fornecedor, com negociação de preço.</summary>
public class ShopPanel : IModalPanel
{
    private readonly SupplierStock supplier;
    private readonly string clerkName;
    private string selectedId;
    private int quantity = 1;
    private long offerCents;
    private long agreedUnitPrice;
    private string clerkLine;
    private Vector2 scroll;
    private int haggleAttempts;

    public string Title => GameServices.SupplierName(supplier.SupplierId);

    public ShopPanel(SupplierStock supplier, string clerkName)
    {
        this.supplier = supplier;
        this.clerkName = clerkName;
        clerkLine = "Bom dia! Fique à vontade, qualquer coisa é só chamar.";
    }

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        int hour = GameServices.CurrentHour;
        if (!supplier.IsOpen(hour))
        {
            GUI.Label(area, $"A loja está fechada agora. Horário: {supplier.OpensAtHour}h às {supplier.ClosesAtHour}h.", UIStyles.Text);
            return;
        }

        float listWidth = area.width * 0.45f;
        DrawCatalog(new Rect(area.x, area.y, listWidth, area.height));
        DrawDeal(new Rect(area.x + listWidth + 20, area.y, area.width - listWidth - 20, area.height));
    }

    private void DrawCatalog(Rect r)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 26), "Catálogo", UIStyles.Header);
        var ids = supplier.ProductIds.ToList();
        float rowH = 58;
        scroll = GUI.BeginScrollView(new Rect(r.x, r.y + 30, r.width, r.height - 30), scroll,
            new Rect(0, 0, r.width - 20, ids.Count * (rowH + 6)));

        for (int i = 0; i < ids.Count; i++)
        {
            var info = ProductDatabase.Get(ids[i]);
            bool locked = GameServices.PlayerLevel < supplier.GetRequiredLevel(ids[i]);
            string label = $"{info?.Name ?? ids[i]}\n{CustomerBrain.FormatReais(supplier.GetListPriceCents(ids[i]))}  ·  estoque {supplier.GetStock(ids[i])}" +
                           (locked ? $"  ·  nível {supplier.GetRequiredLevel(ids[i])}" : "");
            var style = ids[i] == selectedId ? UIStyles.Selected : UIStyles.ListItem;
            GUI.enabled = !locked;
            if (GUI.Button(new Rect(0, i * (rowH + 6), r.width - 24, rowH), label, style)) Select(ids[i]);
            GUI.enabled = true;
        }
        GUI.EndScrollView();
    }

    private void Select(string id)
    {
        selectedId = id;
        quantity = 1;
        agreedUnitPrice = 0;
        haggleAttempts = 0;
        offerCents = supplier.GetVolumePriceCents(id, quantity);
        var info = ProductDatabase.Get(id);
        clerkLine = info != null
            ? $"Esse {info.Name.ToLower()} sai por {CustomerBrain.FormatReais(supplier.GetListPriceCents(id))} a unidade."
            : clerkLine;
    }

    private void DrawDeal(Rect r)
    {
        float y = r.y;
        GUI.Label(new Rect(r.x, y, r.width, 60), $"<b>{clerkName}:</b> \"{clerkLine}\"", UIStyles.Text);
        y += 70;

        if (selectedId == null)
        {
            GUI.Label(new Rect(r.x, y, r.width, 60), "Escolha um produto no catálogo.", UIStyles.Small);
            return;
        }

        var info = ProductDatabase.Get(selectedId);
        if (info != null)
        {
            GUI.Label(new Rect(r.x, y, r.width, 26), info.Name, UIStyles.Header); y += 28;
            GUI.Label(new Rect(r.x, y, r.width, 46),
                $"Preço de mercado na revenda: ~{CustomerBrain.FormatReais(info.ReferencePriceCents)}  ·  Fragilidade {(int)(info.Fragility * 100)}%  ·  Caixa {BoxName(info.Size)}",
                UIStyles.Small);
            y += 48;
        }

        GUI.Label(new Rect(r.x, y, 120, 34), "Quantidade", UIStyles.Text);
        if (GUI.Button(new Rect(r.x + 130, y, 40, 34), "-", UIStyles.Button)) SetQuantity(quantity - 1);
        GUI.Label(new Rect(r.x + 180, y, 50, 34), quantity.ToString(), UIStyles.Header);
        if (GUI.Button(new Rect(r.x + 230, y, 40, 34), "+", UIStyles.Button)) SetQuantity(quantity + 1);
        GUI.Label(new Rect(r.x + 285, y + 4, r.width - 285, 30), $"Mochila livre: {GameServices.Inventory.GetFreeUnits(StorageLocation.Backpack)}", UIStyles.Small);
        y += 44;

        long asking = supplier.GetVolumePriceCents(selectedId, quantity);
        long unit = agreedUnitPrice > 0 ? agreedUnitPrice : asking;
        GUI.Label(new Rect(r.x, y, r.width, 28),
            $"Preço pedido: {CustomerBrain.FormatReais(asking)} /un" + (asking < supplier.GetListPriceCents(selectedId) ? "  (desconto por volume)" : ""),
            UIStyles.Text);
        y += 32;

        GUI.Label(new Rect(r.x, y, r.width, 26), "Sua proposta por unidade:", UIStyles.Small); y += 26;
        offerCents = UIStyles.PriceField(new Rect(r.x, y, Mathf.Min(r.width, 420), 36), offerCents);
        y += 44;

        if (GUI.Button(new Rect(r.x, y, 200, 40), "Propor preço", UIStyles.Button)) Haggle();
        if (agreedUnitPrice > 0)
            GUI.Label(new Rect(r.x + 210, y + 6, r.width - 210, 30), $"Combinado: {CustomerBrain.FormatReais(agreedUnitPrice)} /un", UIStyles.Good);
        y += 52;

        long total = unit * quantity;
        GUI.Label(new Rect(r.x, y, r.width, 30), $"Total: {CustomerBrain.FormatReais(total)}   ·   Saldo: {CustomerBrain.FormatReais(GameServices.Wallet.BalanceCents)}", UIStyles.Money);
        y += 38;

        if (GUI.Button(new Rect(r.x, y, 260, 46), $"Comprar ({CustomerBrain.FormatReais(total)})", UIStyles.Button))
            Buy(unit);
    }

    private void SetQuantity(int q)
    {
        quantity = Mathf.Clamp(q, 1, Mathf.Max(1, supplier.GetStock(selectedId)));
        agreedUnitPrice = 0; // mudou a quantidade, o preço precisa ser renegociado
        offerCents = supplier.GetVolumePriceCents(selectedId, quantity);
    }

    private void Haggle()
    {
        if (haggleAttempts >= 3)
        {
            clerkLine = "Olha, já fiz o que dava. O preço é esse.";
            return;
        }
        haggleAttempts++;

        var response = GameServices.Purchases.EvaluateOffer(supplier, selectedId, quantity, offerCents);
        switch (response.Result)
        {
            case SupplierOfferResult.Accepted:
                agreedUnitPrice = response.UnitPriceCents;
                clerkLine = $"Fechado, faço por {CustomerBrain.FormatReais(agreedUnitPrice)} cada.";
                break;
            case SupplierOfferResult.CounterOffer:
                agreedUnitPrice = response.UnitPriceCents;
                offerCents = response.UnitPriceCents;
                clerkLine = $"Assim não dá... consigo fazer por {CustomerBrain.FormatReais(response.UnitPriceCents)}.";
                break;
            default:
                clerkLine = "Esse valor nem cobre o meu custo. Não dá.";
                break;
        }
    }

    private void Buy(long unitPrice)
    {
        var result = GameServices.Purchases.TryPurchase(supplier, selectedId, quantity, unitPrice,
            GameServices.PlayerLevel, GameServices.CurrentHour, StorageLocation.Backpack, out var receipt);

        switch (result)
        {
            case PurchaseResult.Success:
                clerkLine = "Obrigado! Volte sempre.";
                GameUI.Toast($"Comprou {receipt.Quantity}x {ProductDatabase.NameOf(receipt.ProductId)} por {CustomerBrain.FormatReais(receipt.TotalCents)}");
                agreedUnitPrice = 0;
                haggleAttempts = 0;
                break;
            case PurchaseResult.InsufficientFunds: clerkLine = "Hmm, parece que o dinheiro não dá."; break;
            case PurchaseResult.NoSpace: clerkLine = "Sua mochila está cheia. Guarde coisas no depósito."; break;
            case PurchaseResult.OutOfStock: clerkLine = "Não tenho essa quantidade em estoque."; break;
            case PurchaseResult.PriceBelowFloor: clerkLine = "Por esse preço eu não vendo."; break;
            case PurchaseResult.LevelTooLow: clerkLine = "Esse produto eu só vendo para revendedores mais experientes."; break;
            default: clerkLine = "Não consegui fechar essa compra."; break;
        }
    }

    public static string BoxName(BoxSize size) =>
        size == BoxSize.Small ? "pequena" : size == BoxSize.Medium ? "média" : "grande";
}
