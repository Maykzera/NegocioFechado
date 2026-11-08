using UnityEngine;

/// <summary>Guarda as configurações da IA só neste aparelho (PlayerPrefs). A chave nunca vai para o salvamento do jogo.</summary>
public static class AISettingsStore
{
    private const string KeyUse = "dm_ai_use";
    private const string KeyApi = "dm_ai_key";
    private const string KeyModel = "dm_ai_model";
    private const string KeyName = "dm_player_name";

    public static AISettings Load()
    {
        return new AISettings
        {
            UseRealAI = PlayerPrefs.GetInt(KeyUse, 1) == 1,
            ApiKey = PlayerPrefs.GetString(KeyApi, ""),
            Model = PlayerPrefs.GetString(KeyModel, AISettings.DefaultModel)
        };
    }

    public static void Save(AISettings s)
    {
        PlayerPrefs.SetInt(KeyUse, s.UseRealAI ? 1 : 0);
        PlayerPrefs.SetString(KeyApi, s.ApiKey ?? "");
        PlayerPrefs.SetString(KeyModel, string.IsNullOrWhiteSpace(s.Model) ? AISettings.DefaultModel : s.Model.Trim());
        PlayerPrefs.Save();
    }

    public static string LoadPlayerName() => PlayerPrefs.GetString(KeyName, "Lucas");

    public static void SavePlayerName(string name)
    {
        PlayerPrefs.SetString(KeyName, string.IsNullOrWhiteSpace(name) ? "Lucas" : name.Trim());
        PlayerPrefs.Save();
    }
}
