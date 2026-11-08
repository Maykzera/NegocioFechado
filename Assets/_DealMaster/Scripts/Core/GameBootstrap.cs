using UnityEngine;

/// <summary>Inicializa os serviços do jogo ao carregar a cena.</summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private WalletService wallet;
    [SerializeField] private int backpackCapacity = 10;
    [SerializeField] private int warehouseCapacity = 60;
    [SerializeField] private int benchCapacity = 4;
    [SerializeField] private int targetFrameRate = 60;

    private void Awake()
    {
        if (wallet == null) wallet = GetComponent<WalletService>();
        if (wallet == null) wallet = gameObject.AddComponent<WalletService>();

        var inventory = new InventoryService(backpackCapacity, warehouseCapacity, benchCapacity);
        GameServices.Initialize(wallet, inventory);

        Application.targetFrameRate = Application.isMobilePlatform ? 30 : targetFrameRate;
    }

    private void OnDestroy()
    {
        GameServices.Reset();
    }
}
