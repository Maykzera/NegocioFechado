using UnityEditor;

/// <summary>Etapa 5: conecta a negociação ao sistema de vendas.</summary>
[InitializeOnLoad]
public static class SalesSceneSetup
{
    static SalesSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += layout => layout.Systems.gameObject.AddComponent<SalesBridge>();
    }
}
