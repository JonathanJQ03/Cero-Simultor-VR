using UnityEngine;

public class QuirofanoAmbient : MonoBehaviour
{
    [Header("Volúmenes")]
    [Range(0f, 1f)] public float humVolume        = 0.22f;  // zumbido eléctrico
    [Range(0f, 1f)] public float ventilatorVolume = 0.35f;  // ventilador/respirador
    [Range(0f, 1f)] public float noiseVolume      = 0.18f;  // ruido de fondo

    AudioSource _srcHum;
    AudioSource _srcVent;
    AudioSource _srcNoise;

    const int SR       = 22050;   // menor sample rate para ambient (ahorra memoria)
    const float LOOP   = 6f;      // duración del loop en segundos

    void Awake()
    {
        _srcHum   = CreateSource("Hum",        humVolume,        0f);
        _srcVent  = CreateSource("Ventilator", ventilatorVolume, 0f);
        _srcNoise = CreateSource("Noise",      noiseVolume,      0f);

        _srcHum.clip   = GenerateHum();
        _srcVent.clip  = GenerateVentilator();
        _srcNoise.clip = GenerateWhiteNoise();
    }

    void Start()
    {
        _srcHum.Play();
        _srcVent.Play();
        _srcNoise.Play();
    }

    AudioSource CreateSource(string label, float vol, float spatialBlend)
    {
        var go  = new GameObject("Ambient_" + label);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.volume       = vol;
        src.loop         = true;
        src.playOnAwake  = false;
        src.spatialBlend = spatialBlend;
        src.priority     = 200;   // baja prioridad
        return src;
    }

    // Zumbido eléctrico de equipos (60 Hz + armónicos)
    AudioClip GenerateHum()
    {
        int n   = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            d[i] = Mathf.Sin(2f * Mathf.PI * 60f  * t) * 0.50f
                 + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.25f
                 + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.12f;
            d[i] *= 0.4f;
        }
        return MakeClip("Hum", d);
    }

    // Ventilador / respirador: ruido filtrado con ritmo de ~14 respiraciones/min
    AudioClip GenerateVentilator()
    {
        int   n    = Mathf.RoundToInt(SR * LOOP);
        float[] d  = new float[n];
        var rng    = new System.Random(99);
        float prev = 0f;
        float breathRate = 14f / 60f;  // ciclos por segundo

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;

            // Ruido de baja frecuencia (filtro paso-bajo simple)
            float raw  = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, raw, 0.015f);

            // Modulación de volumen = respiración rítmica (inhala/exhala)
            float breath = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * breathRate * t - Mathf.PI * 0.5f);
            breath = Mathf.Pow(breath, 1.5f);  // más marcado

            // Mecanismo del ventilador (click suave al inicio de cada ciclo)
            float mechCycle = (t * breathRate) % 1f;
            float mechClick = Mathf.Exp(-mechCycle * 60f) * 0.3f;

            d[i] = (prev * breath + mechClick) * 0.6f;
        }
        return MakeClip("Ventilator", d);
    }

    // Ruido de fondo muy suave (presencia de sala)
    AudioClip GenerateWhiteNoise()
    {
        int   n   = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];
        var rng   = new System.Random(13);
        float prev = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, raw, 0.02f);  // paso-bajo
            d[i] = prev * 0.35f;
        }
        return MakeClip("WhiteNoise", d);
    }

    static AudioClip MakeClip(string label, float[] data)
    {
        var c = AudioClip.Create("Ambient_" + label, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
}
