using UnityEngine;

/// <summary>Serializador JSON da Unity.</summary>
public sealed class UnityJsonSerializer : ISaveSerializer
{
    public string Serialize(SaveData data) => JsonUtility.ToJson(data, true);
    public SaveData Deserialize(string text) => JsonUtility.FromJson<SaveData>(text);
}

/// <summary>Salva e carrega a carreira na central. Salva sozinho no fim do dia e ao sair ou pausar o app.</summary>
[DefaultExecutionOrder(500)]
public class SaveGameBridge : MonoBehaviour
{
    public const string FileName = "dealmaster_callcenter.json";

    private static SaveGameService service;
    private PlayerController player;

    public static SaveGameService Service =>
        service ?? (service = new SaveGameService(new FileSaveStorage(), new UnityJsonSerializer(),
            System.IO.Path.Combine(Application.persistentDataPath, FileName)));

    public static SaveGameBridge Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        player = FindObjectOfType<PlayerController>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnApplicationPause(bool paused) { if (paused) SaveNow(false); }
    private void OnApplicationQuit() => SaveNow(false);

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
        if (result.Migrated) GameUI.Toast("Salvamento de uma versão antiga: sua carreira na central começa do zero.", 6f);
        return true;
    }

    public void NewGame()
    {
        Service.DeleteAll();
        GameUI.CloseAll();
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private SaveData Capture()
    {
        var g = GameServices.Game;
        var d = new SaveData
        {
            balanceCents = GameServices.Wallet.BalanceCents,
            xp = g.Career.Xp,
            day = GameServices.Clock.Day,
            minutes = GameServices.Clock.Minutes,
            energy = g.Energy,
            seed = g.Seed,
        };
        d.upgrades.AddRange(g.Upgrades);
        d.quotaDays.AddRange(g.QuotaDays);
        d.missions.AddRange(g.Missions.Completed);
        d.sales.AddRange(g.Sales.Sales);
        d.ledger.AddRange(g.Ledger.Entries);
        d.reports.AddRange(g.Reports);
        d.pendingReviews.AddRange(g.PendingReviews);
        d.reviews.AddRange(g.Reviews);
        d.queue.AddRange(g.Queue);
        if (player != null)
        {
            var t = player.transform;
            d.playerX = t.position.x; d.playerY = t.position.y; d.playerZ = t.position.z; d.playerYaw = t.eulerAngles.y;
            d.hasPlayerPosition = true;
        }
        return d;
    }

    private void Apply(SaveData d)
    {
        GameServices.Wallet.Restore(d.balanceCents);
        GameServices.Clock.Restore(d.day, d.minutes);
        GameServices.Game.Restore(d.day, d.energy, d.xp, d.seed == 0 ? GameServices.Game.Seed : d.seed, d.upgrades, d.quotaDays,
            d.missions, d.sales, d.ledger, d.reports, d.pendingReviews, d.reviews, d.queue);
        if (d.hasPlayerPosition && player != null)
            player.Teleport(new Vector3(d.playerX, d.playerY, d.playerZ), d.playerYaw);
    }
}
