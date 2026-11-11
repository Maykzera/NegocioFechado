using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// O computador virtual da mesa: discador, ligação (chat com o cliente), CRM, catálogo, metas, loja e configurações.
/// </summary>
public class ComputerPanel : IModalPanel
{
    private enum App { Dialer, Call, Crm, Catalog, Goals, Shop, Settings }

    private static readonly (App app, string label)[] Apps =
    {
        (App.Dialer, "Discador"), (App.Call, "Ligação"), (App.Crm, "CRM"), (App.Catalog, "Catálogo"),
        (App.Goals, "Metas"), (App.Shop, "Loja"), (App.Settings, "Configurações")
    };

    private static readonly string[] QuickLines =
    {
        "Bom dia! Meu nome é {nome}, falo da {empresa}. Tem um minutinho?",
        "Como está a sua situação hoje com isso?",
        "Entendo perfeitamente. Faz sentido a sua preocupação.",
        "Posso cadastrar para você?",
        "Peço desculpas pelo incômodo. Vou retirar seu número da nossa lista.",
        "Obrigado pelo seu tempo, tenha um bom dia!",
    };

    private App app = App.Dialer;
    private string input = "";
    private string notice;
    private Vector2 scrollA, scrollB, chatScroll;
    private bool showSaleForm;
    private string saleServiceId;
    private long salePrice;
    private CallReport viewingReport;
    private AISettings editing;
    private string playerNameField;
    private bool working;
    private int lastTranscriptCount;

    public string Title => $"{CallCenterGame.CompanyName} · Estação de {GameServices.Game.PlayerName}";

    public Action Closed;

    public ComputerPanel()
    {
        var call = GameServices.Game.ActiveCall;
        if (call != null) app = App.Call;
    }

    public void OnClosed() => Closed?.Invoke();

    public void DrawPanel(Rect area)
    {
        var g = GameServices.Game;
        float side = Mathf.Min(180, area.width * 0.22f);

        // Barra lateral de apps (como uma área de trabalho)
        GUI.Box(new Rect(area.x, area.y, side, area.height), GUIContent.none);
        float y = area.y + 8;
        foreach (var (a, label) in Apps)
        {
            string text = label;
            if (a == App.Call && g.ActiveCall != null && g.ActiveCall.State != CallState.Ended) text += " ●";
            if (GUI.Button(new Rect(area.x + 8, y, side - 16, 40), text, a == app ? UIStyles.Selected : UIStyles.ListItem)) { app = a; notice = null; viewingReport = null; }
            y += 46;
        }
        GUI.Label(new Rect(area.x + 8, area.yMax - 70, side - 16, 64),
            $"{GameServices.Clock.TimeText} · Dia {GameServices.Clock.Day}\n{Money.Format(GameServices.Wallet.BalanceCents)}\nEnergia {g.Energy}%", UIStyles.Small);

        var main = new Rect(area.x + side + 14, area.y, area.width - side - 14, area.height);
        switch (app)
        {
            case App.Dialer: DrawDialer(main); break;
            case App.Call: DrawCall(main); break;
            case App.Crm: DrawCrm(main); break;
            case App.Catalog: DrawCatalog(main); break;
            case App.Goals: DrawGoals(main); break;
            case App.Shop: DrawShop(main); break;
            case App.Settings: DrawSettings(main); break;
        }

        if (!string.IsNullOrEmpty(notice))
            GUI.Label(new Rect(main.x, main.yMax - 28, main.width, 28), notice, UIStyles.Bad);
    }

    // ------------------------------------------------------------------ Discador

