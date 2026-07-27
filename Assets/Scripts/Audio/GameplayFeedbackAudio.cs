using UnityEngine;

// Feedback sonoro al aplicar herramientas al paciente y al finalizar la simulación.
// Coloca en cualquier GameObject de la escena Piso Quirofano.
public class GameplayFeedbackAudio : MonoBehaviour
{
    [Range(0f, 1f)] public float correctVolume = 0.75f;
    [Range(0f, 1f)] public float wrongVolume   = 0.80f;
    [Range(0f, 1f)] public float victoryVolume = 0.85f;
    [Range(0f, 1f)] public float defeatVolume  = 0.75f;
    [Range(0f, 1f)] public float paroVolume    = 0.90f;

    AudioSource _src;
    AudioClip _correctClip;
    AudioClip _wrongClip;
    AudioClip _victoryClip;
    AudioClip _defeatClip;
    AudioClip _paroClip;

    PatientFSM _fsm;
    const int SR = 44100;

    void Awake()
    {
        _src              = gameObject.AddComponent<AudioSource>();
        _src.spatialBlend = 0f;
        _src.playOnAwake  = false;
        _src.priority     = 64;

        _correctClip = GenerateCorrect();
        _wrongClip   = GenerateWrong();
        _victoryClip = GenerateVictory();
        _defeatClip  = GenerateDefeat();
        _paroClip    = GenerateParo();
    }

    void Start()
    {
        _fsm = FindFirstObjectByType<PatientFSM>();
        if (_fsm == null) return;
        _fsm.OnCorrectTool   += _ => _src.PlayOneShot(_correctClip, correctVolume);
        _fsm.OnWrongTool     += (_, __) => _src.PlayOneShot(_wrongClip, wrongVolume);
        _fsm.OnCriticalError += _ => _src.PlayOneShot(_paroClip, paroVolume);
        _fsm.OnSimulationEnd += HandleSimulationEnd;
    }

    void OnDestroy()
    {
        if (_fsm == null) return;
        _fsm.OnCriticalError -= _ => _src.PlayOneShot(_paroClip, paroVolume);
        _fsm.OnSimulationEnd -= HandleSimulationEnd;
    }

    void HandleSimulationEnd(bool victory)
    {
        _src.Stop();
        _src.PlayOneShot(victory ? _victoryClip : _defeatClip,
                         victory ? victoryVolume : defeatVolume);
    }

    // Herramienta correcta: acorde ascendente Do-Mi-Sol (chime médico)
    AudioClip GenerateCorrect()
    {
        float dur    = 0.55f;
        int   n      = Mathf.RoundToInt(SR * dur);
        float[] d    = new float[n];
        float[] notes = { 523.25f, 659.25f, 783.99f };  // C5 E5 G5
        float[] onsets = { 0f, 0.12f, 0.25f };

        for (int ni = 0; ni < notes.Length; ni++)
        {
            int start = Mathf.RoundToInt(onsets[ni] * SR);
            for (int i = start; i < n; i++)
            {
                float t   = (i - start) / (float)SR;
                float env = Mathf.Exp(-t * 5.5f);
                d[i] += Mathf.Sin(2f * Mathf.PI * notes[ni] * t) * env * 0.55f
                       + Mathf.Sin(2f * Mathf.PI * notes[ni] * 2f * t) * env * 0.12f;
            }
        }
        Normalize(d, 0.88f);
        return MakeClip("Correct", d);
    }

