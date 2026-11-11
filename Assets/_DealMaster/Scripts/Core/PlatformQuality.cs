using UnityEngine;

/// <summary>Perfis gráficos distintos para PC e Android.</summary>
public class PlatformQuality : MonoBehaviour
{
    [SerializeField] private float mobileShadowDistance = 25f;
    [SerializeField] private float pcShadowDistance = 60f;
    [SerializeField] private float mobileRenderScale = 0.85f;

    private void Awake()
    {
        string[] names = QualitySettings.names;
        if (Application.isMobilePlatform)
        {
            QualitySettings.SetQualityLevel(Mathf.Min(1, names.Length - 1), true);
            QualitySettings.shadowDistance = mobileShadowDistance;
            QualitySettings.antiAliasing = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 30;
            ScalableBufferManager.ResizeBuffers(mobileRenderScale, mobileRenderScale);
        }
        else
        {
            QualitySettings.SetQualityLevel(names.Length - 1, true);
            QualitySettings.shadowDistance = pcShadowDistance;
            Application.targetFrameRate = 60;
        }
    }
}
