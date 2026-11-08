using UnityEngine;

/// <summary>Move o sol e ajusta a luz conforme a hora do jogo.</summary>
public class DayNightController : MonoBehaviour
{
    [SerializeField] private Light sun;
    [SerializeField] private Gradient sunColor;
    [SerializeField] private float maxIntensity = 1.1f;

    private Light[] streetLights;

    private void Start()
    {
        if (sun == null) sun = RenderSettings.sun;
        if (sunColor == null || sunColor.colorKeys.Length < 2)
        {
            sunColor = new Gradient();
            sunColor.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.7f, 0.45f), 0f),
                    new GradientColorKey(new Color(1f, 0.97f, 0.9f), 0.35f),
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.65f),
                    new GradientColorKey(new Color(0.95f, 0.55f, 0.35f), 0.9f),
                    new GradientColorKey(new Color(0.3f, 0.35f, 0.6f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        }
        streetLights = System.Array.FindAll(FindObjectsOfType<Light>(), l => l.type == LightType.Point && l.transform.parent != null && l.transform.parent.name == "Poste");
    }

    private void Update()
    {
        var clock = GameServices.Clock;
        if (clock == null || sun == null) return;

        float t = clock.DayProgress;
        float elevation = Mathf.Lerp(15f, 165f, t);
        sun.transform.rotation = Quaternion.Euler(elevation, -30f, 0f);
        sun.color = sunColor.Evaluate(t);
        sun.intensity = Mathf.Lerp(0.25f, maxIntensity, Mathf.Sin(t * Mathf.PI) * 1.2f);
        RenderSettings.ambientIntensity = Mathf.Lerp(0.5f, 1f, Mathf.Sin(t * Mathf.PI));

        bool night = t > 0.82f;
        foreach (var l in streetLights) if (l != null) l.enabled = night;
    }
}
