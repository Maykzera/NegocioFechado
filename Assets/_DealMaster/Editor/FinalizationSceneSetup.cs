using UnityEditor;

/// <summary>Etapa 8: salvamento, menu inicial, qualidade por plataforma e sons.</summary>
[InitializeOnLoad]
public static class FinalizationSceneSetup
{
    static FinalizationSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += layout =>
        {
            var go = layout.Systems.gameObject;
            go.AddComponent<PlatformQuality>();
            go.AddComponent<UnityEngine.AudioSource>();
            go.AddComponent<AudioFeedback>();
            go.AddComponent<SaveGameBridge>();
            go.AddComponent<StartMenu>();
        };
    }
}
