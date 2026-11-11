using UnityEngine;

/// <summary>Monta os sistemas da central ao carregar a cena e liga a IA (de verdade ou offline).</summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private WalletService wallet;
    [SerializeField] private int seed = 2026;

    private void Awake()
    {
        if (wallet == null) wallet = GetComponent<WalletService>();
        if (wallet == null) wallet = gameObject.AddComponent<WalletService>();

        var ai = AISettingsStore.Load();
        var game = new CallCenterGame(wallet, seed) { PlayerName = AISettingsStore.LoadPlayerName() };
        ConfigureAI(game, ai);

        GameServices.Initialize(wallet, game, new GameClock(), ai);
        Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
    }

    /// <summary>Define quem interpreta o cliente e o supervisor. Chamado de novo quando o jogador muda as configurações.</summary>
    public static void ConfigureAI(CallCenterGame game, AISettings ai)
    {
#if DEALMASTER_ANTHROPIC_SDK
        game.CustomerAIFactory = (lead, ctx) => ai.CanUseRealAI ? new ClaudeCustomerAI(ai.ApiKey, ai.Model) : null;
        game.Supervisor = ai.CanUseRealAI ? (ISupervisorAI)new ClaudeSupervisor(ai.ApiKey, ai.Model) : new OfflineSupervisor();
#else
        game.CustomerAIFactory = null;
        game.Supervisor = new OfflineSupervisor();
#endif
    }

    private void OnDestroy()
    {
        GameServices.Reset();
    }
}
