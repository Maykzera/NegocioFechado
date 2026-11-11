/// <summary>
/// Configuração da IA de verdade. A chave da API é do próprio jogador, digitada no computador virtual
/// (Configurações) e guardada só neste aparelho. Nunca coloque uma chave no código ou num build distribuído.
/// </summary>
public sealed class AISettings
{
    public const string DefaultModel = "claude-opus-5-5";

    public bool UseRealAI = true;
    public string ApiKey = "";
    public string Model = DefaultModel;

    public bool CanUseRealAI => UseRealAI && !string.IsNullOrWhiteSpace(ApiKey) && ClaudeAvailability.SdkInstalled;
}

public static class ClaudeAvailability
{
#if DEALMASTER_ANTHROPIC_SDK
    public const bool SdkInstalled = true;
#else
    public const bool SdkInstalled = false;
#endif

    public static string Status(AISettings s)
    {
        if (!SdkInstalled) return "SDK da Anthropic não instalado: usando o cliente offline. Veja o README para instalar.";
        if (!s.UseRealAI) return "IA de verdade desligada: usando o cliente offline.";
        if (string.IsNullOrWhiteSpace(s.ApiKey)) return "Sem chave da API: usando o cliente offline.";
        return $"IA de verdade ligada ({s.Model}). Sem internet ou com erro, o jogo usa o cliente offline.";
    }
}
