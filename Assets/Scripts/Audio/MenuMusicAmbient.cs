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
    const float LOOP = 10f;

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

    // Pad suave en Do mayor (Do4-Mi4-Sol4-Do5) con respiración lenta
    AudioClip GeneratePad()
    {
        int     n   = Mathf.RoundToInt(SR * LOOP);
        float[] d   = new float[n];

        // Do mayor: C4=261.6, E4=329.6, G4=392.0, C5=523.3, G3=196.0
        float[] freqs = { 196.0f, 261.6f, 329.6f, 392.0f, 523.3f };
        float[] amps  = { 0.22f,  0.32f,  0.26f,  0.20f,  0.12f };

        float breathRate = 0.08f;   // respiración muy lenta (~1 ciclo / 12s)
        float lfoRate    = 0.05f;   // vibrato ultra lento

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;

            // Modulación de volumen suave (inhala/exhala)
            float breath = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * breathRate * t);

            // LFO de frecuencia muy sutil
            float lfo = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * lfoRate * t);

            float s = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                float hz = freqs[f] * lfo;
                s += Mathf.Sin(2f * Mathf.PI * hz * t)         * amps[f]
                   + Mathf.Sin(2f * Mathf.PI * hz * 2f * t)    * amps[f] * 0.12f;
            }

            d[i] = s * breath * 0.45f;
        }

        // Fade in/out para evitar click en el loop
        int fade = Mathf.RoundToInt(SR * 0.4f);
        for (int i = 0; i < fade; i++)
        {
            float t = i / (float)fade;
            d[i]         *= t;
            d[n - 1 - i] *= t;
        }

        var c = AudioClip.Create("MenuPad", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
