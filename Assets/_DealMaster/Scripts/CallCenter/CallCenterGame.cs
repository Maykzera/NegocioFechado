using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// O jogo da central de vendas, sem dependência de cena: fila de clientes, energia,
/// ligações, registro de vendas, supervisor, carreira, missões, melhorias e fim do expediente.
/// </summary>
public sealed class CallCenterGame
{
    public const string CompanyName = "Conecta Vendas";
    public const int MaxEnergy = 100;
    public const long CoffeePriceCents = 300;
    public const int LeadsPerDay = 12;

    private readonly IWallet wallet;
    private readonly List<Lead> queue = new List<Lead>();
    private readonly List<CallReport> reports = new List<CallReport>();
    private readonly List<CallReport> pendingReviews = new List<CallReport>();
    private readonly List<SupervisorReview> reviews = new List<SupervisorReview>();
    private readonly HashSet<string> upgrades = new HashSet<string>();
    private readonly HashSet<int> quotaDays = new HashSet<int>();
    private int callCounter;
    private System.Random rng;

    public FinanceLedger Ledger { get; } = new FinanceLedger();
    public SalesDesk Sales { get; }
    public CareerService Career { get; } = new CareerService();
    public MissionBoard Missions { get; } = new MissionBoard();
    public IWallet Wallet => wallet;

    public string PlayerName { get; set; } = "Você";
    public int Day { get; private set; } = 1;
    public int Energy { get; private set; } = MaxEnergy;
    public int Seed { get; private set; }

    public IReadOnlyList<Lead> Queue => queue;
    public IReadOnlyList<CallReport> Reports => reports;
    public IReadOnlyList<CallReport> PendingReviews => pendingReviews;
    public IReadOnlyList<SupervisorReview> Reviews => reviews;
    public IEnumerable<string> Upgrades => upgrades;
    public IEnumerable<int> QuotaDays => quotaDays;

    public CallSession ActiveCall { get; private set; }

    /// <summary>Cria a IA do cliente para cada ligação (Claude ou offline).</summary>
    public Func<Lead, CallContext, ICustomerAI> CustomerAIFactory;

    /// <summary>Supervisor (Claude ou offline). O offline é sempre o reserva.</summary>
    public ISupervisorAI Supervisor;

    public event Action<string> OnNotice;
    public event Action<CallReport> OnCallFinished;
    public event Action<SupervisorReview> OnReviewed;

    public int Level => Career.Level;
    public int Quota => SalesDesk.QuotaFor(Level);
    public int SalesToday => Sales.SalesOnDay(Day);

    public CallCenterGame(IWallet wallet, int seed)
    {
        this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        Seed = seed;
        rng = new System.Random(seed);
        Sales = new SalesDesk(wallet, Ledger) { DayProvider = () => Day };
        Ledger.DayProvider = () => Day;
        Missions.OnCompleted += m => { Notice($"Missão concluída: {m.Title} (+{m.RewardXp} XP)"); Career.AddXp(m.RewardXp); };
        Career.OnLevelUp += l => Notice($"Promoção! Agora você é {CareerService.TitleFor(l)} (nível {l}).");
        RefillQueue();
    }

    public bool HasUpgrade(string id) => upgrades.Contains(id);

    // ------------------------------------------------------------------ ligações

    public string CanCall(Lead lead)
    {
        if (ActiveCall != null && ActiveCall.State != CallState.Ended) return "Termine a ligação atual primeiro.";
        if (lead == null || !queue.Contains(lead)) return "Escolha alguém da lista.";
        if (Energy < EnergyPerCall) return "Você está sem energia. Tome um café na copa ou na sua mesa.";
        return null;
    }

    public int EnergyPerCall => HasUpgrade(UpgradeCatalog.ErgoChair) ? 5 : 10;

