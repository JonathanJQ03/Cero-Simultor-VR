using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultsController : MonoBehaviour
{
    public static ResultsController Instance { get; private set; }

    [Header("Colors")]
    public Color winAccent    = new Color(0.10f, 0.79f, 0.42f, 1f);
    public Color loseAccent   = new Color(0.80f, 0.20f, 0.20f, 1f);
    public Color overlayColor = new Color(0.02f, 0.04f, 0.09f, 0.93f);

    CanvasGroup     _cg;
    Image           _cardBorderImg;
    TextMeshProUGUI _badgeText;
    Image           _badgeBg;
    TextMeshProUGUI _titleText;
    TextMeshProUGUI _subtitleText;
    TextMeshProUGUI _statTime;
    TextMeshProUGUI _statErrors;
    TextMeshProUGUI _statResult;
    Image           _btnBorderImg;
    TextMeshProUGUI _btnText;

    bool       _showing;
    PatientFSM _fsm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        BuildUI();
    }

    void Start()
    {
        _fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        if (_fsm != null)
            _fsm.OnSimulationEnd += OnSimulationEnd;
    }

    void OnDestroy()
    {
        if (_fsm != null)
            _fsm.OnSimulationEnd -= OnSimulationEnd;
    }

    void OnSimulationEnd(bool success)
    {
        var gm = GameManager.Instance ?? FindObjectOfType<GameManager>();
        float elapsed = gm != null ? gm.GetElapsedTime() : 0f;
        int   errors  = _fsm != null ? _fsm.ErrorCount : 0;

        // Para victoria: esperar 6s para que la secuencia del VRFeedbackPopup termine primero
        // Para derrota: mostrar inmediatamente
        if (success)
            StartCoroutine(ShowDelayed(true, elapsed, errors, 6f));
        else
            ShowResult(false, elapsed, errors);
    }

    IEnumerator ShowDelayed(bool success, float elapsed, int errors, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowResult(success, elapsed, errors);
    }

    // anyKeyDown removed — only the on-screen button returns to menu

    public void ShowResult(bool success, float elapsed, int errors)
    {
        Color accent    = success ? winAccent : loseAccent;
        Color dimAccent = new Color(accent.r, accent.g, accent.b, 0.15f);

        if (_badgeText != null)
        {
            _badgeText.text  = success ? "SIMULACIÓN COMPLETADA" : "SIMULACIÓN FALLIDA";
            _badgeText.color = accent;
        }
        if (_badgeBg    != null) _badgeBg.color = dimAccent;

        if (_titleText  != null)
        {
            _titleText.text  = success ? "VICTORIA" : "PACIENTE FALLECIDO";
            _titleText.color = accent;
        }
        if (_subtitleText != null)
            _subtitleText.text = success
                ? "Paciente estabilizado con éxito"
                : "El paciente no pudo ser estabilizado";

        int min = Mathf.FloorToInt(elapsed / 60f);
        int sec = Mathf.FloorToInt(elapsed % 60f);
        if (_statTime   != null) _statTime.text   = $"{min:00}:{sec:00}";
        if (_statErrors != null) _statErrors.text = errors.ToString();
        if (_statResult != null)
        {
            _statResult.text  = success ? "Éxito" : "Fallido";
            _statResult.color = accent;
        }

        if (_cardBorderImg != null) _cardBorderImg.color = new Color(accent.r, accent.g, accent.b, 0.25f);
        if (_btnBorderImg  != null) _btnBorderImg.color  = new Color(accent.r, accent.g, accent.b, 0.45f);
        if (_btnText       != null) _btnText.color       = accent;

        _showing = true;
        StartCoroutine(FadeIn());
    }

    public void ReturnToMenu()
    {
        _showing = false;
        SceneManager.LoadScene("MenuPrincipal");
    }

    IEnumerator FadeIn()
    {
        _cg.blocksRaycasts = true;
        float t = 0f;
        while (t < 0.45f) { t += Time.deltaTime; _cg.alpha = t / 0.45f; yield return null; }
        _cg.alpha        = 1f;
        _cg.interactable = true;
    }

    // ── UI construction ───────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("ResultsCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        _cg                  = canvasGO.AddComponent<CanvasGroup>();
        _cg.alpha            = 0f;
        _cg.interactable     = false;   // prevent clicks while hidden
        _cg.blocksRaycasts   = false;

        // Full-screen dark overlay
        var overlayGO = NewRect("Overlay", canvasGO.transform);
        overlayGO.AddComponent<Image>().color = overlayColor;
        StretchFull(overlayGO);

        // Card — 1280 × 720 centered (fills ~67% of 1920×1080)
        var cardGO = NewRect("ResultsCard", canvasGO.transform);
        _cardBorderImg        = cardGO.AddComponent<Image>();
        _cardBorderImg.sprite = UISprite();
        _cardBorderImg.type   = Image.Type.Sliced;
        _cardBorderImg.color  = new Color(winAccent.r, winAccent.g, winAccent.b, 0.25f);
        CenterRect(cardGO.GetComponent<RectTransform>(), 1280f, 720f);

        var innerGO = NewRect("CardBg", cardGO.transform);
        innerGO.AddComponent<Image>().color = new Color(0.025f, 0.06f, 0.12f, 1f);
        StretchInset(innerGO, 1.5f);

        float y = 80f;

        // ── Badge ─────────────────────────────────────────────────────────
        var badgeGO = NewRect("Badge", cardGO.transform);
        _badgeBg        = badgeGO.AddComponent<Image>();
        _badgeBg.sprite = UISprite();
        _badgeBg.type   = Image.Type.Sliced;
        _badgeBg.color  = new Color(winAccent.r, winAccent.g, winAccent.b, 0.15f);
        TopCentered(badgeGO.GetComponent<RectTransform>(), 480f, 44f, y);

        _badgeText = NewTMP("BadgeText", badgeGO.transform, "SIMULACIÓN COMPLETADA",
            18f, winAccent, TextAlignmentOptions.Center, true);
        _badgeText.characterSpacing = 2.5f;
        StretchFull(_badgeText.gameObject, 12f, 6f);
        y += 68f;

        // ── Title ─────────────────────────────────────────────────────────
        _titleText = NewTMP("Title", cardGO.transform, "VICTORIA",
            110f, winAccent, TextAlignmentOptions.Center, true);
        _titleText.enableWordWrapping = false;
        TopStretch(_titleText.GetComponent<RectTransform>(), -80f, 136f, y);
        y += 148f;

        // ── Subtitle ──────────────────────────────────────────────────────
        _subtitleText = NewTMP("Subtitle", cardGO.transform,
            "Paciente estabilizado con éxito", 26f,
            new Color(0.35f, 0.53f, 0.68f, 1f), TextAlignmentOptions.Center, false);
        TopStretch(_subtitleText.GetComponent<RectTransform>(), -80f, 38f, y);
        y += 62f;

        // ── Stats row — 3 panels (anchor top-center, pivot top-left) ──────
        const float statsW = 1160f;
        float panW  = (statsW - 20f) / 3f;
        float xLeft = -statsW / 2f;

        string[] labels   = { "TIEMPO", "ERRORES", "RESULTADO" };
        string[] defaults = { "00:00",  "0",       "—" };
        var vals = new TextMeshProUGUI[3];

        for (int i = 0; i < 3; i++)
        {
            var sGO = NewRect("Stat" + i, cardGO.transform);
            sGO.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.17f, 1f);
            var sRT = sGO.GetComponent<RectTransform>();
            sRT.anchorMin        = new Vector2(0.5f, 1f);
            sRT.anchorMax        = new Vector2(0.5f, 1f);
            sRT.pivot            = new Vector2(0f,   1f);
            sRT.sizeDelta        = new Vector2(panW, 150f);
            sRT.anchoredPosition = new Vector2(xLeft + i * (panW + 10f), -y);

            var val = NewTMP("Val", sGO.transform, defaults[i], 54f,
                new Color(0.72f, 0.85f, 0.95f, 1f), TextAlignmentOptions.Center, true);
            var vRT = val.GetComponent<RectTransform>();
            vRT.anchorMin = new Vector2(0f, 0.40f); vRT.anchorMax = new Vector2(1f, 1f);
            vRT.offsetMin = new Vector2(4f, 8f);    vRT.offsetMax = new Vector2(-4f, -8f);

            var lbl = NewTMP("Lbl", sGO.transform, labels[i], 18f,
                new Color(0.30f, 0.44f, 0.56f, 1f), TextAlignmentOptions.Center, false);
            lbl.characterSpacing = 2f;
            var lRT = lbl.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 0f);   lRT.anchorMax = new Vector2(1f, 0.40f);
            lRT.offsetMin = new Vector2(4f, 8f);   lRT.offsetMax = new Vector2(-4f, 0f);

            vals[i] = val;
        }
        _statTime   = vals[0];
        _statErrors = vals[1];
        _statResult = vals[2];
        y += 168f;

        // ── Divider ───────────────────────────────────────────────────────
        var divGO = NewRect("Divider", cardGO.transform);
        divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.07f);
        TopStretch(divGO.GetComponent<RectTransform>(), -80f, 2f, y);
        y += 24f;

        // ── Button ────────────────────────────────────────────────────────
        var btnGO = NewRect("BtnMenu", cardGO.transform);
        _btnBorderImg        = btnGO.AddComponent<Image>();
        _btnBorderImg.sprite = UISprite();
        _btnBorderImg.type   = Image.Type.Sliced;
        _btnBorderImg.color  = new Color(winAccent.r, winAccent.g, winAccent.b, 0.45f);
        TopStretch(btnGO.GetComponent<RectTransform>(), -80f, 78f, y);

        var btnInner = NewRect("BtnBg", btnGO.transform);
        btnInner.AddComponent<Image>().color = new Color(0.04f, 0.09f, 0.17f, 0.6f);
        StretchInset(btnInner, 1f);

        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = _btnBorderImg;
        btn.onClick.AddListener(ReturnToMenu);

        _btnText = NewTMP("BtnTxt", btnGO.transform, "Volver al Menú Principal",
            28f, winAccent, TextAlignmentOptions.Center, true);
        _btnText.enableWordWrapping = false;
        StretchFull(_btnText.gameObject);
    }

    // ── Layout helpers ────────────────────────────────────────────────────

    static Sprite UISprite() =>
        Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

    static GameObject NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static TextMeshProUGUI NewTMP(string name, Transform parent, string text,
        float size, Color color, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        return t;
    }

    static void CenterRect(RectTransform rt, float w, float h)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);        rt.anchoredPosition = Vector2.zero;
    }

    static void TopCentered(RectTransform rt, float w, float h, float yOff)
    {
        rt.anchorMin = new Vector2(0.5f, 1f); rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);     rt.anchoredPosition = new Vector2(0f, -yOff);
    }

    static void TopStretch(RectTransform rt, float hInset, float h, float yOff)
    {
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(hInset, h); rt.anchoredPosition = new Vector2(0f, -yOff);
    }

    static void StretchFull(GameObject go, float hPad = 0f, float vPad = 0f)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(hPad, vPad); rt.offsetMax = new Vector2(-hPad, -vPad);
    }

    static void StretchInset(GameObject go, float inset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
    }
}
