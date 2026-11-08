using System.Linq;
using UnityEngine;

/// <summary>Painel financeiro: resumo, vendas, livro-caixa e reclamações a resolver.</summary>
public class FinancePanel : IModalPanel
{
    private enum Tab { Progress, Summary, Sales, Ledger, Complaints }

    private Tab tab;
    private Vector2 scroll;
    private string message;

    public string Title => "Painel do negócio";

    public FinancePanel()
    {
        tab = Tab.Progress;
        if (GameServices.Sales != null && GameServices.Sales.PendingComplaints().Any()) tab = Tab.Complaints;
    }

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        string[] names = { "Progresso", "Resumo", "Vendas", "Caixa", "Reclamações" };
        float w = area.width / names.Length;
        for (int i = 0; i < names.Length; i++)
            if (GUI.Button(new Rect(area.x + i * w, area.y, w - 6, 36), names[i], (int)tab == i ? UIStyles.Selected : UIStyles.Button))
            { tab = (Tab)i; scroll = Vector2.zero; message = null; }

        var body = new Rect(area.x, area.y + 46, area.width, area.height - 46);
        switch (tab)
        {
            case Tab.Progress: DrawProgress(body); break;
            case Tab.Summary: DrawSummary(body); break;
            case Tab.Sales: DrawSales(body); break;
            case Tab.Ledger: DrawLedger(body); break;
            case Tab.Complaints: DrawComplaints(body); break;
        }
        if (message != null) GUI.Label(new Rect(area.x, area.yMax - 30, area.width, 30), message, UIStyles.Bad);
    }

    private static void DrawProgress(Rect r)
    {
        var p = GameServices.Progression;
        GUI.Label(new Rect(r.x, r.y, r.width, 30), $"Nível {p.Level} · {p.Title}", UIStyles.Header);
        UIStyles.Bar(new Rect(r.x, r.y + 36, r.width * 0.5f, 22), p.ProgressToNext, new Color(0.4f, 0.6f, 0.95f));
        GUI.Label(new Rect(r.x + r.width * 0.5f + 10, r.y + 34, r.width * 0.5f, 26), $"{p.Xp} XP · faltam {p.XpToNext} para o nível {p.Level + 1}", UIStyles.Small);

        float y = r.y + 72;
        GUI.Label(new Rect(r.x, y, r.width * 0.5f, 26), "Missões", UIStyles.Header);
        float ly = y + 30;
        foreach (var m in GameServices.Missions.Missions)
        {
            string mark = m.Completed ? "✓" : m.Main ? "★" : "•";
            GUI.Label(new Rect(r.x, ly, r.width * 0.55f, 46), $"{mark} <b>{m.Title}</b> (+{m.RewardXp} XP)\n{m.Description}", m.Completed ? UIStyles.Good : UIStyles.Small);
            ly += 48;
        }

        float rx = r.x + r.width * 0.6f;
        GUI.Label(new Rect(rx, y, r.width * 0.4f, 26), "Próximos desbloqueios", UIStyles.Header);
        float ry = y + 30;
        foreach (var u in ProgressionService.Unlocks)
        {
            bool got = p.Level >= u.Level;
            GUI.Label(new Rect(rx, ry, r.width * 0.4f, 44), $"{(got ? "✓" : $"Nível {u.Level}:")} {u.Description}", got ? UIStyles.Good : UIStyles.Small);
            ry += 46;
        }
    }

    private static void DrawSummary(Rect r)
    {
        var ledger = GameServices.Ledger;
        var sales = GameServices.Sales;
        long stock = GameServices.Inventory.GetTotalStockValueCents();
        long profit = sales.Sales.Sum(s => s.ProfitCents);
        float y = r.y;

        void Line(string label, string value, GUIStyle style = null)
        {
            GUI.Label(new Rect(r.x, y, 320, 30), label, UIStyles.Text);
            GUI.Label(new Rect(r.x + 330, y, r.width - 330, 30), value, style ?? UIStyles.Money);
            y += 34;
        }

        Line("Caixa atual", CustomerBrain.FormatReais(GameServices.Wallet.BalanceCents));
        Line("Valor em estoque (custo)", CustomerBrain.FormatReais(stock));
        Line("Receita de vendas", CustomerBrain.FormatReais(ledger.Revenue));
        Line("Compras de mercadoria", CustomerBrain.FormatReais(ledger.PurchasesCost));
        Line("Despesas (embalagem e taxas)", CustomerBrain.FormatReais(ledger.Expenses));
        Line("Lucro direto das vendas", CustomerBrain.FormatReais(profit), profit >= 0 ? UIStyles.Good : UIStyles.Bad);
        Line("Fluxo de caixa", CustomerBrain.FormatReais(ledger.CashFlow), ledger.CashFlow >= 0 ? UIStyles.Good : UIStyles.Bad);
        Line("Vendas realizadas", sales.Sales.Count.ToString(), UIStyles.Text);
        Line("Reputação", $"{sales.Reputation:0}/100", UIStyles.Text);
    }

    private void DrawSales(Rect r)
    {
        var list = GameServices.Sales.Sales.Reverse().ToList();
        if (list.Count == 0) { GUI.Label(r, "Nenhuma venda ainda.", UIStyles.Small); return; }
        float rowH = 56;
        scroll = GUI.BeginScrollView(r, scroll, new Rect(0, 0, r.width - 20, list.Count * rowH));
        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            float y = i * rowH;
            GUI.Label(new Rect(0, y, r.width - 220, 28), $"#{s.SaleId} · Dia {s.Day} · {ProductDatabase.NameOf(s.ProductId)} para {s.CustomerName}", UIStyles.Text);
            GUI.Label(new Rect(0, y + 26, r.width - 220, 26),
                $"preço {CustomerBrain.FormatReais(s.PriceCents)} · embalagem {(s.Packed ? $"{s.PackagingScore * 10:0.0}/10" : "nenhuma")}" + (s.Damaged ? " · AVARIADO" : ""),
                UIStyles.Small);
            GUI.Label(new Rect(r.width - 210, y + 10, 190, 30), CustomerBrain.FormatReais(s.ProfitCents), s.ProfitCents >= 0 ? UIStyles.Good : UIStyles.Bad);
        }
        GUI.EndScrollView();
    }

    private void DrawLedger(Rect r)
    {
        var entries = GameServices.Ledger.Entries.Reverse().ToList();
        if (entries.Count == 0) { GUI.Label(r, "Sem movimentações.", UIStyles.Small); return; }
        float rowH = 30;
        scroll = GUI.BeginScrollView(r, scroll, new Rect(0, 0, r.width - 20, entries.Count * rowH));
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            GUI.Label(new Rect(0, i * rowH, r.width - 200, rowH), $"Dia {e.Day} · {e.Description}", UIStyles.Small);
            GUI.Label(new Rect(r.width - 190, i * rowH, 170, rowH), CustomerBrain.FormatReais(e.AmountCents), e.AmountCents >= 0 ? UIStyles.Good : UIStyles.Bad);
        }
        GUI.EndScrollView();
    }

    private void DrawComplaints(Rect r)
    {
        var pending = GameServices.Sales.PendingComplaints().ToList();
        if (pending.Count == 0) { GUI.Label(r, "Nenhuma reclamação pendente.", UIStyles.Good); return; }

        float rowH = 110;
        scroll = GUI.BeginScrollView(r, scroll, new Rect(0, 0, r.width - 20, pending.Count * rowH));
        for (int i = 0; i < pending.Count; i++)
        {
            var s = pending[i];
            float y = i * rowH;
            GUI.Box(new Rect(0, y, r.width - 24, rowH - 8), GUIContent.none);
            GUI.Label(new Rect(10, y + 6, r.width - 40, 50),
                $"{s.CustomerName}: \"O {ProductDatabase.NameOf(s.ProductId).ToLower()} chegou avariado!\"  (venda #{s.SaleId}, {CustomerBrain.FormatReais(s.PriceCents)})",
                UIStyles.Text);
            float bw = (r.width - 60) / 3;
            if (GUI.Button(new Rect(10, y + 56, bw, 40), "Reembolso total", UIStyles.Button)) Resolve(s, ComplaintResolution.FullRefund);
            if (GUI.Button(new Rect(20 + bw, y + 56, bw, 40), "Reembolsar metade", UIStyles.Button)) Resolve(s, ComplaintResolution.PartialRefund);
            if (GUI.Button(new Rect(30 + bw * 2, y + 56, bw, 40), "Recusar", UIStyles.Button)) Resolve(s, ComplaintResolution.Deny);
        }
        GUI.EndScrollView();
    }

    private void Resolve(SaleRecord sale, ComplaintResolution resolution)
    {
        if (!GameServices.Sales.ResolveComplaint(sale, resolution, out var error)) message = error;
        else if (resolution == ComplaintResolution.Deny) CustomerSpawner.Memory.RecordUnhappyExit(sale.CustomerName);
    }
}