    public async Task<CallSession> StartCallAsync(Lead lead)
    {
        string block = CanCall(lead);
        if (block != null) { Notice(block); return null; }

        Energy -= EnergyPerCall;
        queue.Remove(lead);
        var context = new CallContext
        {
            PlayerName = PlayerName,
            CompanyName = CompanyName,
            PlayerLevel = Level,
            HasHeadsetUpgrade = HasUpgrade(UpgradeCatalog.Headset)
        };
        float bonus = HasUpgrade(UpgradeCatalog.Headset) ? 0.05f : 0f;
        var offline = new OfflineCustomerAI(lead, bonus);
        var ai = CustomerAIFactory?.Invoke(lead, context) ?? offline;
        var call = new CallSession($"C{Day:00}-{++callCounter:000}", lead, context, ai, ai == offline ? null : offline, Day);
        ActiveCall = call;
        await call.DialAsync(rng.NextDouble());
        if (call.State == CallState.Ended) FinishCall(call);
        return call;
    }

    public bool TryRegisterSale(string serviceId, long priceCents, out string error)
    {
        var call = ActiveCall;
        if (call == null) { error = "Nenhuma ligação ativa."; return false; }
        if (!Sales.TryRegister(call, serviceId, priceCents, Level, out var sale, out error)) return false;
        Career.AddXp(CareerService.XpForSale(sale));
        Notice($"Venda registrada! Comissão de {Money.Format(sale.CommissionCents)}.");
        CheckMissions();
        return true;
    }

    /// <summary>Encerra (se preciso) e arquiva a ligação ativa.</summary>
    public CallReport FinishCall(CallSession call = null)
    {
        call = call ?? ActiveCall;
        if (call == null) return null;
        call.HangUp();
        var report = CallReport.From(call, Sales.ForCall(call.CallId));
        if (reports.All(r => r.CallId != report.CallId))
        {
            reports.Add(report);
            Career.AddXp(CareerService.XpForCall(report));
            if (report.Outcome != CallOutcome.NoAnswer && report.PlayerLines.Count > 0) pendingReviews.Add(report);
            OnCallFinished?.Invoke(report);
            CheckMissions();
        }
        if (ActiveCall == call) ActiveCall = null;
        if (queue.Count < 3) RefillQueue();
        return report;
    }

    // ------------------------------------------------------------------ supervisor

    public async Task<SupervisorReview> ReviewNextAsync()
    {
        if (pendingReviews.Count == 0) return null;
        var report = pendingReviews[0];
        SupervisorReview review;
        try
        {
            review = Supervisor != null ? await Supervisor.ReviewAsync(report) : OfflineSupervisor.Review(report);
        }
        catch (Exception)
        {
            review = OfflineSupervisor.Review(report);
        }
        pendingReviews.RemoveAt(0);
        reviews.Add(review);

        var sale = Sales.ForCall(report.CallId);
        if (sale != null && review.LiesFound.Count > 0 && !sale.ChargedBack)
        {
            long taken = Sales.Chargeback(sale, "o cliente foi enganado");
            review.ChargebackApplied = true;
            Notice($"Venda cancelada por informação falsa. Estorno de {Money.Format(taken)}.");
        }
        Career.AddXp(CareerService.XpForReview(review));
        OnReviewed?.Invoke(review);
        CheckMissions();
        return review;
    }

    // ------------------------------------------------------------------ copa e melhorias

    public bool BuyCoffee(out string error)
    {
        if (Energy >= MaxEnergy) { error = "Você já está com energia total."; return false; }
        if (!HasUpgrade(UpgradeCatalog.CoffeeMaker))
        {
            if (!wallet.TryDebit(CoffeePriceCents)) { error = "Sem dinheiro para o café."; return false; }
            Ledger.Record(LedgerEntryType.Coffee, -CoffeePriceCents, "Café");
        }
        Energy = Math.Min(MaxEnergy, Energy + 35);
        error = null;
        return true;
    }

    public bool BuyUpgrade(string id, out string error)
    {
        var u = UpgradeCatalog.Get(id);
        if (u == null) { error = "Melhoria desconhecida."; return false; }
        if (upgrades.Contains(id)) { error = "Você já tem essa melhoria."; return false; }
        if (Level < u.RequiredLevel) { error = $"Disponível a partir do nível {u.RequiredLevel}."; return false; }
        if (!wallet.TryDebit(u.PriceCents)) { error = "Dinheiro insuficiente."; return false; }
        upgrades.Add(id);
        Ledger.Record(LedgerEntryType.Upgrade, -u.PriceCents, $"Melhoria: {u.Name}");
        Notice($"Comprou: {u.Name}.");
        error = null;
        return true;
    }

