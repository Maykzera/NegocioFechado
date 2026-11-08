using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Serializador JSON da Unity.</summary>
public sealed class UnityJsonSerializer : ISaveSerializer
{
    public string Serialize(SaveData data) => JsonUtility.ToJson(data, true);
    public SaveData Deserialize(string text) => JsonUtility.FromJson<SaveData>(text);
}

/// <summary>
/// Junta o estado de todos os sistemas num SaveData e restaura a partir dele.
/// Salva automaticamente no fim de cada dia e ao sair/pausar o app.
/// </summary>
[DefaultExecutionOrder(500)]
public class SaveGameBridge : MonoBehaviour
{
    public const string FileName = "dealmaster_save.json";

    private static SaveGameService service;
    private WalletService wallet;
    private PlayerController player;

    public static SaveGameService Service =>
        service ?? (service = new SaveGameService(new FileSaveStorage(), new UnityJsonSerializer(),
            System.IO.Path.Combine(Application.persistentDataPath, FileName)));

    public static SaveGameBridge Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        wallet = FindObjectOfType<WalletService>();
        player = FindObjectOfType<PlayerController>();
    }

    private void Start()
    {
        ProgressionBridge.DayEnded += OnDayEnded;
    }

    private void OnDestroy()
    {
        ProgressionBridge.DayEnded -= OnDayEnded;
        if (Instance == this) Instance = null;
    }

    private void OnDayEnded(int day) => SaveNow(false);

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveNow(false);
    }

    private void OnApplicationQuit()
    {
        SaveNow(false);
    }

    public bool SaveNow(bool notify = true)
    {
        if (!GameServices.IsReady || StartMenuPanel.IsShowing) return false;
        bool ok = Service.Save(Capture(), out var error);
        if (notify) GameUI.Toast(ok ? "Jogo salvo." : error);
        return ok;
    }

    public bool LoadSaved()
    {
        var result = Service.Load();
        if (!result.Success)
        {
            GameUI.Toast("Não foi possível carregar: " + result.Error, 5f);
            return false;
        }
        Apply(result.Data);
        if (result.FromBackup) GameUI.Toast("O último salvamento estava danificado; carregamos a cópia de segurança.", 5f);
        return true;
    }

    public void NewGame()
    {
        Service.DeleteAll();
        GameUI.CloseAll();
        CustomerSpawner.Memory.Restore(new List<CustomerRecord>());
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private SaveData Capture()
    {
        var d = new SaveData
        {
            balanceCents = GameServices.Wallet.BalanceCents,
            xp = GameServices.Progression.Xp,
            day = GameServices.Clock.Day,
            minutes = GameServices.Clock.Minutes,
            reputation = GameServices.Sales.Reputation,
            nextOrderNumber = GameServices.Packaging.NextOrderNumber,
            sales = GameServices.Sales.Sales.ToList(),
            completedMissions = GameServices.Missions.CompletedIds.ToList(),
            customers = CustomerSpawner.Memory.Records.ToList()
        };

        if (player != null)
        {
            var t = player.transform;
            d.playerX = t.position.x; d.playerY = t.position.y; d.playerZ = t.position.z;
            d.playerYaw = t.eulerAngles.y;
            d.hasPlayerPosition = true;
        }

        foreach (StorageLocation loc in System.Enum.GetValues(typeof(StorageLocation)))
            foreach (var s in GameServices.Inventory.GetStacks(loc))
                d.inventory.Add(new StackData { location = (int)loc, productId = s.ProductId, quantity = s.Quantity, totalCostCents = s.TotalCostCents });

        d.packedOrders = GameServices.Packaging.Orders.Select(SaveMapping.ToData).ToList();
        d.ledger = GameServices.Ledger.Entries.Select(SaveMapping.ToData).ToList();
        d.mentorHistory = MentorDesk.Service.History.Select(SaveMapping.ToData).ToList();
        d.mentorPending = MentorDesk.Service.Pending.Select(SaveMapping.ToData).ToList();

        foreach (var s in GameServices.Suppliers)
        {
            var sd = new SupplierData { id = s.SupplierId, relationship = s.Relationship };
            foreach (var id in s.ProductIds) { sd.productIds.Add(id); sd.stocks.Add(s.GetStock(id)); }
            d.suppliers.Add(sd);
        }
        return d;
    }

    private void Apply(SaveData d)
    {
        if (wallet != null) wallet.Restore(d.balanceCents);
        GameServices.Progression.Restore(d.xp);
        GameServices.Clock.Restore(d.day, d.minutes);
        GameServices.Missions.Restore(d.completedMissions);
        CustomerSpawner.Memory.Restore(d.customers ?? new List<CustomerRecord>());

        var inv = GameServices.Inventory;
        inv.Clear();
        foreach (var s in d.inventory ?? new List<StackData>())
            inv.RestoreStack((StorageLocation)s.location, s.productId, s.quantity, s.totalCostCents);
        ProgressionBridge.ApplyLevel(GameServices.Progression.Level);

        GameServices.Packaging.Restore((d.packedOrders ?? new List<OrderData>()).Select(SaveMapping.FromData), d.nextOrderNumber);
        GameServices.Sales.Restore(d.sales ?? new List<SaleRecord>(), d.reputation);
        GameServices.Ledger.Restore((d.ledger ?? new List<LedgerData>()).Select(SaveMapping.FromData));
        MentorDesk.Service.Restore(
            (d.mentorHistory ?? new List<EvaluationData>()).Select(SaveMapping.FromData),
            (d.mentorPending ?? new List<RecordData>()).Select(SaveMapping.FromData));

        foreach (var sd in d.suppliers ?? new List<SupplierData>())
        {
            var s = GameServices.GetSupplier(sd.id);
            if (s == null) continue;
            var pairs = sd.productIds.Zip(sd.stocks, (id, stock) => new KeyValuePair<string, int>(id, stock));
            s.RestoreState(sd.relationship, pairs);
        }

        if (d.hasPlayerPosition && player != null)
            player.Teleport(new Vector3(d.playerX, d.playerY, d.playerZ), d.playerYaw);
    }
}
