using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(SphereCollider))]
public class InjuryZone : MonoBehaviour
{
    [Header("Interaction")]
    public float triggerRadius      = 0.5f;
    public int   countdownSeconds   = 5;
    public float retireMessageDelay = 2f;

    SphereCollider _collider;
    Coroutine      _countdown;
    MedicalTool    _currentTool;

    AudioSource _tickSrc;
    AudioClip   _tickClip;

    // Overlay compartido entre todas las InjuryZones (static)
    static CanvasGroup     _cg;
    static TextMeshProUGUI _label;

    static readonly Color colorIntervene = new Color(1.00f, 0.75f, 0.10f, 1f);
    static readonly Color colorRetire    = new Color(0.20f, 0.85f, 0.40f, 1f);

    void Awake()
    {
        _collider           = GetComponent<SphereCollider>();
        _collider.isTrigger = true;
        _collider.radius    = triggerRadius;
    }

    void Start()
    {
        EnsureOverlay();

        _tickSrc              = gameObject.AddComponent<AudioSource>();
        _tickSrc.spatialBlend = 0f;
        _tickSrc.playOnAwake  = false;
        _tickSrc.priority     = 100;
        _tickClip             = GenerateTick();
    }

    // ── Overlay ScreenSpaceOverlay (sortingOrder 98, debajo de VRFeedbackPopup/ResultsController)
    static void EnsureOverlay()
    {
        // Si el objeto fue destruido (ej. al reiniciar Play Mode), Unity devuelve true en == null
        if (_cg != null) return;

        var root = new GameObject("_InjuryCountdownHUD");
        Object.DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 98;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();

        _cg                = root.AddComponent<CanvasGroup>();
        _cg.alpha          = 0f;
        _cg.interactable   = false;
        _cg.blocksRaycasts = false;

        // Panel oscuro anclado arriba-centro
        var panelGO  = new GameObject("Panel");
        panelGO.transform.SetParent(root.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.02f, 0.04f, 0.10f, 0.90f);
        var panelRT  = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 1f);
        panelRT.anchorMax        = new Vector2(0.5f, 1f);
        panelRT.pivot            = new Vector2(0.5f, 1f);
        panelRT.sizeDelta        = new Vector2(700f, 120f);
        panelRT.anchoredPosition = new Vector2(0f, -24f);

        // Borde de color
        var borderGO  = new GameObject("Border");
        borderGO.transform.SetParent(panelGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = colorIntervene;
        var borderRT  = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0f, 0f);
        borderRT.anchorMax = new Vector2(1f, 0f);
        borderRT.pivot     = new Vector2(0.5f, 0f);
        borderRT.sizeDelta = new Vector2(0f, 3f);
        borderRT.offsetMin = new Vector2(0f, 0f);
        borderRT.offsetMax = new Vector2(0f, 3f);

        // Texto del countdown
        var textGO = new GameObject("CountdownText");
        textGO.transform.SetParent(panelGO.transform, false);
        _label            = textGO.AddComponent<TextMeshProUGUI>();
        _label.fontSize   = 56f;
        _label.alignment  = TextAlignmentOptions.Center;
        _label.fontStyle  = FontStyles.Bold;
        _label.color      = colorIntervene;
        _label.enableWordWrapping = false;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(16f, 8f);
        textRT.offsetMax = new Vector2(-16f, -8f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_countdown != null) return;

        MedicalTool tool = other.GetComponentInParent<MedicalTool>();
        if (tool == null || !tool.IsHeld) return;

        PatientFSM fsm = PatientFSM.Instance;
        if (fsm == null || fsm.IsFinished) return;

        _currentTool = tool;
        _countdown   = StartCoroutine(CountdownAndApply(tool, fsm));
    }

    void OnTriggerExit(Collider other)
    {
        MedicalTool tool = other.GetComponentInParent<MedicalTool>();
        if (tool != _currentTool) return;
        CancelCountdown();
    }

    void CancelCountdown()
    {
        if (_countdown != null) { StopCoroutine(_countdown); _countdown = null; }
        _currentTool = null;
        HideOverlay();
    }

    IEnumerator CountdownAndApply(MedicalTool tool, PatientFSM fsm)
    {
        for (int i = countdownSeconds; i >= 1; i--)
        {
            if (!tool.IsHeld) { CancelCountdown(); yield break; }
            ShowOverlay($"Interviniendo... {i}", colorIntervene);
            // Tick con pitch creciente: empieza normal, sube conforme queda menos tiempo
            if (_tickSrc != null && _tickClip != null)
            {
                _tickSrc.pitch = 1f + (countdownSeconds - i) * 0.15f;
                _tickSrc.PlayOneShot(_tickClip, 0.65f);
            }
            yield return new WaitForSeconds(1f);
        }

        if (!tool.IsHeld) { CancelCountdown(); yield break; }

        string toolId = tool.GetToolId();
        Debug.Log("[InjuryZone] Aplicando herramienta: " + toolId);

        _currentTool = null;
        _countdown   = null;
        fsm.ProcessTool(toolId);

        if (fsm.IsFinished) { HideOverlay(); yield break; }

        yield return new WaitForSeconds(retireMessageDelay);
        ShowOverlay("Retire la herramienta", colorRetire);
        yield return new WaitForSeconds(2.5f);
        HideOverlay();
    }

    static void ShowOverlay(string text, Color color)
    {
        if (_cg == null || _label == null) return;
        _label.text  = text;
        _label.color = color;
        _cg.alpha    = 1f;
    }

    static void HideOverlay()
    {
        if (_cg == null) return;
        _cg.alpha = 0f;
    }

    // Tick clínico: tono corto con decaimiento rápido (el pitch se ajusta desde fuera)
    static AudioClip GenerateTick()
    {
        const int SR  = 44100;
        float     dur = 0.07f;
        int       n   = Mathf.RoundToInt(SR * dur);
        float[]   d   = new float[n];
        var rng = new System.Random(17);

        for (int i = 0; i < n; i++)
        {
            float t     = i / (float)SR;
            float tNorm = i / (float)n;
            float env   = Mathf.Exp(-tNorm * 22f);
            float noise = (float)(rng.NextDouble() * 2 - 1) * 0.12f;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 1050f * t) * 0.75f + noise) * env;
        }

        float peak = 0f;
        foreach (float s in d) { float a = Mathf.Abs(s); if (a > peak) peak = a; }
        if (peak > 0.001f) for (int i = 0; i < n; i++) d[i] = d[i] / peak * 0.90f;

        var c = AudioClip.Create("InjuryTick", n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
