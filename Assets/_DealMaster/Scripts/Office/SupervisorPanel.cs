using System;
using System.Linq;
using UnityEngine;

/// <summary>Sala da supervisora: avaliação das ligações (IA de verdade ou offline) e histórico de notas.</summary>
public class SupervisorPanel : IModalPanel
{
    private SupervisorReview showing;
    private bool history;
    private bool working;
    private string error;
    private Vector2 scroll;

    public string Title => "Supervisora Regina · Qualidade";

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        var g = GameServices.Game;
        if (GUI.Button(new Rect(area.x, area.y, 180, 36), "Avaliação", history ? UIStyles.Button : UIStyles.Selected)) history = false;
        if (GUI.Button(new Rect(area.x + 190, area.y, 180, 36), "Histórico", history ? UIStyles.Selected : UIStyles.Button)) history = true;
        var body = new Rect(area.x, area.y + 46, area.width, area.height - 46);

        if (history) { DrawHistory(body); return; }
        if (working) { GUI.Label(body, "A supervisora está ouvindo a gravação da ligação...", UIStyles.Text); return; }
        if (showing != null) { DrawReview(body, showing); return; }

        int n = g.PendingReviews.Count;
        GUI.Label(new Rect(body.x, body.y, body.width, 70), n == 0
            ? "\"Ainda não tenho ligações suas para ouvir. Vá para a sua mesa e comece a ligar.\""
            : $"\"Tenho {n} ligação(ões) suas gravadas. Vamos ouvir uma de cada vez.\"", UIStyles.Text);
        float y = body.y + 76;
        foreach (var r in g.PendingReviews.Take(6))
        {
            GUI.Label(new Rect(body.x, y, body.width, 26), $"• Dia {r.Day}: {r.LeadName} · {(r.SaleClosed ? $"venda de {Money.Format(r.SoldPriceCents)}" : "sem venda")}", UIStyles.Small);
            y += 28;
        }
        if (!string.IsNullOrEmpty(error)) GUI.Label(new Rect(body.x, y, body.width, 26), error, UIStyles.Bad);
        if (n > 0 && GUI.Button(new Rect(body.x, y + 30, 320, 48), "Ouvir a próxima ligação", UIStyles.Button)) Review();
    }

    private async void Review()
    {
        working = true;
        error = null;
        try { showing = await GameServices.Game.ReviewNextAsync(); }
        catch (Exception e) { error = e.Message; }
        finally { working = false; }
    }

    private void DrawReview(Rect r, SupervisorReview e)
    {
        float leftW = r.width * 0.48f;
        GUI.Label(new Rect(r.x, r.y, leftW, 28), $"{e.LeadName} · {(e.SaleClosed ? "com venda" : "sem venda")} · {(e.Source == "claude" ? "avaliação da IA" : "avaliação offline")}", UIStyles.Header);
        GUI.Label(new Rect(r.x, r.y + 30, leftW, 40), $"Nota: {e.Grade:0.0}/10", e.Approved ? UIStyles.Good : UIStyles.Bad);
        GUI.Label(new Rect(r.x, r.y + 64, leftW, 50), e.Verdict, UIStyles.Text);
        float y = r.y + 116;
        foreach (var c in e.Criteria)
        {
            GUI.Label(new Rect(r.x, y, 220, 24), c.Name, UIStyles.Small);
            UIStyles.Bar(new Rect(r.x + 225, y + 4, leftW - 290, 16), c.Score, c.Score >= 0.75f ? new Color(0.35f, 0.75f, 0.45f) : c.Score >= 0.45f ? new Color(0.9f, 0.7f, 0.3f) : new Color(0.9f, 0.4f, 0.35f));
            GUI.Label(new Rect(r.x + leftW - 58, y, 58, 24), $"{Mathf.RoundToInt(c.Score * 10)}/10", UIStyles.Small);
            y += 28;
        }
        if (e.ChargebackApplied) GUI.Label(new Rect(r.x, y + 6, leftW, 50), "A venda desta ligação foi CANCELADA e a comissão estornada por causa de informação falsa.", UIStyles.Bad);

        var right = new Rect(r.x + leftW + 20, r.y, r.width - leftW - 20, r.height - 56);
        float h = 60 + (e.Observations.Count + e.Suggestions.Count + e.LiesFound.Count) * 58;
        scroll = GUI.BeginScrollView(right, scroll, new Rect(0, 0, right.width - 20, h));
        float ry = 0;
        GUI.Label(new Rect(0, ry, right.width - 24, 26), "O que eu observei", UIStyles.Header); ry += 30;
        foreach (var o in e.Observations) { GUI.Label(new Rect(0, ry, right.width - 24, 56), "• " + o, UIStyles.Text); ry += 58; }
        foreach (var l in e.LiesFound) { GUI.Label(new Rect(0, ry, right.width - 24, 56), "✗ Mentira: " + l, UIStyles.Bad); ry += 58; }
        GUI.Label(new Rect(0, ry + 4, right.width - 24, 26), "Para a próxima", UIStyles.Header); ry += 34;
        foreach (var s in e.Suggestions) { GUI.Label(new Rect(0, ry, right.width - 24, 56), "→ " + s, UIStyles.Good); ry += 58; }
        GUI.EndScrollView();

        if (GUI.Button(new Rect(r.x, r.yMax - 48, 280, 44), GameServices.Game.PendingReviews.Count > 0 ? "Próxima ligação" : "Voltar", UIStyles.Button))
        {
            showing = null;
            if (GameServices.Game.PendingReviews.Count > 0) Review();
        }
    }

    private void DrawHistory(Rect r)
    {
        var reviews = GameServices.Game.Reviews;
        float avg = reviews.Count == 0 ? 0 : reviews.Skip(Math.Max(0, reviews.Count - 5)).Average(x => x.Grade);
        GUI.Label(new Rect(r.x, r.y, r.width, 28), $"Avaliações: {reviews.Count} · Média recente: {avg:0.0}", UIStyles.Header);
        if (reviews.Count == 0) return;

        var chart = new Rect(r.x, r.y + 36, r.width, 130);
        GUI.Box(chart, GUIContent.none);
        int shown = Mathf.Min(reviews.Count, 20);
        float bw = chart.width / 20f;
        for (int i = 0; i < shown; i++)
        {
            var e = reviews[reviews.Count - shown + i];
            float bh = (chart.height - 10) * e.Grade / 10f;
            var old = GUI.color;
            GUI.color = e.Approved ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.9f, 0.5f, 0.35f);
            GUI.DrawTexture(new Rect(chart.x + i * bw + 3, chart.yMax - bh - 5, bw - 6, bh), Texture2D.whiteTexture);
            GUI.color = old;
        }

        float y = chart.yMax + 10;
        scroll = GUI.BeginScrollView(new Rect(r.x, y, r.width, r.yMax - y), scroll, new Rect(0, 0, r.width - 20, reviews.Count * 32));
        for (int i = 0; i < reviews.Count; i++)
        {
            var e = reviews[reviews.Count - 1 - i];
            if (GUI.Button(new Rect(0, i * 32, r.width - 24, 30), $"Dia {e.Day} · {e.LeadName} · nota {e.Grade:0.0}{(e.LiesFound.Count > 0 ? " · mentira" : "")}", UIStyles.ListItem))
            { showing = e; history = false; }
        }
        GUI.EndScrollView();
    }
}
