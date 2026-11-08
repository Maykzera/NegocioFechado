using System.Linq;
using UnityEngine;

/// <summary>Tela do mestre negociador: análise das negociações pendentes e histórico de notas.</summary>
public class MentorPanel : IModalPanel
{
    private readonly MentorService mentor;
    private MentorEvaluation showing;
    private bool historyTab;
    private Vector2 scroll;

    public string Title => "Mestre Negociador";

    public MentorPanel(MentorService mentor)
    {
        this.mentor = mentor;
    }

    public void OnClosed() { }

    public void DrawPanel(Rect area)
    {
        if (GUI.Button(new Rect(area.x, area.y, 200, 36), "Análise", historyTab ? UIStyles.Button : UIStyles.Selected)) historyTab = false;
        if (GUI.Button(new Rect(area.x + 210, area.y, 200, 36), "Histórico", historyTab ? UIStyles.Selected : UIStyles.Button)) historyTab = true;
        var body = new Rect(area.x, area.y + 46, area.width, area.height - 46);
        if (historyTab) DrawHistory(body);
        else if (showing != null) DrawEvaluation(body, showing);
        else DrawIntro(body);
    }

    private void DrawIntro(Rect r)
    {
        int n = mentor.Pending.Count;
        string intro = n == 0
            ? "\"Ainda não vi nenhuma negociação sua. Vá até a praça, converse com os clientes e volte aqui.\""
            : $"\"Acompanhei {n} negociação(ões) suas. Vamos analisar uma de cada vez.\"";
        GUI.Label(new Rect(r.x, r.y, r.width, 70), intro, UIStyles.Text);

        float y = r.y + 80;
        foreach (var rec in mentor.Pending.Take(6))
        {
            string result = rec.SaleClosed ? $"venda por {CustomerBrain.FormatReais(rec.AgreedPriceCents)}" : "sem venda";
            GUI.Label(new Rect(r.x, y, r.width, 28), $"• Dia {rec.Day}: {rec.CustomerName} · {ProductDatabase.NameOf(rec.ProductId) ?? "nenhum produto"} · {result}", UIStyles.Small);
            y += 30;
        }

        if (n > 0 && GUI.Button(new Rect(r.x, y + 10, 300, 48), "Analisar próxima negociação", UIStyles.Button))
            showing = mentor.ReviewNext();
    }

    private void DrawEvaluation(Rect r, MentorEvaluation e)
    {
        float leftW = r.width * 0.5f;
        GUI.Label(new Rect(r.x, r.y, leftW, 30), $"{e.CustomerName} · {ProductDatabase.NameOf(e.ProductId)}", UIStyles.Header);
        GUI.Label(new Rect(r.x, r.y + 30, leftW, 50), $"Nota: {e.Grade:0.0}/10", e.Grade >= 7f ? UIStyles.Good : UIStyles.Bad);
        GUI.Label(new Rect(r.x, r.y + 60, leftW, 30), e.Verdict, UIStyles.Text);

        float y = r.y + 100;
        foreach (var c in e.Criteria)
        {
            GUI.Label(new Rect(r.x, y, 230, 24), c.Name, UIStyles.Small);
            UIStyles.Bar(new Rect(r.x + 235, y + 4, leftW - 300, 16), c.Value,
                c.Value >= 0.75f ? new Color(0.35f, 0.75f, 0.45f) : c.Value >= 0.45f ? new Color(0.9f, 0.7f, 0.3f) : new Color(0.9f, 0.4f, 0.35f));
            GUI.Label(new Rect(r.x + leftW - 58, y, 58, 24), $"{(int)(c.Value * 100)}%", UIStyles.Small);
            y += 28;
        }

        var right = new Rect(r.x + leftW + 20, r.y, r.width - leftW - 20, r.height);
        scroll = GUI.BeginScrollView(right, scroll, new Rect(0, 0, right.width - 20, 60 + (e.Observations.Count + e.Suggestions.Count) * 56 + 80));
        float ry = 0;
        GUI.Label(new Rect(0, ry, right.width - 24, 26), "O que eu observei", UIStyles.Header); ry += 30;
        foreach (var o in e.Observations) { GUI.Label(new Rect(0, ry, right.width - 24, 54), "• " + o, UIStyles.Text); ry += 56; }
        GUI.Label(new Rect(0, ry + 6, right.width - 24, 26), "Para a próxima", UIStyles.Header); ry += 36;
        foreach (var s in e.Suggestions) { GUI.Label(new Rect(0, ry, right.width - 24, 54), "→ " + s, UIStyles.Good); ry += 56; }
        GUI.EndScrollView();

        if (GUI.Button(new Rect(r.x, r.yMax - 48, 260, 44), mentor.Pending.Count > 0 ? "Próxima negociação" : "Voltar", UIStyles.Button))
            showing = mentor.Pending.Count > 0 ? mentor.ReviewNext() : null;
    }

    private void DrawHistory(Rect r)
    {
        var h = mentor.History;
        GUI.Label(new Rect(r.x, r.y, r.width, 30), $"Avaliações: {h.Count}  ·  Média recente: {mentor.RecentAverage():0.0}", UIStyles.Header);
        if (h.Count == 0) return;

        // Gráfico simples de evolução
        var chart = new Rect(r.x, r.y + 40, r.width, 140);
        GUI.Box(chart, GUIContent.none);
        int shown = Mathf.Min(h.Count, 20);
        float bw = chart.width / 20f;
        for (int i = 0; i < shown; i++)
        {
            var e = h[h.Count - shown + i];
            float bh = (chart.height - 10) * e.Grade / 10f;
            var old = GUI.color;
            GUI.color = e.Grade >= 7f ? new Color(0.35f, 0.75f, 0.45f) : new Color(0.9f, 0.55f, 0.35f);
            GUI.DrawTexture(new Rect(chart.x + i * bw + 3, chart.yMax - bh - 5, bw - 6, bh), Texture2D.whiteTexture);
            GUI.color = old;
        }

        float y = chart.yMax + 10;
        scroll = GUI.BeginScrollView(new Rect(r.x, y, r.width, r.yMax - y), scroll, new Rect(0, 0, r.width - 20, h.Count * 30));
        for (int i = 0; i < h.Count; i++)
        {
            var e = h[h.Count - 1 - i];
            if (GUI.Button(new Rect(0, i * 30, r.width - 24, 28),
                    $"Dia {e.Day} · {e.CustomerName} · {ProductDatabase.NameOf(e.ProductId)} · nota {e.Grade:0.0}", UIStyles.ListItem))
            {
                showing = e;
                historyTab = false;
            }
        }
        GUI.EndScrollView();
    }
}
