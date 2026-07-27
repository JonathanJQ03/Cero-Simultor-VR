using UnityEngine;

public class QuirofanoAmbient : MonoBehaviour
{
    [Header("Volúmenes")]
    [Range(0f, 1f)] public float humVolume        = 0.15f;
    [Range(0f, 1f)] public float ventilatorVolume = 0.02f;
    [Range(0f, 1f)] public float hvacVolume       = 0.02f;
    [Range(0f, 1f)] public float dripVolume       = 0.07f;
    [Range(0f, 1f)] public float noiseVolume      = 0.15f;

    AudioSource _srcHum;
    AudioSource _srcVent;
    AudioSource _srcHvac;
    AudioSource _srcDrip;
    AudioSource _srcNoise;

    const int   SR   = 22050;
    // 12s: ventilador a 10 resp/min (periodo 6s) → 2 ciclos completos, loop limpio
    const float LOOP = 12f;

    void Awake()
    {
        _srcHum   = CreateSource("Hum",        humVolume);
        _srcVent  = CreateSource("Ventilator", ventilatorVolume);
        _srcHvac  = CreateSource("HVAC",       hvacVolume);
        _srcDrip  = CreateSource("Drip",       dripVolume);
        _srcNoise = CreateSource("Noise",      noiseVolume);

        _srcHum.clip   = GenerateHum();
        _srcVent.clip  = GenerateVentilator();
        _srcHvac.clip  = GenerateHvac();
        _srcDrip.clip  = GenerateDrip();
        _srcNoise.clip = GenerateNoise();
    }

    void Start()
    {
        _srcHum.Play();
        _srcVent.Play();
        _srcHvac.Play();
        _srcDrip.Play();
        _srcNoise.Play();
    }

    AudioSource CreateSource(string label, float vol)
    {
        var go  = new GameObject("Ambient_" + label);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.volume       = vol;
        src.loop         = true;
        src.playOnAwake  = false;
        src.spatialBlend = 0f;
        src.priority     = 200;
        return src;
    }

    // Zumbido eléctrico: 60 Hz + armónicos (lámparas, equipos)
    AudioClip GenerateHum()
    {
        int n     = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 60f  * t) * 0.50f
                  + Mathf.Sin(2f * Mathf.PI * 120f * t) * 0.22f
                  + Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.10f
                  + Mathf.Sin(2f * Mathf.PI * 240f * t) * 0.05f) * 0.35f;
        }
        return MakeClip("Hum", d);
    }

    // Ventilador mecánico: 10 resp/min → periodo exacto de 6s, loop de 12s sin corte
    AudioClip GenerateVentilator()
    {
        int    n   = Mathf.RoundToInt(SR * LOOP);
        float[] d  = new float[n];
        var rng    = new System.Random(99);
        float prev = 0f;

        const float breathHz = 10f / 60f;   // 0.1667 Hz, periodo = 6s

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;

            // Ruido filtrado (simula flujo de aire)
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, raw, 0.013f);

            // Envelope de respiración: rango [0.35, 1.0] — nunca cae a cero
            float phase  = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * breathHz * t - Mathf.PI * 0.5f);
            float breath = 0.35f + 0.65f * Mathf.Pow(phase, 0.6f);

            // Click mecánico suave al cambio de ciclo (media onda seno, no exponencial)
            float mechPhase = (t * breathHz) % 1f;
            float mechClick = mechPhase < 0.04f
                ? Mathf.Sin(Mathf.PI * mechPhase / 0.04f) * 0.10f
                : 0f;

            d[i] = prev * breath * 0.75f + mechClick;
        }

        // Fade muy corto solo en los extremos del loop (0.08s) para evitar click de costura
        int fade = Mathf.RoundToInt(SR * 0.08f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }

        return MakeClip("Ventilator", d);
    }

    // HVAC / climatización: flujo de aire constante de alta frecuencia
    AudioClip GenerateHvac()
    {
        int    n    = Mathf.RoundToInt(SR * LOOP);
        float[] d   = new float[n];
        var rng     = new System.Random(42);
        float lp1   = 0f, lp2 = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp1 = Mathf.Lerp(lp1, raw,  0.07f);
            lp2 = Mathf.Lerp(lp2, lp1,  0.04f);
            // Paso-alto = señal – paso-bajo → sonido de flujo, sin bajos
            d[i] = (raw - lp2) * 0.38f;
        }

        int fade = Mathf.RoundToInt(SR * 0.15f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }
        return MakeClip("HVAC", d);
    }

    // Goteo IV: ~1.3 gotas/s con tono corto decayente y pequeña variación
    AudioClip GenerateDrip()
    {
        int    n           = Mathf.RoundToInt(SR * LOOP);
        float[] d          = new float[n];
        var rng            = new System.Random(7);

        const float dripHz = 1.3f;
        int dripSamples    = Mathf.RoundToInt(SR / dripHz);

        // Frecuencia fija por gota (pre-generada)
        int numDrips = n / dripSamples + 2;
        float[] freqs = new float[numDrips];
        for (int k = 0; k < numDrips; k++)
            freqs[k] = 750f + (float)(rng.NextDouble() * 250.0);

        for (int i = 0; i < n; i++)
        {
            int   di      = i / dripSamples;
            int   offset  = i % dripSamples;
            float envTime = offset / (float)SR;
            float env     = Mathf.Exp(-envTime / 0.018f);

            if (env > 0.005f)
            {
                float t    = i / (float)SR;
                float freq = freqs[Mathf.Min(di, numDrips - 1)];
                // Gota: tono puro con decaimiento + clic inicial de ruido
                float noise = di < numDrips ? (float)(rng.NextDouble() * 2 - 1) * 0.25f : 0f;
                d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * env * 0.55f;
            }
        }
        return MakeClip("Drip", d);
    }

    // Presencia de sala: ruido muy suave de fondo
    AudioClip GenerateNoise()
    {
        int    n   = Mathf.RoundToInt(SR * LOOP);
        float[] d  = new float[n];
        var rng    = new System.Random(13);
        float prev = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);
            prev = Mathf.Lerp(prev, raw, 0.022f);
            d[i] = prev * 0.32f;
        }
        return MakeClip("Noise", d);
    }

    static AudioClip MakeClip(string label, float[] data)
    {
        var c = AudioClip.Create("Ambient_" + label, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
}
