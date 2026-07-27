using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ConfiguracionController : MonoBehaviour
{
    Slider          _sVol, _sFx, _sMusic;
    TextMeshProUGUI _lVolVal, _lFxVal, _lMusicVal;
    Toggle          _tCalBaja, _tCalMedia, _tCalAlta;
    Toggle          _tVigOn, _tVigOff;
    Toggle          _tManoDer, _tManoIzq;
    Toggle          _tMovCont, _tMovTel;

    bool _loading;

    static readonly Color BG       = new Color(0.04f, 0.06f, 0.13f);
    static readonly Color PANEL    = new Color(0.09f, 0.12f, 0.22f);
    static readonly Color TEAL     = new Color(0.18f, 0.78f, 0.88f);
    static readonly Color BLUE     = new Color(0.22f, 0.58f, 1.00f);
    static readonly Color TOFF     = new Color(0.12f, 0.15f, 0.25f);
    static readonly Color WHITE    = new Color(0.90f, 0.93f, 0.97f);
    static readonly Color GRAY     = new Color(0.50f, 0.55f, 0.65f);
    static readonly Color BTN_BLUE = new Color(0.10f, 0.25f, 0.55f);
    static readonly Color BTN_ORG  = new Color(0.45f, 0.22f, 0.04f);

    // Tamaños de fuente
    const float FS_TITLE   = 65f;
    const float FS_SECTION = 50f;
    const float FS_LABEL   = 38f;
    const float FS_SUB     = 34f;
    const float FS_TOGGLE  = 34f;
    const float FS_BTN     = 38f;

    void Awake()
    {
        GameSettingsManager.EnsureExists();
        BuildUI();
    }

    void Start()
    {
        LoadSettings();
        SetupListeners();
    }

    void BuildUI()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        var root = GetComponent<RectTransform>();
        (GetComponent<Image>() ?? gameObject.AddComponent<Image>()).color = BG;

        float y = -20f;

        // ── Título ──────────────────────────────────────────────────
        MkLabel(root, "CONFIGURACIÓN", FS_TITLE, TEAL, FontStyles.Bold,
            new Vector2(0.02f, 1), new Vector2(0.98f, 1),
            new Vector2(0, y - 72f), new Vector2(0, y));
        y -= 84f;

        // ── AUDIO ────────────────────────────────────────────────────
        MkSection(root, "AUDIO", ref y);
        (_sVol,   _lVolVal)   = MkSliderRow(root, "Volumen General",   GameSettingsManager.MasterVolume, ref y);
        (_sFx,    _lFxVal)    = MkSliderRow(root, "Efectos de Sonido", GameSettingsManager.FxVolume,    ref y);
        (_sMusic, _lMusicVal) = MkSliderRow(root, "Música Ambiental",  GameSettingsManager.MusicVolume, ref y);

        // ── CALIDAD GRÁFICA ──────────────────────────────────────────
        y -= 12f;
        MkSection(root, "CALIDAD GRÁFICA", ref y);
        (_tCalBaja, _tCalMedia, _tCalAlta) = MkToggle3(root, "Baja", "Media", "Alta", ref y);

        // ── CONFORT VR ───────────────────────────────────────────────
        y -= 12f;
        MkSection(root, "CONFORT VR", ref y);

        MkSubLabel(root, "Viñeteado:",      ref y);
        (_tVigOn,   _tVigOff)  = MkToggle2(root, "Activo",    "Inactivo",       0.30f, ref y);
        MkSubLabel(root, "Mano dominante:", ref y);
        (_tManoDer, _tManoIzq) = MkToggle2(root, "Derecha",   "Izquierda",      0.30f, ref y);
        MkSubLabel(root, "Movimiento:",     ref y);
        (_tMovCont, _tMovTel)  = MkToggle2(root, "Continuo",  "Teletransporte", 0.30f, ref y);

        // ── BOTONES ──────────────────────────────────────────────────
        y -= 20f;
        var btnV = MkButton(root, "VOLVER AL MENÚ",      BTN_BLUE,
            new Vector2(0.02f, 1), new Vector2(0.46f, 1), new Vector2(0, y - 64f), new Vector2(0, y));
        var btnR = MkButton(root, "VALORES POR DEFECTO", BTN_ORG,
            new Vector2(0.54f, 1), new Vector2(0.98f, 1), new Vector2(0, y - 64f), new Vector2(0, y));

        btnV.onClick.AddListener(() => { PlayerPrefs.Save(); SceneManager.LoadScene("MenuPrincipal"); });
        btnR.onClick.AddListener(RestoreDefaults);
    }

    // ── Listeners ────────────────────────────────────────────────────
    void SetupListeners()
    {
        _sVol.onValueChanged.AddListener(v   => { if (!_loading) { GameSettingsManager.SetMasterVolume(v); _lVolVal.text   = Pct(v); } });
        _sFx.onValueChanged.AddListener(v    => { if (!_loading) { GameSettingsManager.SetFxVolume(v);     _lFxVal.text    = Pct(v); } });
        _sMusic.onValueChanged.AddListener(v => { if (!_loading) { GameSettingsManager.SetMusicVolume(v);  _lMusicVal.text = Pct(v); } });

        WireGroup(new[] { _tCalBaja, _tCalMedia, _tCalAlta }, i =>
        {
            QualitySettings.SetQualityLevel(i, true);
            PlayerPrefs.SetInt(GameSettingsManager.K_QUALITY, i);
        });

        _tVigOn.onValueChanged.AddListener(on  => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_VIGNETTE, 1); _tVigOff.SetIsOnWithoutNotify(false); } });
        _tVigOff.onValueChanged.AddListener(on => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_VIGNETTE, 0); _tVigOn.SetIsOnWithoutNotify(false); } });

        _tManoDer.onValueChanged.AddListener(on  => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_HAND, 0); _tManoIzq.SetIsOnWithoutNotify(false); } });
        _tManoIzq.onValueChanged.AddListener(on  => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_HAND, 1); _tManoDer.SetIsOnWithoutNotify(false); } });

        _tMovCont.onValueChanged.AddListener(on => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_MOVMODE, 0); _tMovTel.SetIsOnWithoutNotify(false); } });
        _tMovTel.onValueChanged.AddListener(on  => { if (on && !_loading) { PlayerPrefs.SetInt(GameSettingsManager.K_MOVMODE, 1); _tMovCont.SetIsOnWithoutNotify(false); } });
    }

    void WireGroup(Toggle[] group, System.Action<int> onPick)
    {
        for (int i = 0; i < group.Length; i++)
        {
            int idx = i;
            group[i].onValueChanged.AddListener(on =>
            {
                if (!on || _loading) return;
                onPick(idx);
                for (int j = 0; j < group.Length; j++)
                    if (j != idx) group[j].SetIsOnWithoutNotify(false);
            });
        }
    }

    // ── Cargar / Restaurar ───────────────────────────────────────────
    void LoadSettings()
    {
        _loading = true;

        _sVol.value     = GameSettingsManager.MasterVolume;
        _sFx.value      = GameSettingsManager.FxVolume;
        _sMusic.value   = GameSettingsManager.MusicVolume;
        _lVolVal.text   = Pct(GameSettingsManager.MasterVolume);
        _lFxVal.text    = Pct(GameSettingsManager.FxVolume);
        _lMusicVal.text = Pct(GameSettingsManager.MusicVolume);

        int q = PlayerPrefs.GetInt(GameSettingsManager.K_QUALITY, 2);
        _tCalBaja.SetIsOnWithoutNotify(q == 0);
        _tCalMedia.SetIsOnWithoutNotify(q == 1);
        _tCalAlta.SetIsOnWithoutNotify(q == 2);

        bool vig = PlayerPrefs.GetInt(GameSettingsManager.K_VIGNETTE, 0) == 1;
        _tVigOn.SetIsOnWithoutNotify(vig);  _tVigOff.SetIsOnWithoutNotify(!vig);

        bool lh = PlayerPrefs.GetInt(GameSettingsManager.K_HAND, 0) == 1;
        _tManoDer.SetIsOnWithoutNotify(!lh); _tManoIzq.SetIsOnWithoutNotify(lh);

        int mov = PlayerPrefs.GetInt(GameSettingsManager.K_MOVMODE, 0);
        _tMovCont.SetIsOnWithoutNotify(mov == 0); _tMovTel.SetIsOnWithoutNotify(mov == 1);

        _loading = false;
    }

    void RestoreDefaults()
    {
        PlayerPrefs.SetFloat(GameSettingsManager.K_VOL,    0.8f);
        PlayerPrefs.SetFloat(GameSettingsManager.K_FX,     0.8f);
        PlayerPrefs.SetFloat(GameSettingsManager.K_MUSIC,  0.5f);
        PlayerPrefs.SetInt(GameSettingsManager.K_QUALITY,  2);
        PlayerPrefs.SetInt(GameSettingsManager.K_VIGNETTE, 0);
        PlayerPrefs.SetInt(GameSettingsManager.K_HAND,     0);
        PlayerPrefs.SetInt(GameSettingsManager.K_MOVMODE,  0);
        PlayerPrefs.Save();
        GameSettingsManager.ApplyAll();
        LoadSettings();
    }

    static string Pct(float v) => $"{Mathf.RoundToInt(v * 100)}%";

    // ── Helpers de construcción ──────────────────────────────────────

    void MkSection(RectTransform p, string text, ref float y)
    {
        MkLabel(p, text, FS_SECTION, TEAL, FontStyles.Bold,
            new Vector2(0.02f, 1), new Vector2(0.98f, 1),
            new Vector2(0, y - 52f), new Vector2(0, y));
        var line = new GameObject("Line");
        line.transform.SetParent(p, false);
        var lrt = line.AddComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.02f, 1); lrt.anchorMax = new Vector2(0.98f, 1);
        lrt.offsetMin = new Vector2(0, y - 54f); lrt.offsetMax = new Vector2(0, y - 52f);
        line.AddComponent<Image>().color = TEAL * 0.5f;
        y -= 62f;
    }

    // La sub-etiqueta se dibuja en la misma fila que el toggle; NO avanza y
    void MkSubLabel(RectTransform p, string text, ref float y)
    {
        MkLabel(p, text, FS_SUB, GRAY, FontStyles.Normal,
            new Vector2(0.02f, 1), new Vector2(0.28f, 1),
            new Vector2(0, y - 52f), new Vector2(0, y - 4f));
    }

    (Slider, TextMeshProUGUI) MkSliderRow(RectTransform p, string lbl, float val, ref float y)
    {
        // Etiqueta
        MkLabel(p, lbl, FS_LABEL, WHITE, FontStyles.Normal,
            new Vector2(0.02f, 1), new Vector2(0.30f, 1),
            new Vector2(0, y - 56f), new Vector2(0, y - 4f));

        // Cuerpo del slider
        var sgo = new GameObject("Slider_" + lbl);
        sgo.transform.SetParent(p, false);
        var srt = sgo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.31f, 1); srt.anchorMax = new Vector2(0.82f, 1);
        srt.offsetMin = new Vector2(0, y - 56f); srt.offsetMax = new Vector2(0, y - 8f);
        sgo.AddComponent<Image>().color = PANEL;

        // Fill Area → Fill
        var faGO = Sub(sgo.transform, "Fill Area");
        var faRT = faGO.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.3f); faRT.anchorMax = new Vector2(1, 0.7f);
        faRT.offsetMin = new Vector2(6, 0); faRT.offsetMax = new Vector2(-20, 0);

        var fGO = Sub(faGO.transform, "Fill");
        var fRT = fGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0, 1);
        fRT.sizeDelta = new Vector2(10, 0);
        fGO.AddComponent<Image>().color = BLUE;

        // Handle Slide Area → Handle (perilla grande y visible)
        var haGO = Sub(sgo.transform, "Handle Slide Area");
        var haRT = haGO.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(14, -10); haRT.offsetMax = new Vector2(-14, 10);

        var hGO = Sub(haGO.transform, "Handle");
        var hRT = hGO.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 0); hRT.anchorMax = new Vector2(0, 1);
        hRT.sizeDelta = new Vector2(40, 0);
        hGO.AddComponent<Image>().color = Color.white;

        var slider = sgo.AddComponent<Slider>();
        slider.fillRect      = fRT;
        slider.handleRect    = hRT;
        slider.targetGraphic = hGO.GetComponent<Image>();
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f; slider.maxValue = 1f;
        slider.value         = val;

        // Valor %
        var valLbl = MkLabel(p, Pct(val), FS_LABEL, BLUE, FontStyles.Bold,
            new Vector2(0.83f, 1), new Vector2(0.98f, 1),
            new Vector2(0, y - 56f), new Vector2(0, y - 4f));

        y -= 64f;
        return (slider, valLbl);
    }

    (Toggle, Toggle, Toggle) MkToggle3(RectTransform p, string a, string b, string c, ref float y)
    {
        float w = (0.96f - 0.02f) / 3f;
        var tA = MkToggleBtn(p, a, 0.02f,        0.02f + w - 0.01f, y);
        var tB = MkToggleBtn(p, b, 0.02f + w + 0.01f, 0.02f + 2*w,  y);
        var tC = MkToggleBtn(p, c, 0.02f + 2*w + 0.02f, 0.98f,      y);
        y -= 60f;
        return (tA, tB, tC);
    }

    (Toggle, Toggle) MkToggle2(RectTransform p, string a, string b, float xStart, ref float y)
    {
        var tA = MkToggleBtn(p, a, xStart,          xStart + 0.30f, y);
        var tB = MkToggleBtn(p, b, xStart + 0.32f,  xStart + 0.65f, y);
        y -= 60f;
        return (tA, tB);
    }

    Toggle MkToggleBtn(RectTransform p, string lbl, float x0, float x1, float y)
    {
        var go = new GameObject("Tog_" + lbl);
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, 1); rt.anchorMax = new Vector2(x1, 1);
        rt.offsetMin = new Vector2(2, y - 52f); rt.offsetMax = new Vector2(-2, y - 4f);

        var bgImg = go.AddComponent<Image>(); bgImg.color = TOFF;

        // Overlay activo
        var ck = Sub(go.transform, "Checkmark");
        var ckRT = ck.GetComponent<RectTransform>();
        ckRT.anchorMin = Vector2.zero; ckRT.anchorMax = Vector2.one;
        ckRT.offsetMin = Vector2.zero; ckRT.offsetMax = Vector2.zero;
        var ckImg = ck.AddComponent<Image>(); ckImg.color = BLUE;

        // Texto
        var txt = Sub(go.transform, "Label");
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4, 2); txtRT.offsetMax = new Vector2(-4, -2);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = lbl; tmp.fontSize = FS_TOGGLE;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = WHITE;

        var tog = go.AddComponent<Toggle>();
        tog.targetGraphic = bgImg;
        tog.graphic       = ckImg;
        tog.isOn          = false;
        return tog;
    }

    TextMeshProUGUI MkLabel(RectTransform p, string text, float size, Color color, FontStyles style,
        Vector2 anMin, Vector2 anMax, Vector2 off0, Vector2 off1)
    {
        string s = text.Replace(" ", "");
        var go = new GameObject("L_" + (s.Length > 10 ? s.Substring(0, 10) : s));
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anMin; rt.anchorMax = anMax;
        rt.offsetMin = off0; rt.offsetMax = off1;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    Button MkButton(RectTransform p, string text, Color bg,
        Vector2 anMin, Vector2 anMax, Vector2 off0, Vector2 off1)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anMin; rt.anchorMax = anMax;
        rt.offsetMin = off0; rt.offsetMax = off1;
        var img = go.AddComponent<Image>(); img.color = bg;

        var lgo = Sub(go.transform, "Lbl");
        lgo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        lgo.GetComponent<RectTransform>().anchorMax = Vector2.one;
        var tmp = lgo.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = FS_BTN; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center; tmp.color = Color.white;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cols = btn.colors;
        cols.highlightedColor = new Color(Mathf.Min(bg.r+0.15f,1), Mathf.Min(bg.g+0.15f,1), Mathf.Min(bg.b+0.15f,1));
        cols.pressedColor     = new Color(bg.r*0.7f, bg.g*0.7f, bg.b*0.7f);
        btn.colors = cols;
        return btn;
    }

    static GameObject Sub(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
