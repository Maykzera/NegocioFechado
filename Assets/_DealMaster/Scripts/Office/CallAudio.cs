using UnityEngine;

/// <summary>Sons de telefone gerados por código: chamada, atender, desligar e venda.</summary>
public class CallAudio : MonoBehaviour
{
    private static CallAudio instance;
    private AudioSource source;
    private AudioClip dial, pickup, hangup, sale;

    private void Awake()
    {
        instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        dial = Tone("chamando", new[] { 425f, 0f, 425f, 0f }, 0.35f);
        pickup = Tone("atendeu", new[] { 660f, 880f }, 0.08f);
        hangup = Tone("desligou", new[] { 480f, 0f, 480f, 0f, 480f }, 0.15f);
        sale = Tone("venda", new[] { 784f, 988f, 1319f }, 0.1f);
    }

    public static void Dial() => Play(i => i.dial);
    public static void PickUp() => Play(i => i.pickup);
    public static void HangUp() => Play(i => i.hangup);
    public static void Sale() => Play(i => i.sale);

    private static void Play(System.Func<CallAudio, AudioClip> pick)
    {
        if (instance == null || instance.source == null) return;
        instance.source.PlayOneShot(pick(instance), 0.35f);
    }

    private static AudioClip Tone(string name, float[] notes, float noteSeconds)
    {
        const int rate = 22050;
        int per = (int)(rate * noteSeconds);
        var data = new float[per * notes.Length];
        for (int n = 0; n < notes.Length; n++)
            for (int i = 0; i < per; i++)
            {
                if (notes[n] <= 0) continue;
                float env = Mathf.Clamp01(1f - (float)i / per) * Mathf.Clamp01(i / 150f);
                data[n * per + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * i / rate) * env * 0.4f;
            }
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
