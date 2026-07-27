using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton persistente que aplica los ajustes de PlayerPrefs al juego.
// Se crea automáticamente en la primera escena que lo necesite.
public class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    // Claves PlayerPrefs (mismas que ConfiguracionController)
    public const string K_VOL      = "cfg_vol";
    public const string K_FX       = "cfg_fx";
    public const string K_MUSIC    = "cfg_music";
    public const string K_QUALITY  = "cfg_quality";
    public const string K_VIGNETTE = "cfg_vignette";
    public const string K_HAND     = "cfg_hand";
    public const string K_MOVMODE  = "cfg_movmode";
    public const string K_DIFF     = "cfg_difficulty";  // 0=Fácil 1=Normal 2=Difícil

    // Propiedades de solo lectura para que otros scripts lean los valores actuales
    public static float MasterVolume => PlayerPrefs.GetFloat(K_VOL,   0.8f);
    public static float FxVolume     => PlayerPrefs.GetFloat(K_FX,    0.8f);
    public static float MusicVolume  => PlayerPrefs.GetFloat(K_MUSIC, 0.5f);
    public static int   Difficulty   => PlayerPrefs.GetInt(K_DIFF, 1);

    // Segundos de countdown según dificultad
    public static int CountdownSeconds => Difficulty switch { 0 => 8, 2 => 3, _ => 5 };

    // Eventos para que los AudioSources reactivos actualicen su volumen al vuelo
    public static event System.Action<float> OnMusicVolumeChanged;
    public static event System.Action<float> OnFxVolumeChanged;
    public static event System.Action<float> OnMasterVolumeChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ApplyAll();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // Re-aplica el volumen master al cargar cada escena (AudioListener se recrea)
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyMasterVolume();

    public static void ApplyAll()
    {
        ApplyMasterVolume();
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(K_QUALITY, 2), true);
    }

    public static void ApplyMasterVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    // Llamado desde ConfiguracionController al cambiar sliders
    public static void SetMasterVolume(float v)
    {
        PlayerPrefs.SetFloat(K_VOL, v);
        AudioListener.volume = v;
        OnMasterVolumeChanged?.Invoke(v);
    }

    public static void SetMusicVolume(float v)
    {
        PlayerPrefs.SetFloat(K_MUSIC, v);
        OnMusicVolumeChanged?.Invoke(v);
    }

    public static void SetFxVolume(float v)
    {
        PlayerPrefs.SetFloat(K_FX, v);
        OnFxVolumeChanged?.Invoke(v);
    }

    public static void SetDifficulty(int d)
    {
        PlayerPrefs.SetInt(K_DIFF, d);
        PlayerPrefs.Save();
    }

    // Crea la instancia si no existe (para escenas que no la tienen en la jerarquía)
    public static void EnsureExists()
    {
        if (Instance == null)
            new GameObject("_GameSettingsManager").AddComponent<GameSettingsManager>();
    }
}
