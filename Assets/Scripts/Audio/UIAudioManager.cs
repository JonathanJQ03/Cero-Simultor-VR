using UnityEngine;

public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    [Range(0f, 1f)] public float clickVolume = 0.7f;

    AudioSource _src;
    AudioClip   _hoverClip;
    AudioClip   _clickClip;

    const int SR = 44100;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _src = gameObject.AddComponent<AudioSource>();
        _src.spatialBlend = 0f;
        _src.playOnAwake  = false;

        _hoverClip = GenerateHover();
        _clickClip = GenerateClick();
    }

    public void PlayHover() => _src.PlayOneShot(_hoverClip, hoverVolume * GameSettingsManager.FxVolume);
    public void PlayClick() => _src.PlayOneShot(_clickClip, clickVolume * GameSettingsManager.FxVolume);

    // Hover: tono suave corto (soft beep 600Hz)
    static AudioClip GenerateHover()
    {
        float dur = 0.06f;
        int   n   = Mathf.RoundToInt(SR * dur);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)SR;
            float env = Mathf.Clamp01(t / 0.005f) * Mathf.Clamp01((dur - t) / 0.02f);
            d[i] = Mathf.Sin(2f * Mathf.PI * 600f * t) * 0.4f * env
                 + Mathf.Sin(2f * Mathf.PI * 900f * t) * 0.2f * env;
        }
        var c = AudioClip.Create("UI_Hover", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // Click: tono más firme + transiente (800Hz descendente)
    static AudioClip GenerateClick()
    {
        float dur = 0.10f;
        int   n   = Mathf.RoundToInt(SR * dur);
        float[] d = new float[n];
        var rng   = new System.Random(55);
        for (int i = 0; i < n; i++)
        {
            float t     = i / (float)SR;
            float tNorm = i / (float)n;
            float env   = Mathf.Exp(-tNorm * 18f);
            float freq  = Mathf.Lerp(900f, 400f, tNorm);   // descend pitch
            float noise = (float)(rng.NextDouble() * 2 - 1) * 0.15f;
            d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) * 0.75f + noise) * env;
        }
        var c = AudioClip.Create("UI_Click", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
