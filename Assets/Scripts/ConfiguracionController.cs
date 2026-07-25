using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ConfiguracionController : MonoBehaviour
{
    [Header("Audio")]
    public Slider sliderVolumenGeneral;
    public Slider sliderEfectosSonido;
    public Slider sliderMusicaAmbiental;
    public TextMeshProUGUI volGeneralLabel;
    public TextMeshProUGUI volFxLabel;
    public TextMeshProUGUI volMusicLabel;

    [Header("Graphics Quality")]
    public Toggle[] togglesCalidad;

    [Header("VR Comfort")]
    public Toggle toggleVigneadoOn;
    public Toggle toggleVigneadoOff;
    public Toggle toggleManoDerecha;
    public Toggle toggleManoIzquierda;
    public Toggle toggleMovContinuo;
    public Toggle toggleMovTeleport;

    [Header("Navigation")]
    public Button btnVolver;
    public Button btnRestaurar;

    const string K_VOL = "cfg_vol";
    const string K_FX = "cfg_fx";
    const string K_MUSIC = "cfg_music";
    const string K_QUALITY = "cfg_quality";
    const string K_VIGNETTE = "cfg_vignette";
    const string K_HAND = "cfg_hand";
    const string K_MOVMODE = "cfg_movmode";

    void Start()
    {
        LoadSettings();
        SetupListeners();
        if (btnVolver != null) btnVolver.onClick.AddListener(OnVolver);
        if (btnRestaurar != null) btnRestaurar.onClick.AddListener(RestoreDefaults);
    }

    void LoadSettings()
    {
        float vol = PlayerPrefs.GetFloat(K_VOL, 0.8f);
        float fx = PlayerPrefs.GetFloat(K_FX, 0.8f);
        float music = PlayerPrefs.GetFloat(K_MUSIC, 0.5f);

        SetSlider(sliderVolumenGeneral, vol);
        SetSlider(sliderEfectosSonido, fx);
        SetSlider(sliderMusicaAmbiental, music);
        UpdateLabel(volGeneralLabel, vol);
        UpdateLabel(volFxLabel, fx);
        UpdateLabel(volMusicLabel, music);
        AudioListener.volume = vol;

        SetToggleGroup(togglesCalidad, PlayerPrefs.GetInt(K_QUALITY, 2));

        bool vignette = PlayerPrefs.GetInt(K_VIGNETTE, 0) == 1;
        if (toggleVigneadoOn != null) toggleVigneadoOn.isOn = vignette;
        if (toggleVigneadoOff != null) toggleVigneadoOff.isOn = !vignette;

        bool leftHand = PlayerPrefs.GetInt(K_HAND, 0) == 1;
        if (toggleManoDerecha != null) toggleManoDerecha.isOn = !leftHand;
        if (toggleManoIzquierda != null) toggleManoIzquierda.isOn = leftHand;

        int mov = PlayerPrefs.GetInt(K_MOVMODE, 0);
        if (toggleMovContinuo != null) toggleMovContinuo.isOn = mov == 0;
        if (toggleMovTeleport != null) toggleMovTeleport.isOn = mov == 1;
    }

    void SetSlider(Slider s, float v) { if (s != null) s.value = v; }
    void UpdateLabel(TextMeshProUGUI lbl, float v) { if (lbl != null) lbl.text = $"{Mathf.RoundToInt(v * 100)}%"; }

    void SetToggleGroup(Toggle[] group, int idx)
    {
        if (group == null) return;
        for (int i = 0; i < group.Length; i++)
            if (group[i] != null) group[i].isOn = (i == idx);
    }

    void SetupListeners()
    {
        if (sliderVolumenGeneral != null)
            sliderVolumenGeneral.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(K_VOL, v);
                UpdateLabel(volGeneralLabel, v);
            });

        if (sliderEfectosSonido != null)
            sliderEfectosSonido.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(K_FX, v);
                UpdateLabel(volFxLabel, v);
            });

        if (sliderMusicaAmbiental != null)
            sliderMusicaAmbiental.onValueChanged.AddListener(v =>
            {
                PlayerPrefs.SetFloat(K_MUSIC, v);
                UpdateLabel(volMusicLabel, v);
            });

        if (togglesCalidad != null)
            for (int i = 0; i < togglesCalidad.Length; i++)
            {
                int idx = i;
                if (togglesCalidad[i] != null)
                    togglesCalidad[i].onValueChanged.AddListener(on =>
                    {
                        if (on) { QualitySettings.SetQualityLevel(idx); PlayerPrefs.SetInt(K_QUALITY, idx); }
                    });
            }

        if (toggleVigneadoOn != null)
            toggleVigneadoOn.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_VIGNETTE, 1); });
        if (toggleVigneadoOff != null)
            toggleVigneadoOff.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_VIGNETTE, 0); });

        if (toggleManoDerecha != null)
            toggleManoDerecha.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_HAND, 0); });
        if (toggleManoIzquierda != null)
            toggleManoIzquierda.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_HAND, 1); });

        if (toggleMovContinuo != null)
            toggleMovContinuo.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_MOVMODE, 0); });
        if (toggleMovTeleport != null)
            toggleMovTeleport.onValueChanged.AddListener(on => { if (on) PlayerPrefs.SetInt(K_MOVMODE, 1); });
    }

    void RestoreDefaults()
    {
        PlayerPrefs.SetFloat(K_VOL, 0.8f);
        PlayerPrefs.SetFloat(K_FX, 0.8f);
        PlayerPrefs.SetFloat(K_MUSIC, 0.5f);
        PlayerPrefs.SetInt(K_QUALITY, 2);
        PlayerPrefs.SetInt(K_VIGNETTE, 0);
        PlayerPrefs.SetInt(K_HAND, 0);
        PlayerPrefs.SetInt(K_MOVMODE, 0);
        LoadSettings();
    }

    void OnVolver()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MenuPrincipal");
    }
}
