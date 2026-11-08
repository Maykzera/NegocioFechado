using UnityEngine;

/// <summary>
/// Sons de retorno gerados por código (sem arquivos de áudio): compra, venda, embalagem,
/// reclamação e subida de nível. Substitua por clipes definitivos quando houver.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioFeedback : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float volume = 0.4f;

    private AudioSource source;
    private AudioClip coin, packed, complaint, levelUp;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        coin = Tone("moeda", new[] { 988f, 1319f }, 0.09f);
        packed = Tone("fita", new[] { 330f, 392f }, 0.07f);
        complaint = Tone("reclamacao", new[] { 330f, 262f }, 0.15f);
        levelUp = Tone("nivel", new[] { 523f, 659f, 784f, 1047f }, 0.1f);
    }

    private void Start()
    {
        GameServices.Purchases.OnPurchaseCompleted += _ => Play(packed);
        GameServices.Packaging.OnPacked += _ => Play(packed);
        GameServices.Sales.OnSaleCompleted += s => Play(s.Damaged ? complaint : coin);
        GameServices.Progression.OnLevelUp += _ => Play(levelUp);
    }

    private void Play(AudioClip clip)
    {
        if (clip != null) source.PlayOneShot(clip, volume);
    }

    private static AudioClip Tone(string name, float[] notes, float noteSeconds)
    {
        const int rate = 22050;
        int perNote = (int)(rate * noteSeconds);
        var data = new float[perNote * notes.Length];
        for (int n = 0; n < notes.Length; n++)
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / rate;
                float env = Mathf.Clamp01(1f - (float)i / perNote) * Mathf.Clamp01(i / 200f);
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * env * 0.5f;
            }
        var clip = AudioClip.Create(name, data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
