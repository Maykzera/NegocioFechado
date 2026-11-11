using System;

/// <summary>Acesso central aos sistemas do jogo, preenchido pelo GameBootstrap.</summary>
public static class GameServices
{
    public static WalletService Wallet { get; private set; }
    public static CallCenterGame Game { get; private set; }
    public static GameClock Clock { get; private set; }
    public static AISettings AI { get; private set; }

    public static bool IsReady => Game != null;

    public static void Initialize(WalletService wallet, CallCenterGame game, GameClock clock, AISettings ai)
    {
        Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        Game = game ?? throw new ArgumentNullException(nameof(game));
        Clock = clock ?? throw new ArgumentNullException(nameof(clock));
        AI = ai ?? new AISettings();
    }

    public static void Reset()
    {
        Wallet = null;
        Game = null;
        Clock = null;
        AI = null;
    }
}
