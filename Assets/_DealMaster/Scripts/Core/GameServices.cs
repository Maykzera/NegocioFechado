using System;

/// <summary>
/// Ponto central de acesso aos serviços do jogo, preenchido pelo GameBootstrap.
/// Mantém a lógica de domínio (classes puras) separada dos componentes de cena.
/// </summary>
public static class GameServices
{
    public static IWallet Wallet { get; private set; }
    public static InventoryService Inventory { get; private set; }
    public static PurchaseService Purchases { get; private set; }

    public static bool IsReady => Wallet != null && Inventory != null;

    public static event Action OnReady;

    public static void Initialize(IWallet wallet, InventoryService inventory)
    {
        Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        Purchases = new PurchaseService(wallet, inventory);
        OnReady?.Invoke();
    }

    public static void Reset()
    {
        Wallet = null;
        Inventory = null;
        Purchases = null;
    }
}
