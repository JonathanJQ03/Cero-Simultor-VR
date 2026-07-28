using UnityEngine;

public class QuirofanoAmbient : MonoBehaviour
{
    [Header("Volúmenes")]
    [Range(0f, 1f)] public float ventilatorVolume    = 0.18f;  // respirador mecánico
    [Range(0f, 1f)] public float suctionVolume       = 0.30f;  // aspirador quirúrgico
    [Range(0f, 1f)] public float electrocauterVolume = 0.45f;  // electrocauterio (Bovie)
    [Range(0f, 1f)] public float laminarVolume       = 0.08f;  // flujo laminar de sala
    [Range(0f, 1f)] public float humVolume           = 0.18f;  // zumbido eléctrico

    AudioSource _srcVent;
    AudioSource _srcSuction;
    AudioSource _srcCautery;
    AudioSource _srcLaminar;
    AudioSource _srcHum;

    const int   SR   = 22050;
    // 24s: ventilador (6s x 4 ciclos) + electrocauterio suena ~1 vez cada 24s
    const float LOOP = 24f;

    void Awake()
    {
        _srcVent    = CreateSource("Ventilator",     ventilatorVolume);
        _srcSuction = CreateSource("Suction",        suctionVolume);
        _srcCautery = CreateSource("Electrocautery", electrocauterVolume);
        _srcLaminar = CreateSource("Laminar",        laminarVolume);
        _srcHum     = CreateSource("Hum",            humVolume);

        _srcVent.clip    = GenerateVentilator();
        _srcSuction.clip = GenerateSuction();
        _srcCautery.clip = GenerateElectrocautery();
        _srcLaminar.clip = GenerateLaminar();
        _srcHum.clip     = GenerateHum();
    }

    void Start()
    {
        _srcVent.Play();
        _srcSuction.Play();
        _srcCautery.Play();
        _srcLaminar.Play();
        _srcHum.Play();
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

    // Respirador mecánico: inhale (bomba activa, más agudo) vs exhale (pasivo, más grave)
    // 10 resp/min → periodo 6s → 4 ciclos exactos en 24s (loop limpio)
    AudioClip GenerateVentilator()
    {
        int    n      = Mathf.RoundToInt(SR * LOOP);
        float[] d     = new float[n];
        var rng       = new System.Random(99);
        float prevLow = 0f, prevMid = 0f;

        const float breathHz   = 10f / 60f;
        const float inhaleFrac = 0.38f;

        for (int i = 0; i < n; i++)
        {
            float t     = i / (float)SR;
            float phase = (t * breathHz) % 1f;
            float raw   = (float)(rng.NextDouble() * 2.0 - 1.0);

            float sample;
            if (phase < inhaleFrac)
            {
                float norm = phase / inhaleFrac;
                float env  = norm < 0.15f ? norm / 0.15f : 1f - (norm - 0.15f) / 0.85f * 0.15f;
                prevMid    = Mathf.Lerp(prevMid, raw, 0.055f);
                float valve = norm < 0.03f ? Mathf.Sin(Mathf.PI * norm / 0.03f) * 0.18f : 0f;
                sample = prevMid * (0.55f + env * 0.45f) + valve;
            }
            else
            {
                float norm = (phase - inhaleFrac) / (1f - inhaleFrac);
                float env  = Mathf.Exp(-norm * 2.2f) * 0.6f + 0.4f;
                prevLow    = Mathf.Lerp(prevLow, raw, 0.016f);
                sample     = prevLow * env * 0.90f;
            }

            d[i] = sample * 0.72f;
        }

        int fade = Mathf.RoundToInt(SR * 0.05f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }
        return MakeClip("Ventilator", d);
    }

    // Aspirador/succión quirúrgica: motor + vacío alta frecuencia + burbujeo
    AudioClip GenerateSuction()
    {
        int    n      = Mathf.RoundToInt(SR * LOOP);
        float[] d     = new float[n];
        var rng       = new System.Random(55);
        float prevVac = 0f, prevGurg = 0f;

        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)SR;
            float raw = (float)(rng.NextDouble() * 2.0 - 1.0);

            float motor = Mathf.Sin(2f * Mathf.PI * 82f  * t) * 0.40f
                        + Mathf.Sin(2f * Mathf.PI * 164f * t) * 0.18f
                        + Mathf.Sin(2f * Mathf.PI * 246f * t) * 0.08f;

            prevVac = Mathf.Lerp(prevVac, raw, 0.05f);
            float vacuum = (raw - prevVac) * 0.28f;

            prevGurg = Mathf.Lerp(prevGurg, raw, 0.007f);
            float gurgleAM = 0.4f + 0.6f * Mathf.Abs(
                Mathf.Sin(2f * Mathf.PI * 0.6f * t + Mathf.Sin(2f * Mathf.PI * 0.11f * t) * 4f));
            float gurgle = prevGurg * gurgleAM * 0.35f;

            d[i] = (motor * 0.40f + vacuum + gurgle) * 0.60f;
        }
        return MakeClip("Suction", d);
    }

    // Electrocauterio (Bovie): zumbido chirriante, 2 disparos por loop de 24s
    AudioClip GenerateElectrocautery()
    {
        int    n  = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];
        var rng   = new System.Random(33);

        AddBovie(d, rng, startSec: 5.0f,  durSec: 0.90f);
        AddBovie(d, rng, startSec: 16.0f, durSec: 0.45f);

        return MakeClip("Electrocautery", d);
    }

    void AddBovie(float[] d, System.Random rng, float startSec, float durSec)
    {
        int start = Mathf.RoundToInt(startSec * SR);
        int len   = Mathf.RoundToInt(durSec   * SR);
        if (start + len > d.Length) return;

        for (int j = 0; j < len; j++)
        {
            float localT = j / (float)SR;
            float norm   = j / (float)len;

            float env;
            if      (norm < 0.033f) env = norm / 0.033f;
            else if (norm > 0.94f)  env = (1f - norm) / 0.06f;
            else                    env = 1f;

            float raw  = (float)(rng.NextDouble() * 2.0 - 1.0);
            float buzz = Mathf.Sin(2f * Mathf.PI * 2800f * localT) * 0.45f
                       + Mathf.Sin(2f * Mathf.PI * 4200f * localT) * 0.30f
                       + Mathf.Sin(2f * Mathf.PI * 5600f * localT) * 0.15f
                       + raw * 0.10f;

            // AM rápida: imita el "chirrido" del Bovie
            float am = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 280f * localT);

            d[start + j] += buzz * am * env * 0.80f;
        }
    }

    // Flujo laminar de sala limpia: banda media suave y constante
    AudioClip GenerateLaminar()
    {
        int    n  = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];
        var rng   = new System.Random(42);
        float lp  = 0f;

        for (int i = 0; i < n; i++)
        {
            float raw  = (float)(rng.NextDouble() * 2.0 - 1.0);
            float prev = lp;
            lp  = Mathf.Lerp(lp, raw, 0.06f);
            d[i] = (lp - prev * 0.3f) * 0.48f;
        }

        int fade = Mathf.RoundToInt(SR * 0.2f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }
        return MakeClip("Laminar", d);
    }

    // Zumbido eléctrico: lámparas quirúrgicas + equipos (60 Hz + armónicos)
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
                  + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.04f) * 0.32f;
        }
        return MakeClip("Hum", d);
    }

    static AudioClip MakeClip(string label, float[] data)
    {
        var c = AudioClip.Create("Ambient_" + label, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
}
