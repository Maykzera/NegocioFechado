using System.Linq;
using UnityEngine;

/// <summary>
/// Conecta os eventos do jogo à XP, às missões e aos desbloqueios; avança o relógio
/// e cuida do fim do expediente (aluguel do depósito e reposição dos fornecedores).
/// </summary>
public class ProgressionBridge : MonoBehaviour
{
    [SerializeField] private long dailyRentCents = 2000;
    [SerializeField] private float gameMinutesPerSecond = 2f;

    private int salesToday;
    private long profitToday;

    private void Start()
    {
        var progression = GameServices.Progression;
        GameServices.Clock.GameMinutesPerSecond = gameMinutesPerSecond;

        GameServices.Purchases.OnPurchaseCompleted += r => { progression.AddXp(ProgressionService.XpForPurchase(r), "compra"); CheckMissions(); };
        GameServices.Packaging.OnPacked += o => { progression.AddXp(ProgressionService.XpForPackaging(o.Score), "embalagem"); CheckMissions(); };
        GameServices.Sales.OnSaleCompleted += s =>
        {
            salesToday++;
            profitToday += s.ProfitCents;
            progression.AddXp(ProgressionService.XpForSale(s), "venda");
            CheckMissions();
        };
        MentorDesk.Service.OnEvaluated += e => { progression.AddXp(ProgressionService.XpForEvaluation(e), "avaliação"); CheckMissions(); };

        progression.OnLevelUp += OnLevelUp;
        GameServices.Missions.OnCompleted += m =>
        {
            GameUI.Toast($"Missão concluída: {m.Title} (+{m.RewardXp} XP)", 4f);
            progression.AddXp(m.RewardXp, "missão");
        };
        GameServices.Clock.OnDayEnded += OnDayEnded;

        HUDExtensions.StatusText += Status;
        ApplyLevel(progression.Level);
    }

    private void OnDestroy()
    {
        HUDExtensions.StatusText -= Status;
    }

    private void Update()
    {
        GameServices.Clock.Paused = GameUI.IsModalOpen && GameUI.Current is MentorPanel;
        GameServices.Clock.Advance(Time.deltaTime);
    }

    private void OnLevelUp(int level)
    {
        GameUI.Toast($"Nível {level}! {ProgressionService.TitleFor(level)}", 4f);
        foreach (var u in GameServices.Progression.UnlocksAt(level)) GameUI.Toast("Desbloqueado: " + u.Description, 5f);
        ApplyLevel(level);
    }

    public static void ApplyLevel(int level)
    {
        var caps = ProgressionService.CapacitiesFor(level);
        var inv = GameServices.Inventory;
        inv.SetCapacity(StorageLocation.Backpack, Mathf.Max(caps.backpack, inv.GetUsedUnits(StorageLocation.Backpack)));
        inv.SetCapacity(StorageLocation.Warehouse, Mathf.Max(caps.warehouse, inv.GetUsedUnits(StorageLocation.Warehouse)));
        inv.SetCapacity(StorageLocation.PackingBench, Mathf.Max(caps.bench, inv.GetUsedUnits(StorageLocation.PackingBench)));
        inv.SetCapacity(StorageLocation.Order, Mathf.Max(caps.orders, inv.GetUsedUnits(StorageLocation.Order)));
        foreach (var gate in FindObjectsOfType<LevelGate>()) gate.Refresh(level);
    }

    public static void CheckMissions()
    {
        var sales = GameServices.Sales;
        var mentor = MentorDesk.Service;
        GameServices.Missions.Check(new MissionContext
        {
            Purchases = GameServices.Ledger.Entries.Count(e => e.Type == LedgerEntryType.Purchase),
            PackedOrders = GameServices.Ledger.Entries.Count(e => e.Type == LedgerEntryType.PackagingMaterials),
            Sales = sales.Sales.Count,
            TotalProfitCents = sales.Sales.Sum(s => s.ProfitCents),
            ApprovedProfitableSale = mentor.History.Any(e => e.SaleClosed && MentorEvaluator.IsApproved(e)
                && sales.Sales.Any(s => s.CustomerName == e.CustomerName && s.ProductId == e.ProductId && s.ProfitCents > 0)),
            Reputation = sales.Reputation,
            BestGrade = mentor.History.Count > 0 ? mentor.History.Max(e => e.Grade) : 0f
        });
    }

    private void OnDayEnded(int day)
    {
        var wallet = GameServices.Wallet;
        if (wallet.TryDebit(dailyRentCents))
            GameServices.Ledger.Record(LedgerEntryType.OperatingCost, -dailyRentCents, $"Aluguel do depósito (dia {day})");
        else
            GameUI.Toast("Sem dinheiro para o aluguel do depósito hoje!", 5f);

        foreach (var s in GameServices.Suppliers) s.Restock();

        GameUI.Toast($"Fim do dia {day}: {salesToday} venda(s), lucro direto {CustomerBrain.FormatReais(profitToday)}. Fornecedores reabasteceram.", 6f);
        salesToday = 0;
        profitToday = 0;
        DayEnded?.Invoke(day);
    }

    public static event System.Action<int> DayEnded;

    private static string Status()
    {
        var p = GameServices.Progression;
        var c = GameServices.Clock;
        if (p == null || c == null) return null;
        return $"Dia {c.Day} · {c.TimeText}  ·  Nível {p.Level} ({p.Title}) · {p.Xp} XP";
    }
}
