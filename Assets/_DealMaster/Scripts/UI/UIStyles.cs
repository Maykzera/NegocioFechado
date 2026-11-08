using UnityEngine;

/// <summary>Estilos compartilhados das telas (IMGUI).</summary>
public static class UIStyles
{
    private static GUIStyle text, small, header, moneyStyle, good, bad, button, selected, box;

    public static GUIStyle Text => Ensure(ref text, () => Label(18, Color.white));
    public static GUIStyle Small => Ensure(ref small, () => Label(15, new Color(0.75f, 0.78f, 0.85f)));
    public static GUIStyle Header => Ensure(ref header, () => { var s = Label(20, Color.white); s.fontStyle = FontStyle.Bold; return s; });
    public static GUIStyle MoneyStyle => Ensure(ref moneyStyle, () => { var s = Label(20, new Color(0.55f, 0.9f, 0.6f)); s.fontStyle = FontStyle.Bold; return s; });
    public static GUIStyle Good => Ensure(ref good, () => Label(18, new Color(0.5f, 0.9f, 0.55f)));
    public static GUIStyle Bad => Ensure(ref bad, () => Label(18, new Color(1f, 0.55f, 0.5f)));
    public static GUIStyle Button => Ensure(ref button, () => new GUIStyle(GUI.skin.button) { fontSize = 18 });

    public static GUIStyle Selected => Ensure(ref selected, () =>
    {
        var s = new GUIStyle(GUI.skin.button) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
        s.normal.background = GameHUDController.MakeTexture(new Color(0.25f, 0.4f, 0.7f, 1f));
        s.normal.textColor = Color.white;
        return s;
    });

    public static GUIStyle ListItem => Ensure(ref box, () => new GUIStyle(GUI.skin.button) { fontSize = 17, alignment = TextAnchor.MiddleLeft });

    private static GUIStyle Label(int size, Color color)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true, richText = true };
        s.normal.textColor = color;
        return s;
    }

    private static GUIStyle Ensure(ref GUIStyle field, System.Func<GUIStyle> make)
    {
        if (field == null) field = make();
        return field;
    }

    /// <summary>Barra de progresso simples (0 a 1).</summary>
    public static void Bar(Rect r, float value, Color color)
    {
        GUI.Box(r, GUIContent.none);
        var old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(value), r.height - 4), Texture2D.whiteTexture);
        GUI.color = old;
    }

    /// <summary>Campo de valor em reais com botões de ajuste. Retorna o novo valor em centavos.</summary>
    public static long PriceField(Rect r, long cents, long step = 100)
    {
        float bw = 52;
        if (GUI.Button(new Rect(r.x, r.y, bw, r.height), "-10", Button)) cents -= step * 10;
        if (GUI.Button(new Rect(r.x + bw + 4, r.y, bw, r.height), "-1", Button)) cents -= step;
        GUI.Label(new Rect(r.x + bw * 2 + 12, r.y, r.width - bw * 4 - 24, r.height), Money.Format(cents), MoneyStyle);
        if (GUI.Button(new Rect(r.xMax - bw * 2 - 4, r.y, bw, r.height), "+1", Button)) cents += step;
        if (GUI.Button(new Rect(r.xMax - bw, r.y, bw, r.height), "+10", Button)) cents += step * 10;
        return cents < 100 ? 100 : cents;
    }
}
