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

    [Tooltip("Produtos extras criados como ScriptableObject (opcional). Os 10 produtos padrão são sempre carregados.")]
    [SerializeField] private ProductDefinition[] extraProducts = new ProductDefinition[0];

    private void Awake()
    {
        if (wallet == null) wallet = GetComponent<WalletService>();
        if (wallet == null) wallet = gameObject.AddComponent<WalletService>();

        var inventory = new InventoryService(backpackCapacity, warehouseCapacity, benchCapacity);
        ProductDatabase.Clear();
        DefaultContent.RegisterProducts();
        foreach (var p in extraProducts)
            if (p != null) ProductDatabase.Register(p.ToInfo());

        GameServices.Initialize(wallet, inventory);
        GameServices.RegisterSupplier(DefaultContent.CreateElectronicsSupplier(), "Eletro Centro");
        GameServices.RegisterSupplier(DefaultContent.CreateAccessoriesSupplier(), "Casa dos Acessórios");
        GameServices.RegisterSupplier(DefaultContent.CreateWholesaler(), "Atacado Bom Preço");

        Application.targetFrameRate = Application.isMobilePlatform ? 30 : targetFrameRate;
    }

    private void OnDestroy()
    {
        GameServices.Reset();
    }
}
