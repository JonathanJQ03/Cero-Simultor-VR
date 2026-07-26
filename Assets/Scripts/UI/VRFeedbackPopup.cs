using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRFeedbackPopup : MonoBehaviour
{
    public static VRFeedbackPopup Instance { get; private set; }

    [Header("Duraciones")]
    public float displayDuration = 2.5f;
    public float fadeDuration    = 0.35f;

    [Header("Colores")]
    public Color correctColor  = new Color(0.10f, 0.72f, 0.20f, 0.95f);
    public Color wrongColor    = new Color(0.80f, 0.10f, 0.10f, 0.95f);
    public Color criticalColor = new Color(0.85f, 0.40f, 0.00f, 0.95f);
    public Color winColor      = new Color(0.05f, 0.55f, 0.85f, 0.97f);
    public Color loseColor     = new Color(0.30f, 0.00f, 0.00f, 0.97f);
    public Color infoColor     = new Color(0.20f, 0.20f, 0.60f, 0.95f);

    Image           _panelImg;
    TextMeshProUGUI _labelText;
    CanvasGroup     _cg;
    Coroutine       _routine;
    Coroutine       _sequenceRoutine;
    PatientFSM      _fsm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (_cg == null) BuildUI();
    }

    void Start()
    {
        _fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        if (_fsm != null)
        {
            _fsm.OnCorrectTool   += OnCorrect;
            _fsm.OnWrongTool     += OnWrong;
            _fsm.OnCriticalError += OnCritical;
            _fsm.OnSimulationEnd += OnSimulationEnd;
        }
    }

    void OnDestroy()
    {
        if (_fsm == null) return;
        _fsm.OnCorrectTool   -= OnCorrect;
        _fsm.OnWrongTool     -= OnWrong;
        _fsm.OnCriticalError -= OnCritical;
        _fsm.OnSimulationEnd -= OnSimulationEnd;
    }

    // ── FSM handlers ──────────────────────────────────────────────────────

    void OnCorrect(string toolId)
        => Show(FriendlyName(toolId) + " - Correcto", correctColor);

    void OnWrong(string toolId, string _)
        => Show(FriendlyName(toolId) + " - Incorrecto", wrongColor);

    void OnCritical(string toolId)
    {
        if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
        _sequenceRoutine = StartCoroutine(CriticalSequence(FriendlyName(toolId)));
    }

    void OnSimulationEnd(bool success)
    {
        if (success)
        {
            if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
            _sequenceRoutine = StartCoroutine(WinSequence());
        }
        else
        {
            if (_sequenceRoutine != null) StopCoroutine(_sequenceRoutine);
            ShowPersistent("PACIENTE FALLECIDO", loseColor);
        }
    }

    // ── Secuencias ────────────────────────────────────────────────────────

    IEnumerator CriticalSequence(string toolName)
    {
        // Alerta 1: [herramienta] - Error Critico
        Show(toolName + " - Error Critico", criticalColor);
        yield return new WaitForSeconds(displayDuration + fadeDuration + 0.1f);

        // Alerta 2: Paciente en paro
        Show("Paciente en paro", criticalColor);
    }

    IEnumerator WinSequence()
    {
        // Esperar que termine la alerta "- Correcto" de la última herramienta
        yield return new WaitForSeconds(displayDuration + fadeDuration + 0.2f);

        // Alerta: Paciente Estabilizando...
        SetPanel("Paciente Estabilizando...", correctColor);
        yield return FadeIn(0.2f);
        yield return new WaitForSeconds(1.8f);
        yield return FadeOut();

        yield return new WaitForSeconds(0.2f);

        // Final: HAS GANADO (permanente)
        ShowPersistent("HAS GANADO - Paciente Estabilizado!", winColor);
    }

    // ── API pública ───────────────────────────────────────────────────────

    public void Show(string message, Color color)
    {
        SetPanel(message, color);
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine());
    }

    public void ShowPersistent(string message, Color color)
    {
        if (_routine != null) StopCoroutine(_routine);
        SetPanel(message, color);
        if (_labelText != null) _labelText.fontSize = 32f;
        _routine = StartCoroutine(FadeIn(0.3f));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetPanel(string message, Color color)
    {
        if (_panelImg  != null) _panelImg.color = color;
        if (_labelText != null)
        {
            _labelText.text     = message;
            _labelText.fontSize = 26f;
        }
    }

    IEnumerator ShowRoutine()
    {
        yield return FadeIn(0.15f);
        yield return new WaitForSeconds(displayDuration);
        yield return FadeOut();
        if (_labelText != null) _labelText.fontSize = 26f;
    }

    IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        while (t < duration) { t += Time.deltaTime; _cg.alpha = t / duration; yield return null; }
        _cg.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        while (t < fadeDuration) { t += Time.deltaTime; _cg.alpha = 1f - t / fadeDuration; yield return null; }
        _cg.alpha = 0f;
    }

    string FriendlyName(string toolId)
    {
        switch (toolId)
        {
            case "Bisturi":         return "Bisturi";
            case "TijerasDeTrauma": return "Tijeras de Trauma";
            case "VendasHemo":      return "Vendas Hemostaticas";
            case "Gasas":           return "Gasas";
            case "Torniquete":      return "Torniquete";
            case "Epinefrina":      return "Epinefrina";
            case "Desfibrilador":   return "Desfibrilador";
            case "Laringoscopio":   return "Laringoscopio";
            case "CanulaDeGuedel":  return "Canula de Guedel";
            default:                return toolId;
        }
    }

    // ── Construcción UI ───────────────────────────────────────────────────

    void BuildUI()
    {
        var canvasGO = new GameObject("VRFeedbackCanvas");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        _cg       = canvasGO.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;

        var panelGO = new GameObject("FeedbackPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        _panelImg        = panelGO.AddComponent<Image>();
        _panelImg.color  = correctColor;
        _panelImg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        _panelImg.type   = Image.Type.Sliced;

        var panelRT              = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.20f, 1f);
        panelRT.anchorMax        = new Vector2(0.80f, 1f);
        panelRT.pivot            = new Vector2(0.5f,  1f);
        panelRT.sizeDelta        = new Vector2(0f, 75f);
        panelRT.anchoredPosition = new Vector2(0f, -8f);

        var textGO = new GameObject("FeedbackText");
        textGO.transform.SetParent(panelGO.transform, false);

        _labelText                   = textGO.AddComponent<TextMeshProUGUI>();
        _labelText.text              = "";
        _labelText.fontSize          = 26f;
        _labelText.fontStyle         = FontStyles.Bold;
        _labelText.color             = Color.white;
        _labelText.alignment         = TextAlignmentOptions.Center;
        _labelText.enableWordWrapping = false;

        var textRT    = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 6f);
        textRT.offsetMax = new Vector2(-16f, -6f);
    }
}
