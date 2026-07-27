using UnityEngine;

// Música ambiental del quirófano: drone pad clínico + tensión
// Coloca en un GameObject vacío en la escena "Piso Quirofano"
public class GameplayMusicAmbient : MonoBehaviour
{
    [Range(0f, 1f)] public float musicVolume = 0.30f;

    AudioSource _src;
    const int   SR      = 22050;
    const float LOOP    = 8f;     // loop de 8 segundos

    void Awake()
    {
        _src              = gameObject.AddComponent<AudioSource>();
        _src.spatialBlend = 0f;   // 2D — música de fondo
        _src.loop         = true;
        _src.volume       = musicVolume;
        _src.priority     = 128;
        _src.playOnAwake  = false;
        _src.clip         = GenerateDronePad();
    }

    void Start()
    {
        GameSettingsManager.EnsureExists();
        _src.volume = musicVolume * GameSettingsManager.MusicVolume;
        GameSettingsManager.OnMusicVolumeChanged += v => _src.volume = musicVolume * v;
        _src.Play();
    }

    // Drone pad clínico: quinta perfecta + LFO lento + shimmer
    AudioClip GenerateDronePad()
    {
        int   n   = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];

        // Notas: Do2 (65Hz) + Sol2 (98Hz) + Do3 (130Hz) + Mi3 (164Hz)
        float[] freqs  = { 65f, 98f, 130f, 164f, 196f };
        float[] amps   = { 0.40f, 0.30f, 0.25f, 0.15f, 0.08f };

        float lfoRate  = 0.12f;   // modulación muy lenta (cada ~8s)
        float lfoDepth = 0.18f;

        for (int i = 0; i < n; i++)
        {
            float t    = i / (float)SR;
            float lfo  = 1f - lfoDepth + lfoDepth * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * lfoRate * t));

            float sample = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                // Onda mixta: seno suave + algo de armónico 2x para calidez
                sample += Mathf.Sin(2f * Mathf.PI * freqs[f] * t) * amps[f]
                        + Mathf.Sin(2f * Mathf.PI * freqs[f] * 2f * t) * amps[f] * 0.18f;
            }

            // Shimmer de alta frecuencia muy suave (~4kHz)
            float shimmer = Mathf.Sin(2f * Mathf.PI * 3800f * t)
                          * 0.018f
                          * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.07f * t));

            d[i] = (sample * lfo + shimmer) * 0.55f;
        }

        // Fade in/out suave en los extremos del loop para evitar click
        int fade = Mathf.RoundToInt(SR * 0.25f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }

        var c = AudioClip.Create("GameplayDrone", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