    // Herramienta incorrecta: disonancia corta (cluster descendente)
    AudioClip GenerateWrong()
    {
        float dur  = 0.45f;
        int   n    = Mathf.RoundToInt(SR * dur);
        float[] d  = new float[n];
        var rng    = new System.Random(7);

        for (int i = 0; i < n; i++)
        {
            float t    = i / (float)SR;
            float tNorm = i / (float)n;
            float env  = Mathf.Exp(-tNorm * 7f);
            float noise = (float)(rng.NextDouble() * 2 - 1) * 0.20f;

            // Dos tonos disonantes que caen
            float f1 = Mathf.Lerp(320f, 160f, tNorm);
            float f2 = Mathf.Lerp(290f, 145f, tNorm);
            d[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t)
                  + Mathf.Sin(2f * Mathf.PI * f2 * t) + noise) * env * 0.50f;
        }
        Normalize(d, 0.88f);
        return MakeClip("Wrong", d);
    }

    // Victoria: fanfare corta ascendente (Do-Sol-Do-Mi-Sol)
    AudioClip GenerateVictory()
    {
        float dur      = 2.0f;
        int   n        = Mathf.RoundToInt(SR * dur);
        float[] d      = new float[n];

        // Notas de la fanfare con tiempos de onset
        float[] freqs  = { 392f, 523.25f, 659.25f, 783.99f, 1046.5f };
        float[] onsets = { 0f,   0.18f,   0.36f,   0.54f,   0.72f   };
        float[] durs   = { 0.30f, 0.30f,  0.30f,   0.30f,   0.80f   };

        for (int ni = 0; ni < freqs.Length; ni++)
        {
            int start  = Mathf.RoundToInt(onsets[ni] * SR);
            int notLen = Mathf.RoundToInt(durs[ni]   * SR);
            for (int i = start; i < Mathf.Min(start + notLen + SR, n); i++)
            {
                float t   = (i - start) / (float)SR;
                float env = t < 0.02f
                    ? t / 0.02f
                    : Mathf.Exp(-(t - 0.02f) * 3.5f) * 0.85f + 0.15f * Mathf.Exp(-(t - 0.02f) * 0.8f);
                float hz  = freqs[ni];
                d[i] += (Mathf.Sin(2f * Mathf.PI * hz * t) * 0.60f
                       + Mathf.Sin(2f * Mathf.PI * hz * 2f * t) * 0.20f
                       + Mathf.Sin(2f * Mathf.PI * hz * 3f * t) * 0.08f) * env;
            }
        }
        Normalize(d, 0.90f);
        return MakeClip("Victory", d);
    }

    // Derrota: tres tonos descendentes graves y lentos
    AudioClip GenerateDefeat()
    {
        float dur      = 2.2f;
        int   n        = Mathf.RoundToInt(SR * dur);
        float[] d      = new float[n];

        float[] freqs  = { 220f, 174.61f, 130.81f };   // La3 → Fa3 → Do3
        float[] onsets = { 0f,   0.55f,   1.15f    };
        float[] durs2  = { 0.70f, 0.70f,  1.00f    };

        for (int ni = 0; ni < freqs.Length; ni++)
        {
            int start  = Mathf.RoundToInt(onsets[ni] * SR);
            int notLen = Mathf.RoundToInt(durs2[ni]  * SR);
            for (int i = start; i < Mathf.Min(start + notLen + SR, n); i++)
            {
                float t   = (i - start) / (float)SR;
                float env = Mathf.Exp(-t * 1.8f) * 0.80f + 0.20f * Mathf.Exp(-t * 0.4f);
                float hz  = freqs[ni];
                d[i] += (Mathf.Sin(2f * Mathf.PI * hz * t) * 0.65f
                       + Mathf.Sin(2f * Mathf.PI * hz * 2f * t) * 0.18f) * env;
            }
        }
        Normalize(d, 0.90f);
        return MakeClip("Defeat", d);
    }

    // Paro cardíaco: golpe grave + sting metálico descendente (impacto dramático)
    AudioClip GenerateParo()
    {
        float dur  = 1.4f;
        int   n    = Mathf.RoundToInt(SR * dur);
        float[] d  = new float[n];
        var rng    = new System.Random(88);

        for (int i = 0; i < n; i++)
        {
            float t     = i / (float)SR;
            float tNorm = i / (float)n;

            // Golpe bajo (thud): 55 Hz + 80 Hz, decaimiento rápido
            float thudEnv = Mathf.Exp(-t * 8f);
            float thud    = (Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.70f
                           + Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.40f) * thudEnv;

            // Sting metálico: frecuencia que cae de 880 → 220 Hz (glide descendente)
            float stingFreq = Mathf.Lerp(880f, 220f, Mathf.Pow(tNorm, 0.5f));
            float stingEnv  = Mathf.Exp(-t * 3.5f);
            float sting     = Mathf.Sin(2f * Mathf.PI * stingFreq * t) * stingEnv * 0.55f;

            // Ruido de impacto inicial (transiente)
            float noise     = (float)(rng.NextDouble() * 2 - 1);
            float noiseEnv  = Mathf.Exp(-t * 30f);
            float transient = noise * noiseEnv * 0.35f;

            d[i] = thud + sting + transient;
        }

        Normalize(d, 0.92f);
        return MakeClip("Paro", d);
    }

    static void Normalize(float[] d, float target)
    {
        float peak = 0f;
        foreach (float s in d) { float a = Mathf.Abs(s); if (a > peak) peak = a; }
        if (peak < 0.0001f) return;
        float scale = target / peak;
        for (int i = 0; i < d.Length; i++) d[i] *= scale;
    }

    static AudioClip MakeClip(string label, float[] data)
    {
        var c = AudioClip.Create("Feedback_" + label, data.Length, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
}
