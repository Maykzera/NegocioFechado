using System.Collections.Generic;
using UnityEngine;

/// <summary>Uma tela modal desenhada pela GameUI (loja, negociação, embalagem, mestre...).</summary>
public interface IModalPanel
{
    string Title { get; }

    /// <summary>Desenha o conteúdo do painel dentro da área informada (coordenadas virtuais 1280x720).</summary>
    void DrawPanel(Rect area);

    void OnClosed();
}

/// <summary>
/// Controle central das telas: abre e fecha painéis, trava o cursor e mostra avisos rápidos.
/// A interface usa IMGUI com escala automática, o que funciona igual em PC e Android.
/// </summary>
public static class GameUI
{
    public const float VirtualHeight = 720f;

    private static readonly Stack<IModalPanel> panels = new Stack<IModalPanel>();
    private static readonly List<(string text, float until)> toasts = new List<(string, float)>();

    public static bool IsModalOpen => panels.Count > 0;
    public static IModalPanel Current => panels.Count > 0 ? panels.Peek() : null;

    public static float Scale => Screen.height / VirtualHeight;
    public static float VirtualWidth => Screen.width / Scale;

    public static IReadOnlyList<(string text, float until)> Toasts => toasts;

    public static void Open(IModalPanel panel)
    {
        if (panel == null) return;
        panels.Push(panel);
        LockCursor(false);
    }

    public static void Close()
    {
        if (panels.Count == 0) return;
        var p = panels.Pop();
        p.OnClosed();
        if (panels.Count == 0 && !Application.isMobilePlatform) LockCursor(true);
    }

    public static void CloseAll()
    {
        while (panels.Count > 0) Close();
    }

    public static void Toast(string message, float seconds = 3f)
    {
        toasts.Add((message, Time.unscaledTime + seconds));
        if (toasts.Count > 4) toasts.RemoveAt(0);
    }

    internal static void PruneToasts()
    {
        toasts.RemoveAll(t => t.until < Time.unscaledTime);
    }

    public static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
