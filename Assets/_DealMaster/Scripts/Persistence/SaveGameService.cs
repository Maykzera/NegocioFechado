using System;
using System.IO;

/// <summary>Leitura e escrita de arquivos (permite testar sem disco).</summary>
public interface ISaveStorage
{
    bool Exists(string path);
    string Read(string path);
    void Write(string path, string content);
    void Delete(string path);
    void Move(string from, string to);
}

/// <summary>Converte SaveData em texto (na Unity: JsonUtility).</summary>
public interface ISaveSerializer
{
    string Serialize(SaveData data);
    SaveData Deserialize(string text);
}

public sealed class FileSaveStorage : ISaveStorage
{
    public bool Exists(string path) => File.Exists(path);
    public string Read(string path) => File.ReadAllText(path);
    public void Write(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }
    public void Delete(string path) { if (File.Exists(path)) File.Delete(path); }
    public void Move(string from, string to) => File.Move(from, to);
}

public readonly struct LoadResult
{
    public readonly SaveData Data;
    public readonly bool FromBackup;
    public readonly bool Migrated;
    public readonly string Error;

    public LoadResult(SaveData data, bool fromBackup, bool migrated, string error)
    {
        Data = data;
        FromBackup = fromBackup;
        Migrated = migrated;
        Error = error;
    }

    public bool Success => Data != null;
}

/// <summary>
/// Salvamento local versionado: escrita atômica (arquivo temporário + troca),
/// cópia de segurança do último salvamento válido, validação e migração entre versões.
/// </summary>
public sealed class SaveGameService
{
    public const int CurrentVersion = 3;

    private readonly ISaveStorage storage;
    private readonly ISaveSerializer serializer;
    private readonly string path;

    public string MainPath => path;
    public string BackupPath => path + ".bak";
    public string TempPath => path + ".tmp";

    public SaveGameService(ISaveStorage storage, ISaveSerializer serializer, string path)
    {
        this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.path = string.IsNullOrEmpty(path) ? throw new ArgumentException("caminho vazio") : path;
    }

    public bool HasSave => storage.Exists(path) || storage.Exists(BackupPath);

    public bool Save(SaveData data, out string error)
    {
        try
        {
            data.version = CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");
            string error0 = Validate(data);
            if (error0 != null) { error = error0; return false; }

            string text = serializer.Serialize(data);

            // 1) escreve no temporário; 2) guarda o salvamento anterior como backup; 3) troca.
            storage.Write(TempPath, text);
            if (storage.Exists(path))
            {
                storage.Delete(BackupPath);
                storage.Move(path, BackupPath);
            }
            storage.Move(TempPath, path);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            error = "Falha ao salvar: " + e.Message;
            return false;
        }
    }

    public LoadResult Load()
    {
        var main = TryRead(path);
        if (main.data != null) return new LoadResult(main.data, false, main.migrated, null);

        var backup = TryRead(BackupPath);
        if (backup.data != null) return new LoadResult(backup.data, true, backup.migrated, main.error);

        return new LoadResult(null, false, false, main.error ?? backup.error ?? "Nenhum jogo salvo.");
    }

    public void DeleteAll()
    {
        storage.Delete(path);
        storage.Delete(BackupPath);
        storage.Delete(TempPath);
    }

    private (SaveData data, bool migrated, string error) TryRead(string file)
    {
        try
        {
            if (!storage.Exists(file)) return (null, false, "Arquivo não encontrado.");
            var data = serializer.Deserialize(storage.Read(file));
            if (data == null) return (null, false, "Arquivo corrompido.");
            if (data.version > CurrentVersion) return (null, false, "Salvo por uma versão mais nova do jogo.");

            bool migrated = Migrate(data);
            string invalid = Validate(data);
            return invalid == null ? (data, migrated, null) : (null, false, invalid);
        }
        catch (Exception e)
        {
            return (null, false, "Arquivo corrompido: " + e.Message);
        }
    }

    /// <summary>Atualiza salvamentos antigos para o formato atual.</summary>
    public static bool Migrate(SaveData data)
    {
        if (data.version >= CurrentVersion) return false;
        // Versões 1 e 2 eram do protótipo de comércio, sem nada aproveitável: começa uma carreira nova.
        var fresh = new SaveData { version = CurrentVersion, balanceCents = Math.Max(0, data.balanceCents), day = 1, energy = 100 };
        data.version = fresh.version;
        data.day = fresh.day;
        data.energy = fresh.energy;
        data.xp = 0;
        data.upgrades = new System.Collections.Generic.List<string>();
        data.quotaDays = new System.Collections.Generic.List<int>();
        data.missions = new System.Collections.Generic.List<string>();
        data.sales = new System.Collections.Generic.List<SaleRecord>();
        data.ledger = new System.Collections.Generic.List<LedgerEntry>();
        data.reports = new System.Collections.Generic.List<CallReport>();
        data.pendingReviews = new System.Collections.Generic.List<CallReport>();
        data.reviews = new System.Collections.Generic.List<SupervisorReview>();
        data.queue = new System.Collections.Generic.List<Lead>();
        return true;
    }

    public static string Validate(SaveData data)
    {
        if (data == null) return "Dados vazios.";
        if (data.balanceCents < 0) return "Saldo inválido.";
        if (data.xp < 0) return "Experiência inválida.";
        if (data.day < 1) return "Dia inválido.";
        if (data.energy < 0 || data.energy > 100) return "Energia inválida.";
        return null;
    }
}
