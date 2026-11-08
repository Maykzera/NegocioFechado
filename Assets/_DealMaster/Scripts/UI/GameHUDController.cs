using UnityEngine;

/// <summary>
/// Desenha o HUD (dinheiro, mira, texto de interação, avisos) e o painel modal aberto.
/// </summary>
public class GameHUDController : MonoBehaviour
{
    [SerializeField] private InteractionController interaction;

    private GUIStyle label;
    private GUIStyle big;
    private GUIStyle prompt;
    private GUIStyle title;
    private GUIStyle panelBox;

    private void Awake()
    {
        if (interaction == null) interaction = FindObjectOfType<InteractionController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameUI.IsModalOpen) GameUI.Close();
            else GameUI.LockCursor(Cursor.lockState != CursorLockMode.Locked);
        }
        GameUI.PruneToasts();
    }

    private void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
        label.normal.textColor = Color.white;
        big = new GUIStyle(label) { fontSize = 26, fontStyle = FontStyle.Bold };
        prompt = new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        title = new GUIStyle(big) { fontSize = 24 };
        panelBox = new GUIStyle(GUI.skin.box);
        panelBox.normal.background = MakeTexture(new Color(0.08f, 0.09f, 0.12f, 0.94f));
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.matrix = Matrix4x4.Scale(new Vector3(GameUI.Scale, GameUI.Scale, 1f));
        float w = GameUI.VirtualWidth;
        float h = GameUI.VirtualHeight;

        DrawStatus();

        if (GameUI.IsModalOpen)
        {
            DrawPanel(w, h);
        }
        else
        {
            // Mira
            GUI.Label(new Rect(w / 2 - 10, h / 2 - 14, 20, 28), "+", prompt);

            string text = interaction != null ? interaction.CurrentPrompt : null;
            if (!string.IsNullOrEmpty(text))
            {
                string key = Application.isMobilePlatform ? "" : "[E] ";
                GUI.Label(new Rect(w / 2 - 300, h / 2 + 30, 600, 30), key + text, prompt);
            }
        }

        DrawToasts(w, h);
        GUI.matrix = Matrix4x4.identity;
    }

    private void DrawStatus()
    {
        if (!GameServices.IsReady) return;
        GUI.Label(new Rect(20, 14, 400, 34), CustomerBrain.FormatReais(GameServices.Wallet.BalanceCents), big);
        HUDExtensions.DrawStatusLines(new Rect(20, 50, 420, 120), label);
    }

    private void DrawPanel(float w, float h)
    {
        var panel = GameUI.Current;
        float pw = Mathf.Min(w - 40, 1000);
        float ph = h - 80;
        var area = new Rect((w - pw) / 2, 40, pw, ph);

        GUI.Box(area, GUIContent.none, panelBox);
        GUI.Label(new Rect(area.x + 20, area.y + 12, area.width - 140, 34), panel.Title, title);
        if (GUI.Button(new Rect(area.xMax - 110, area.y + 12, 90, 34), "Fechar"))
        {
            GameUI.Close();
            return;
        }

        panel.DrawPanel(new Rect(area.x + 20, area.y + 56, area.width - 40, area.height - 72));
    }

    private void DrawToasts(float w, float h)
    {
        float y = h - 60;
        var toasts = GameUI.Toasts;
        for (int i = toasts.Count - 1; i >= 0; i--)
        {
            GUI.Box(new Rect(w / 2 - 320, y, 640, 40), GUIContent.none, panelBox);
            GUI.Label(new Rect(w / 2 - 310, y + 4, 620, 32), toasts[i].text, prompt);
            y -= 46;
        }
    }

    public static Texture2D MakeTexture(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}

/// <summary>Linhas extras de status no HUD, preenchidas pelos sistemas das próximas etapas.</summary>
public static class HUDExtensions
{
    private static readonly System.Collections.Generic.List<System.Func<string>> providers =
        new System.Collections.Generic.List<System.Func<string>>();

    public static event System.Func<string> StatusText
    {
        add { if (value != null) providers.Add(value); }
        remove { providers.Remove(value); }
    }

    public static string Compose()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in providers)
        {
            string line = p();
            if (string.IsNullOrEmpty(line)) continue;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(line);
        }
        return sb.ToString();
    }

    public static void DrawStatusLines(Rect area, GUIStyle style)
    {
        string text = Compose();
        if (!string.IsNullOrEmpty(text)) GUI.Label(area, text, style);
    }
}
