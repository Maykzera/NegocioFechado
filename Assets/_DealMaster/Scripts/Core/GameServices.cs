using System;
using System.Collections.Generic;

/// <summary>
/// Ponto central de acesso aos serviços do jogo, preenchido pelo GameBootstrap.
/// Mantém a lógica de domínio (classes puras) separada dos componentes de cena.
/// </summary>
public static class GameServices
{
    private static readonly Dictionary<string, SupplierStock> suppliers = new Dictionary<string, SupplierStock>();
    private static readonly Dictionary<string, string> supplierNames = new Dictionary<string, string>();

    public static IWallet Wallet { get; private set; }
    public static InventoryService Inventory { get; private set; }
    public static PurchaseService Purchases { get; private set; }
    public static PackagingService Packaging { get; private set; }
    public static FinanceLedger Ledger { get; private set; }
    public static SalesService Sales { get; private set; }
    public static ProgressionService Progression { get; private set; }
    public static MissionService Missions { get; private set; }
    public static GameClock Clock { get; private set; }

    /// <summary>Nível do empreendedor. Controlado pelo sistema de progressão.</summary>
    public static Func<int> PlayerLevelProvider;

    /// <summary>Hora atual do jogo (0 a 23). Controlada pelo relógio do jogo.</summary>
    public static Func<int> HourProvider;

    public static int PlayerLevel => PlayerLevelProvider?.Invoke() ?? 1;
    public static int CurrentHour => HourProvider?.Invoke() ?? 10;

    public static bool IsReady => Wallet != null && Inventory != null;

    public static IEnumerable<SupplierStock> Suppliers => suppliers.Values;

    public static event Action OnReady;

    public static void Initialize(IWallet wallet, InventoryService inventory)
    {
        Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Purchases = new PurchaseService(wallet, inventory);
        Packaging = new PackagingService(wallet, inventory);
        Ledger = new FinanceLedger();
        Sales = new SalesService(wallet, inventory, Packaging, Ledger);
        Progression = new ProgressionService();
        Missions = new MissionService();
        Clock = new GameClock();

        PlayerLevelProvider = () => Progression.Level;
        HourProvider = () => Clock.Hour;
        Ledger.DayProvider = () => Clock.Day;
        Sales.DayProvider = () => Clock.Day;

        Purchases.OnPurchaseCompleted += r => Ledger.Record(LedgerEntryType.Purchase, -r.TotalCents,
            $"Compra: {r.Quantity}x {ProductDatabase.NameOf(r.ProductId)} ({SupplierName(r.SupplierId)})");
        Packaging.OnPacked += o => Ledger.Record(LedgerEntryType.PackagingMaterials, -o.Score.MaterialsCostCents,
            $"Embalagem {o.OrderId}: {ProductDatabase.NameOf(o.ProductId)}");
        OnReady?.Invoke();
    }

    public static void RegisterSupplier(SupplierStock supplier, string displayName)
    {
        suppliers[supplier.SupplierId] = supplier;
        supplierNames[supplier.SupplierId] = displayName;
    }

    public static SupplierStock GetSupplier(string id) =>
        id != null && suppliers.TryGetValue(id, out var s) ? s : null;

    public static string SupplierName(string id) =>
        id != null && supplierNames.TryGetValue(id, out var n) ? n : id;

    public static void Reset()
    {
        Wallet = null;
        Inventory = null;
        Purchases = null;
        Packaging = null;
        Ledger = null;
        Sales = null;
        Progression = null;
        Missions = null;
        Clock = null;
        PlayerLevelProvider = null;
        HourProvider = null;
        suppliers.Clear();
        supplierNames.Clear();
    }
}
