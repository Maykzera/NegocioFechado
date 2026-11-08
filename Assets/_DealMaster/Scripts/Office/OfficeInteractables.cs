using UnityEngine;

/// <summary>Mesa do jogador: sentar e usar o computador.</summary>
public class PlayerDesk : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform monitor;

    public Transform SeatPoint { get => seatPoint; set => seatPoint = value; }
    public Transform Monitor { get => monitor; set => monitor = value; }

    public string InteractionPrompt => GameServices.Game?.ActiveCall != null ? "Voltar para a ligação" : "Sentar e usar o computador";

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor)
    {
        var player = interactor.Player;
        if (player != null && seatPoint != null)
        {
            Vector3 look = (monitor != null ? monitor.position : seatPoint.position + seatPoint.forward) - seatPoint.position;
            player.Teleport(seatPoint.position, Quaternion.LookRotation(new Vector3(look.x, 0, look.z)).eulerAngles.y);
            player.SetPitch(12f);
        }
        GameUI.Open(new ComputerPanel());
    }
}

/// <summary>Mesa da supervisora.</summary>
public class SupervisorDesk : MonoBehaviour, IInteractable
{
    public string InteractionPrompt
    {
        get
        {
            int n = GameServices.Game?.PendingReviews.Count ?? 0;
            return n > 0 ? $"Falar com a supervisora ({n} ligação(ões) para avaliar)" : "Falar com a supervisora";
        }
    }

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor) => GameUI.Open(new SupervisorPanel());
}

/// <summary>Cafeteira da copa: recupera energia.</summary>
public class CoffeeMachine : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => $"Tomar um café ({Money.Format(CallCenterGame.CoffeePriceCents)}) · energia {GameServices.Game?.Energy ?? 0}%";

    public bool CanInteract(InteractionController interactor) => GameServices.IsReady;

    public void Interact(InteractionController interactor)
    {
        if (GameServices.Game.BuyCoffee(out var error)) { GameUI.Toast("Café quentinho. Energia recuperada."); CallAudio.Sale(); }
        else GameUI.Toast(error);
    }
}

/// <summary>Quadro de ranking do dia: colegas e você.</summary>
public class RankingBoard : MonoBehaviour
{
    [SerializeField] private TextMesh text;
    private static readonly string[] Names = { "Paula", "Jefferson", "Marta", "Kleber", "Simone", "Diego" };
    private int lastDay = -1;
    private int[] coworkerSales = new int[6];
    private float timer;

    public TextMesh Text { get => text; set => text = value; }

    private void Update()
    {
        var g = GameServices.Game;
        if (g == null || text == null) return;
        if (lastDay != g.Day)
        {
            lastDay = g.Day;
            for (int i = 0; i < coworkerSales.Length; i++) coworkerSales[i] = 0;
        }
        timer += Time.deltaTime;
        if (timer > 25f)
        {
            timer = 0;
            int i = Random.Range(0, coworkerSales.Length);
            if (Random.value < 0.35f) coworkerSales[i]++;
        }

        var rows = new System.Collections.Generic.List<(string name, int sales)>();
        for (int i = 0; i < Names.Length; i++) rows.Add((Names[i], coworkerSales[i]));
        rows.Add(("VOCÊ (" + g.PlayerName + ")", g.SalesToday));
        rows.Sort((a, b) => b.sales.CompareTo(a.sales));
        var sb = new System.Text.StringBuilder("RANKING DO DIA\n");
        for (int i = 0; i < rows.Count; i++) sb.Append($"{i + 1}. {rows[i].name}  {rows[i].sales}\n");
        sb.Append($"\nMETA: {g.Quota} vendas");
        text.text = sb.ToString();
    }
}
