using UnityEngine;
using UnityEngine.SceneManagement;

// Música ambiental de fondo para escenas de menú (calma, modo mayor).
// Persiste entre escenas (DontDestroyOnLoad). Se destruye al entrar al quirófano.
public class MenuMusicAmbient : MonoBehaviour
{
    public static MenuMusicAmbient Instance { get; private set; }

    [Range(0f, 1f)] public float volume = 0.28f;

    AudioSource _src;
    const int   SR   = 22050;
    const float LOOP = 40f;

    void Awake()
    {
        // Singleton: si ya hay una instancia, destruir este duplicado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _src              = gameObject.AddComponent<AudioSource>();
        _src.spatialBlend = 0f;
        _src.loop         = true;
        _src.volume       = volume;
        _src.priority     = 130;
        _src.playOnAwake  = false;
        _src.clip         = GeneratePad();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Piso Quirofano")
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameSettingsManager.EnsureExists();
        _src.volume = volume * GameSettingsManager.MusicVolume;
        GameSettingsManager.OnMusicVolumeChanged += v => _src.volume = volume * v;
        _src.Play();
    }

    // Cuerdas en trémolo — Dm7 cinematográfico
    AudioClip GeneratePad()
    {
        int     n = Mathf.RoundToInt(SR * LOOP);
        float[] d = new float[n];

        // Dm7: D2 drone grave, D3, F3, A3, C4, A4 shimmer
        float[] freqs = { 73.4f,  146.8f, 174.6f, 220.0f, 261.6f, 440.0f };
        float[] amps  = { 0.28f,  0.24f,  0.22f,  0.20f,  0.15f,  0.07f };

        // 3 copias ligeramente desafinadas por nota → efecto ensemble de cuerdas
        float[] detune = { 1.0000f, 1.0012f, 0.9988f };

        float tremoloRate  = 7.2f;   // ~7Hz → trémolo de arco
        float tremoloDepth = 0.52f;
        float buildRate    = 0.05f;  // oleada dramática lenta

        var rng = new System.Random(77);

        for (int i = 0; i < n; i++)
        {
            float t     = i / (float)SR;
            float tNorm = i / (float)n;

            // Trémolo: simula el arco de cuerda
            float tremolo = 1f - tremoloDepth
                          + tremoloDepth * Mathf.Abs(Mathf.Sin(Mathf.PI * tremoloRate * t));

            // Construcción dramática: sube en la primera mitad, baja al final
            float build = 0.65f + 0.35f * Mathf.Sin(Mathf.PI * tNorm);

            float s = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                for (int k = 0; k < detune.Length; k++)
                {
                    float hz = freqs[f] * detune[k];
                    // Fundamental + armónicos para timbre de cuerda
                    s += Mathf.Sin(2f * Mathf.PI * hz        * t) * amps[f] * 0.58f
                       + Mathf.Sin(2f * Mathf.PI * hz * 2f   * t) * amps[f] * 0.24f
                       + Mathf.Sin(2f * Mathf.PI * hz * 3f   * t) * amps[f] * 0.11f
                       + Mathf.Sin(2f * Mathf.PI * hz * 4f   * t) * amps[f] * 0.05f;
                }
            }
            s /= detune.Length; // normalizar las 3 copias

            // Ruido de arco muy sutil
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.005f;

            d[i] = (s * tremolo * build + noise) * 0.95f;
        }

        // Fade in/out para loop sin click
        int fade = Mathf.RoundToInt(SR * 5f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }

        var c = AudioClip.Create("MenuCinematic", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