    // ------------------------------------------------------------------ dia

    /// <summary>Fim do expediente: bônus da meta, energia recarregada e nova lista de clientes.</summary>
    public string EndDay()
    {
        if (ActiveCall != null) FinishCall(ActiveCall);
        int sold = SalesToday;
        string summary = $"Fim do dia {Day}: {sold} venda(s) de {Quota} da meta, ganhos de {Money.Format(Ledger.SumOnDay(Day))}.";
        if (sold >= Quota && !quotaDays.Contains(Day))
        {
            quotaDays.Add(Day);
            long bonus = 5000L * Level;
            wallet.TryCredit(bonus);
            Ledger.Record(LedgerEntryType.QuotaBonus, bonus, $"Bônus de meta do dia {Day}");
            Career.AddXp(30);
            summary += $" Meta batida: bônus de {Money.Format(bonus)}!";
        }
        CheckMissions();
        Day++;
        Energy = MaxEnergy;
        queue.Clear();
        RefillQueue();
        Notice(summary);
        return summary;
    }

    public void RefillQueue()
    {
        bool qualified = HasUpgrade(UpgradeCatalog.QualifiedLeads);
        int needed = LeadsPerDay - queue.Count;
        foreach (var lead in LeadGenerator.GenerateQueue(Seed + Day * 1000 + reports.Count, needed, Level, qualified))
            queue.Add(lead);
    }

    public void CheckMissions()
    {
        Missions.Evaluate(new MissionContext
        {
            CallsAnswered = reports.Count(r => r.Outcome != CallOutcome.NoAnswer),
            Sales = Sales.Sales.Count(s => !s.ChargedBack),
            ApprovedSales = reviews.Count(r => r.SaleClosed && r.Approved),
            BestGrade10 = reviews.Count == 0 ? 0 : (int)Math.Round(reviews.Max(r => r.Grade) * 10),
            QuotaDays = quotaDays.Count,
            RespectedDoNotCall = reports.Any(r => r.DoNotCall && r.RespectedDoNotCall)
        });
    }

    private void Notice(string message) => OnNotice?.Invoke(message);

    // ------------------------------------------------------------------ salvamento

    public void Restore(int day, int energy, int xp, int seed, IEnumerable<string> ownedUpgrades, IEnumerable<int> quota,
        IEnumerable<string> missions, IEnumerable<SaleRecord> sales, IEnumerable<LedgerEntry> ledger,
        IEnumerable<CallReport> savedReports, IEnumerable<CallReport> pending, IEnumerable<SupervisorReview> savedReviews,
        IEnumerable<Lead> savedQueue)
    {
        Day = Math.Max(1, day);
        Energy = Math.Max(0, Math.Min(MaxEnergy, energy));
        Seed = seed;
        rng = new System.Random(seed + day);
        Career.Restore(xp);
        upgrades.Clear(); foreach (var u in ownedUpgrades ?? Enumerable.Empty<string>()) upgrades.Add(u);
        quotaDays.Clear(); foreach (var d in quota ?? Enumerable.Empty<int>()) quotaDays.Add(d);
        Missions.Restore(missions);
        Sales.Restore(sales ?? Enumerable.Empty<SaleRecord>());
        Ledger.Restore(ledger ?? Enumerable.Empty<LedgerEntry>());
        reports.Clear(); reports.AddRange(savedReports ?? Enumerable.Empty<CallReport>());
        pendingReviews.Clear(); pendingReviews.AddRange(pending ?? Enumerable.Empty<CallReport>());
        reviews.Clear(); reviews.AddRange(savedReviews ?? Enumerable.Empty<SupervisorReview>());
        callCounter = reports.Count;
        queue.Clear(); queue.AddRange(savedQueue ?? Enumerable.Empty<Lead>());
        if (queue.Count == 0) RefillQueue();
        ActiveCall = null;
    }
}