    private void DrawDialer(Rect r)
    {
        var g = GameServices.Game;
        GUI.Label(new Rect(r.x, r.y, r.width, 28), $"Lista de ligações de hoje ({g.Queue.Count})", UIStyles.Header);
        GUI.Label(new Rect(r.x, r.y + 28, r.width, 24), "Você só sabe o básico de cada pessoa. O resto, descubra conversando.", UIStyles.Small);

        bool budgetHint = g.HasUpgrade(UpgradeCatalog.SecondMonitor);
        float rowH = budgetHint ? 78 : 62;
        var leads = g.Queue.ToList();
        scrollA = GUI.BeginScrollView(new Rect(r.x, r.y + 58, r.width, r.height - 90), scrollA, new Rect(0, 0, r.width - 20, leads.Count * (rowH + 6)));
        for (int i = 0; i < leads.Count; i++)
        {
            var l = leads[i];
            float y = i * (rowH + 6);
            GUI.Box(new Rect(0, y, r.width - 24, rowH), GUIContent.none);
            GUI.Label(new Rect(10, y + 4, r.width - 200, 26), $"<b>{l.Name}</b>, {l.Age} anos · {l.Phone}", UIStyles.Text);
            GUI.Label(new Rect(10, y + 30, r.width - 200, 26), $"{l.Occupation} · {l.City}", UIStyles.Small);
            if (budgetHint)
                GUI.Label(new Rect(10, y + 52, r.width - 200, 24), $"Faixa de orçamento: {Money.Format(l.BudgetCents / 1000 * 1000)} a {Money.Format((l.BudgetCents / 1000 + 1) * 1000)} por mês", UIStyles.Good);
            GUI.enabled = !working;
            if (GUI.Button(new Rect(r.width - 170, y + 10, 140, 42), "Ligar", UIStyles.Button)) Run(() => Dial(l));
            GUI.enabled = true;
        }
        GUI.EndScrollView();
    }

    private async Task Dial(Lead lead)
    {
        string block = GameServices.Game.CanCall(lead);
        if (block != null) { notice = block; return; }
        app = App.Call;
        input = "";
        showSaleForm = false;
        CallAudio.Dial();
        var call = await GameServices.Game.StartCallAsync(lead);
        if (call == null) return;
        if (call.State == CallState.Ended) { notice = $"{lead.Name} não atendeu."; CallAudio.HangUp(); app = App.Dialer; }
        else CallAudio.PickUp();
    }

    // ------------------------------------------------------------------ Ligação

