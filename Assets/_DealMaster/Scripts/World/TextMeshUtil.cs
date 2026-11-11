using UnityEngine;

/// <summary>Configura TextMesh com a fonte embutida da Unity (placas e nomes dos NPCs).</summary>
public static class TextMeshUtil
{
    private static Font font;

    public static Font BuiltinFont
    {
        get
        {
            if (font != null) return font;
            // Unity 2022.2+ / Unity 6: "LegacyRuntime.ttf". Versões antigas: "Arial.ttf".
            foreach (var name in new[] { "LegacyRuntime.ttf", "Arial.ttf" })
            {
                try { font = Resources.GetBuiltinResource<Font>(name); }
                catch (System.Exception) { font = null; }
                if (font != null) break;
            }
            return font;
        }
    }

    public static void ApplyFont(TextMesh tm)
    {
        var f = BuiltinFont;
        if (tm == null || f == null) return;
        tm.font = f;
        var renderer = tm.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = f.material;
    }
}
