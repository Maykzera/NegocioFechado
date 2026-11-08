using UnityEngine;

/// <summary>Avança o relógio do expediente, mostra os avisos do jogo e fecha o dia às 18h.</summary>
public class CallCenterDirector : MonoBehaviour
{
    [SerializeField] private float gameMinutesPerSecond = 1f;

    private void Start()
    {
        var game = GameServices.Game;
        GameServices.Clock.GameMinutesPerSecond = gameMinutesPerSecond;
        game.OnNotice += msg => GameUI.Toast(msg, 4.5f);
        GameServices.Clock.OnDayEnded += _ =>
        {
            game.EndDay();
            if (SaveGameBridge.Instance != null) SaveGameBridge.Instance.SaveNow(false);
        };
        HUDExtensions.StatusText += Status;
    }

    private void OnDestroy()
    {
        HUDExtensions.StatusText -= Status;
    }

    private void Update()
    {
        var clock = GameServices.Clock;
        if (clock == null) return;
        // O tempo para no menu inicial, na sala do supervisor e enquanto a IA pensa; corre durante as ligações.
        clock.Paused = GameUI.Current is StartMenuPanel || GameUI.Current is SupervisorPanel;
        clock.Advance(Time.deltaTime);
    }

    private static string Status()
    {
        var g = GameServices.Game;
        var c = GameServices.Clock;
        if (g == null || c == null) return null;
        string line = $"Dia {c.Day} · {c.TimeText} · {CareerService.TitleFor(g.Level)} (nível {g.Level})\n" +
                      $"Vendas hoje {g.SalesToday}/{g.Quota} · Energia {g.Energy}%";
        if (g.PendingReviews.Count > 0) line += $"\nSupervisora: {g.PendingReviews.Count} ligação(ões) para avaliar";
        if (g.ActiveCall != null && g.ActiveCall.State == CallState.InCall) line += "\n<color=#7fe08f>● Em ligação</color>";
        return line;
    }
}
