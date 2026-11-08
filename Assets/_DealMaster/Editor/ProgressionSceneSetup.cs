using UnityEditor;

/// <summary>Etapa 7: progressão, missões, relógio e ciclo de dia e noite.</summary>
[InitializeOnLoad]
public static class ProgressionSceneSetup
{
    static ProgressionSceneSetup()
    {
        DealMasterSceneBuilder.OnPopulate += layout =>
        {
            layout.Systems.gameObject.AddComponent<ProgressionBridge>();
            layout.Systems.gameObject.AddComponent<DayNightController>();
        };
    }
}