    private void DrawCall(Rect r)
    {
        var g = GameServices.Game;
        var call = g.ActiveCall;
        if (call == null)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 60), "Nenhuma ligação ativa. Abra o Discador e escolha alguém para ligar.", UIStyles.Text);
            return;
        }

        var l = call.Lead;
        var turn = call.LastTurn;
        string state = call.State == CallState.Ringing ? "Chamando..." : call.State == CallState.InCall ? (call.Busy ? "Cliente falando..." : "Em ligação") : OutcomeText(call.Outcome);
        GUI.Label(new Rect(r.x, r.y, r.width * 0.6f, 28), $"<b>{l.Name}</b> · {l.Phone}", UIStyles.Text);
        GUI.Label(new Rect(r.x, r.y + 26, r.width * 0.6f, 24), $"{state}   ·   {(turn?.Source == "claude" ? "IA de verdade" : "cliente offline")}", UIStyles.Small);

        float bx = r.x + r.width * 0.62f, bw = r.width * 0.38f;
        if (turn != null)
        {
            Meter(bx, r.y, bw, "Interesse", turn.Interest / 100f, new Color(0.95f, 0.7f, 0.25f));
            Meter(bx, r.y + 24, bw, "Confiança", turn.Trust / 100f, new Color(0.4f, 0.65f, 0.95f));
        }

        // Conversa
        float chatTop = r.y + 56, inputH = 44, bottomH = showSaleForm ? 150 : 96;
        var chat = new Rect(r.x, chatTop, r.width, r.height - (chatTop - r.y) - bottomH - inputH);
        GUI.Box(chat, GUIContent.none);
        float lineH = 54;
        var lines = call.Transcript;
        if (lines.Count != lastTranscriptCount) { chatScroll.y = float.MaxValue; lastTranscriptCount = lines.Count; }
        chatScroll = GUI.BeginScrollView(new Rect(chat.x + 6, chat.y + 6, chat.width - 12, chat.height - 12), chatScroll, new Rect(0, 0, chat.width - 36, Mathf.Max(lines.Count * lineH, chat.height - 12)));
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            string who = line.FromPlayer ? $"<color=#8ec5ff><b>Você:</b></color>" : $"<color=#ffd27a><b>{l.Name.Split(' ')[0]}:</b></color>";
            GUI.Label(new Rect(0, i * lineH, chat.width - 40, lineH), $"{who} {line.Text}", UIStyles.Text);
        }
        GUI.EndScrollView();

        float y = chat.yMax + 6;
        if (call.State == CallState.InCall)
        {
            GUI.SetNextControlName("callInput");
            bool enter = Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter) && GUI.GetNameOfFocusedControl() == "callInput";
            input = GUI.TextField(new Rect(r.x, y, r.width - 130, inputH - 6), input ?? "", 600, Styles.Input);
            GUI.enabled = !call.Busy && !string.IsNullOrWhiteSpace(input);
            if (GUI.Button(new Rect(r.xMax - 120, y, 120, inputH - 6), "Falar", UIStyles.Button) || (enter && GUI.enabled)) Send(call);
            GUI.enabled = true;
            y += inputH;

            // Frases prontas (ajudam no celular)
            float qw = (r.width - 5 * 6) / 6;
            for (int i = 0; i < QuickLines.Length; i++)
            {
                string label = new[] { "Apresentar", "Perguntar", "Entender", "Fechar", "Tirar da lista", "Despedir" }[i];
                if (GUI.Button(new Rect(r.x + i * (qw + 6), y, qw, 34), label, UIStyles.ListItem))
                    input = QuickLines[i].Replace("{nome}", GameServices.Game.PlayerName).Replace("{empresa}", CallCenterGame.CompanyName);
            }
            y += 42;
        }

        if (showSaleForm) { DrawSaleForm(new Rect(r.x, y, r.width, 104)); y += 108; }

        float bw2 = 220;
        if (call.State == CallState.InCall)
        {
            GUI.enabled = !call.SaleRegistered;
            if (GUI.Button(new Rect(r.x, y, bw2, 40), call.SaleRegistered ? "Venda registrada ✓" : "Registrar venda no CRM", UIStyles.Button))
            {
                showSaleForm = !showSaleForm;
                var agreement = call.Agreement;
                saleServiceId = agreement?.AcceptedServiceId ?? call.FocusServiceId ?? saleServiceId;
                var svc = ServiceCatalog.Get(saleServiceId);
                salePrice = agreement?.AcceptedPriceCents ?? svc?.MonthlyPriceCents ?? 0;
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(r.x + bw2 + 10, y, 160, 40), "Desligar", UIStyles.Button)) { call.HangUp(); CallAudio.HangUp(); }
        }
        else
        {
            if (!string.IsNullOrEmpty(call.LastAiError)) GUI.Label(new Rect(r.x, y - 26, r.width, 24), "A IA falhou e o cliente offline assumiu: " + call.LastAiError, UIStyles.Bad);
            if (GUI.Button(new Rect(r.x, y, 300, 40), "Arquivar e voltar ao discador", UIStyles.Button))
            {
                GameServices.Game.FinishCall(call);
                app = App.Dialer;
                showSaleForm = false;
            }
        }
    }

    private void Send(CallSession call)
    {
        string text = input;
        input = "";
        GUI.FocusControl("callInput");
        Run(async () =>
        {
            var t = await call.SendAsync(text);
            if (t != null && (t.Decision == CustomerDecision.HungUp || t.Decision == CustomerDecision.Refused)) CallAudio.HangUp();
            if (t != null && t.Decision == CustomerDecision.WantsToBuy) GameUI.Toast("O cliente aceitou! Registre a venda no CRM.", 4f);
        });
    }

    private void DrawSaleForm(Rect r)
    {
        var g = GameServices.Game;
        GUI.Box(r, GUIContent.none);
        GUI.Label(new Rect(r.x + 8, r.y + 4, r.width, 24), "Serviço vendido:", UIStyles.Small);
        var services = ServiceCatalog.UnlockedAt(g.Level).ToList();
        float bw = Mathf.Min(170, (r.width - 16) / Mathf.Max(1, services.Count) - 4);
        for (int i = 0; i < services.Count; i++)
            if (GUI.Button(new Rect(r.x + 8 + i * (bw + 4), r.y + 28, bw, 30), services[i].Company, services[i].Id == saleServiceId ? UIStyles.Selected : UIStyles.ListItem))
            {
                saleServiceId = services[i].Id;
                salePrice = services[i].MonthlyPriceCents;
            }
        GUI.Label(new Rect(r.x + 8, r.y + 66, 160, 30), "Mensalidade:", UIStyles.Small);
        salePrice = UIStyles.PriceField(new Rect(r.x + 120, r.y + 64, 360, 34), salePrice, 10);
        if (GUI.Button(new Rect(r.xMax - 180, r.y + 64, 170, 34), "Confirmar venda", UIStyles.Button))
        {
            if (g.TryRegisterSale(saleServiceId, salePrice, out var error)) { showSaleForm = false; notice = null; CallAudio.Sale(); }
            else notice = error;
        }
    }

    private static string OutcomeText(CallOutcome o)
    {
        switch (o)
        {
            case CallOutcome.NoAnswer: return "Ninguém atendeu";
            case CallOutcome.HungUp: return "O cliente desligou";
            case CallOutcome.Refused: return "O cliente encerrou a ligação";
            case CallOutcome.Sale: return "Ligação encerrada com venda";
            default: return "Ligação encerrada";
        }
    }

    // ------------------------------------------------------------------ CRM

    private void DrawCrm(Rect r)
    {
        var g = GameServices.Game;
        if (viewingReport != null)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 28), $"{viewingReport.LeadName} · {OutcomeText(viewingReport.Outcome)}", UIStyles.Header);
            scrollB = GUI.BeginScrollView(new Rect(r.x, r.y + 34, r.width, r.height - 84), scrollB, new Rect(0, 0, r.width - 20, 2000));
            GUI.Label(new Rect(0, 0, r.width - 30, 2000), viewingReport.TranscriptText("Você", viewingReport.LeadName.Split(' ')[0]), UIStyles.Text);
            GUI.EndScrollView();
            if (GUI.Button(new Rect(r.x, r.yMax - 44, 160, 40), "Voltar", UIStyles.Button)) viewingReport = null;
            return;
        }

        GUI.Label(new Rect(r.x, r.y, r.width, 28), $"Histórico de ligações ({g.Reports.Count})", UIStyles.Header);
        var reports = g.Reports.Reverse().ToList();
        scrollB = GUI.BeginScrollView(new Rect(r.x, r.y + 34, r.width, r.height - 34), scrollB, new Rect(0, 0, r.width - 20, reports.Count * 52));
        for (int i = 0; i < reports.Count; i++)
        {
            var rep = reports[i];
            var sale = g.Sales.ForCall(rep.CallId);
            string s = sale == null ? "" : sale.ChargedBack ? " · venda CANCELADA" : $" · venda {Money.Format(sale.PriceCents)}";
            if (GUI.Button(new Rect(0, i * 52, r.width - 24, 46), $"Dia {rep.Day} · {rep.LeadName} · {OutcomeText(rep.Outcome)}{s}", UIStyles.ListItem))
                viewingReport = rep;
        }
        GUI.EndScrollView();
    }

    // ------------------------------------------------------------------ Catálogo

    private void DrawCatalog(Rect r)
    {
        int level = GameServices.Game.Level;
        GUI.Label(new Rect(r.x, r.y, r.width, 28), "Catálogo de serviços (consulte antes de prometer qualquer coisa)", UIStyles.Header);
        float h = 0;
        foreach (var s in ServiceCatalog.All) h += 40 + 26 * (s.Facts.Count + s.Lies.Count + 2);
        scrollA = GUI.BeginScrollView(new Rect(r.x, r.y + 34, r.width, r.height - 34), scrollA, new Rect(0, 0, r.width - 20, h));
        float y = 0;
        foreach (var s in ServiceCatalog.All)
        {
            bool locked = s.RequiredLevel > level;
            GUI.Label(new Rect(0, y, r.width - 30, 30), $"<b>{s.Name}</b>" + (locked ? $"  <color=#ff8a72>(nível {s.RequiredLevel})</color>" : ""), UIStyles.Text);
            y += 30;
            GUI.Label(new Rect(10, y, r.width - 40, 26),
                $"{Money.Format(s.MonthlyPriceCents)}/mês · mínimo autorizado {Money.Format(s.MinPriceCents)} · " +
                (s.FidelityMonths > 0 ? $"fidelidade {s.FidelityMonths} meses" : "sem fidelidade") + $" · comissão {s.CommissionRate:0.#}x a mensalidade", UIStyles.Small);
            y += 26;
            foreach (var f in s.Facts) { GUI.Label(new Rect(10, y, r.width - 40, 26), "✓ " + f.Text, UIStyles.Good); y += 26; }
            foreach (var l in s.Lies) { GUI.Label(new Rect(10, y, r.width - 40, 26), "✗ Não prometa: " + l.Text, UIStyles.Bad); y += 26; }
            y += 10;
        }
        GUI.EndScrollView();
    }

    // ------------------------------------------------------------------ Metas

    private void DrawGoals(Rect r)
    {
        var g = GameServices.Game;
        int lv = g.Level;
        float y = r.y;
        GUI.Label(new Rect(r.x, y, r.width, 30), $"{CareerService.TitleFor(lv)} · nível {lv}", UIStyles.Header); y += 32;
        int cur = CareerService.XpForLevel(lv), next = CareerService.XpForLevel(lv + 1);
        UIStyles.Bar(new Rect(r.x, y, r.width * 0.5f, 18), (g.Career.Xp - cur) / (float)(next - cur), new Color(0.4f, 0.6f, 0.95f));
        GUI.Label(new Rect(r.x + r.width * 0.5f + 10, y - 4, r.width * 0.5f, 26), $"{g.Career.Xp} XP · faltam {next - g.Career.Xp}", UIStyles.Small); y += 30;
        GUI.Label(new Rect(r.x, y, r.width, 28), $"Meta de hoje: {g.SalesToday}/{g.Quota} vendas · Ganhos hoje {Money.Format(g.Ledger.SumOnDay(g.Day))} · Total líquido {Money.Format(g.Ledger.NetEarnings)}", UIStyles.Text); y += 36;

        GUI.Label(new Rect(r.x, y, r.width, 26), "Missões", UIStyles.Header); y += 28;
        foreach (var m in MissionBoard.All)
        {
            bool done = g.Missions.IsDone(m.Id);
            GUI.Label(new Rect(r.x, y, r.width, 44), $"{(done ? "✓" : m.Main ? "★" : "•")} <b>{m.Title}</b> (+{m.RewardXp} XP) — {m.Description}", done ? UIStyles.Good : UIStyles.Small);
            y += 40;
        }
    }

    // ------------------------------------------------------------------ Loja

    private void DrawShop(Rect r)
    {
        var g = GameServices.Game;
        GUI.Label(new Rect(r.x, r.y, r.width, 28), "Melhorias para a sua estação", UIStyles.Header);
        float y = r.y + 36;
        foreach (var u in UpgradeCatalog.All)
        {
            bool owned = g.HasUpgrade(u.Id), locked = g.Level < u.RequiredLevel;
            GUI.Box(new Rect(r.x, y, r.width, 70), GUIContent.none);
            GUI.Label(new Rect(r.x + 10, y + 4, r.width - 220, 28), $"<b>{u.Name}</b> · {Money.Format(u.PriceCents)}" + (locked ? $" · nível {u.RequiredLevel}" : ""), UIStyles.Text);
            GUI.Label(new Rect(r.x + 10, y + 34, r.width - 220, 30), u.Description, UIStyles.Small);
            GUI.enabled = !owned && !locked;
            if (GUI.Button(new Rect(r.xMax - 190, y + 14, 170, 42), owned ? "Comprado ✓" : "Comprar", UIStyles.Button))
                if (!g.BuyUpgrade(u.Id, out var e)) notice = e;
            GUI.enabled = true;
            y += 78;
        }
        if (GUI.Button(new Rect(r.x, y + 6, 260, 42), g.HasUpgrade(UpgradeCatalog.CoffeeMaker) ? "Tomar café (grátis)" : $"Tomar café ({Money.Format(CallCenterGame.CoffeePriceCents)})", UIStyles.Button))
            if (!g.BuyCoffee(out var e)) notice = e; else CallAudio.Sale();
    }

    // ------------------------------------------------------------------ Configurações

    private void DrawSettings(Rect r)
    {
        var current = GameServices.AI;
        if (editing == null)
        {
            editing = new AISettings { UseRealAI = current.UseRealAI, ApiKey = current.ApiKey, Model = current.Model };
            playerNameField = GameServices.Game.PlayerName;
        }
        float y = r.y;
        GUI.Label(new Rect(r.x, y, r.width, 28), "Configurações", UIStyles.Header); y += 36;

        GUI.Label(new Rect(r.x, y, 200, 30), "Seu nome no jogo:", UIStyles.Text);
        playerNameField = GUI.TextField(new Rect(r.x + 200, y, 280, 32), playerNameField ?? "", 30, Styles.Input); y += 44;

        editing.UseRealAI = GUI.Toggle(new Rect(r.x, y, r.width, 30), editing.UseRealAI, "  Usar IA de verdade (Claude) nos clientes e na supervisora", UIStyles.Text); y += 38;
        GUI.Label(new Rect(r.x, y, 200, 30), "Chave da API:", UIStyles.Text);
        editing.ApiKey = GUI.PasswordField(new Rect(r.x + 200, y, r.width - 200, 32), editing.ApiKey ?? "", '•', 300, Styles.Input); y += 40;
        GUI.Label(new Rect(r.x, y, r.width, 46), "Crie a chave em console.anthropic.com. Ela fica salva só neste aparelho. Cada fala do cliente usa a API e tem custo.", UIStyles.Small); y += 50;

        GUI.Label(new Rect(r.x, y, 200, 30), "Modelo:", UIStyles.Text);
        editing.Model = GUI.TextField(new Rect(r.x + 200, y, 300, 32), editing.Model ?? "", 60, Styles.Input); y += 40;
        string[] presets = { "claude-opus-5-5", "claude-sonnet-5-5", "claude-haiku-5-5" };
        string[] presetLabels = { "Opus 5.5 (melhor)", "Sonnet 5.5", "Haiku 5.5 (mais barato)" };
        for (int i = 0; i < presets.Length; i++)
            if (GUI.Button(new Rect(r.x + 200 + i * 205, y, 200, 34), presetLabels[i], editing.Model == presets[i] ? UIStyles.Selected : UIStyles.ListItem)) editing.Model = presets[i];
        y += 46;

        GUI.Label(new Rect(r.x, y, r.width, 50), ClaudeAvailability.Status(editing), editing.CanUseRealAI ? UIStyles.Good : UIStyles.Small); y += 56;

        if (GUI.Button(new Rect(r.x, y, 200, 42), "Salvar", UIStyles.Button))
        {
            AISettingsStore.Save(editing);
            AISettingsStore.SavePlayerName(playerNameField);
            current.UseRealAI = editing.UseRealAI; current.ApiKey = editing.ApiKey; current.Model = editing.Model;
            GameServices.Game.PlayerName = string.IsNullOrWhiteSpace(playerNameField) ? "Lucas" : playerNameField.Trim();
            GameBootstrap.ConfigureAI(GameServices.Game, current);
            GameUI.Toast("Configurações salvas. Valem a partir da próxima ligação.");
        }
        if (SaveGameBridge.Instance != null && GUI.Button(new Rect(r.x + 210, y, 200, 42), "Salvar o jogo", UIStyles.Button)) SaveGameBridge.Instance.SaveNow();
    }

    // ------------------------------------------------------------------ utilitários

    private static void Meter(float x, float y, float w, string label, float v, Color c)
    {
        GUI.Label(new Rect(x, y - 2, 100, 22), label, UIStyles.Small);
        UIStyles.Bar(new Rect(x + 100, y + 2, w - 150, 16), v, c);
        GUI.Label(new Rect(x + w - 46, y - 2, 46, 22), $"{Mathf.RoundToInt(v * 100)}%", UIStyles.Small);
    }

    private void Run(Func<Task> action)
    {
        if (working) return;
        working = true;
        Exec();

        async void Exec()
        {
            try { await action(); }
            catch (Exception e) { notice = "Erro: " + e.Message; Debug.LogException(e); }
            finally { working = false; }
        }
    }

    private static class Styles
    {
        private static GUIStyle input;
        public static GUIStyle Input => input ?? (input = new GUIStyle(GUI.skin.textField) { fontSize = 18, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6) });
    }
}
