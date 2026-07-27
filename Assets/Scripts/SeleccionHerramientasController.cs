using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SeleccionHerramientasController : MonoBehaviour
{
    [System.Serializable]
    public class ToolCard
    {
        public string     toolId;
        public GameObject cardRoot;
        public Image      cardBg;
        public Image      checkOverlay;
        public Image      checkIcon;
    }

    [Header("Tool Cards")]
    public List<ToolCard> toolCards = new List<ToolCard>();

    [Header("UI")]
    public TextMeshProUGUI txtCounter;
    public GameObject      btnIngresar;

    [Header("Case Context")]
    public TextMeshProUGUI txtCaseReminder;

    // ── Colores ──────────────────────────────────────────────────────────
    static readonly Color C_CORRECT_BG  = new Color(0.04f, 0.22f, 0.10f);
    static readonly Color C_CORRECT_OVR = new Color(0.10f, 0.90f, 0.45f, 0.18f);
    static readonly Color C_CORRECT_CHK = new Color(0.15f, 0.95f, 0.50f);
    static readonly Color C_WRONG_BG    = new Color(0.30f, 0.04f, 0.04f);
    static readonly Color C_WRONG_OVR   = new Color(0.95f, 0.15f, 0.10f, 0.35f);
    static readonly Color C_NORMAL_BG   = new Color(0.10f, 0.14f, 0.18f, 0.95f);
    static readonly Color C_HIDDEN      = new Color(0, 0, 0, 0);
    static readonly Color C_YELLOW      = new Color(1.00f, 0.75f, 0.10f);
    static readonly Color C_RED         = new Color(0.95f, 0.25f, 0.25f);
    static readonly Color C_GREEN       = new Color(0.15f, 0.95f, 0.50f);

    // ── HUD procedural (feedback penalización) ───────────────────────────
    CanvasGroup     _penaltyCG;
    TextMeshProUGUI _txtPenalty;

    bool _penaltyFlashing;

    // ── Lifecycle ────────────────────────────────────────────────────────
    void Start()
    {
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();

        // El timer ya debería estar corriendo desde ReportePaciente,
        // pero si se carga esta escena directamente lo arrancamos aquí.
        SessionTimer.EnsureExists().StartTimer();

        BuildHUD();
        ApplyCaseReminder();
        RefreshAll();
    }

    void Update()
    {
        var st = SessionTimer.Instance;
        if (st != null && st.Expired && !_penaltyFlashing)
            OnTimerExpired();
    }

    // ── HUD ──────────────────────────────────────────────────────────────
    void BuildHUD()
    {
        // El timer lo gestiona SessionTimer con su propio HUD persistente.
        // Aquí sólo creamos el panel de feedback de penalización.
        var root = new GameObject("_SelPenaltyHUD");
        root.transform.SetParent(null);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        // Panel de penalización (centro-superior, aparece brevemente)
        var penPanel = MkPanel(root.transform, new Color(0.55f, 0.04f, 0.04f, 0.90f));
        _penaltyCG = penPanel.AddComponent<CanvasGroup>();
        _penaltyCG.alpha          = 0f;
        _penaltyCG.blocksRaycasts = false;
        var ppRT = penPanel.GetComponent<RectTransform>();
        ppRT.anchorMin        = new Vector2(0.5f, 1); ppRT.anchorMax = new Vector2(0.5f, 1);
        ppRT.pivot            = new Vector2(0.5f, 1);
        ppRT.sizeDelta        = new Vector2(560, 80);
        ppRT.anchoredPosition = new Vector2(0, -110);

        _txtPenalty = MkTMP(penPanel.transform, "HERRAMIENTA INCORRECTA  —  -5 seg", 22, Color.white, FontStyles.Bold);
        var ptRT = _txtPenalty.GetComponent<RectTransform>();
        ptRT.anchorMin = Vector2.zero; ptRT.anchorMax = Vector2.one;
        ptRT.offsetMin = new Vector2(8, 4); ptRT.offsetMax = new Vector2(-8, -4);
        _txtPenalty.alignment = TextAlignmentOptions.Center;
    }

    void OnDestroy()
    {
        var hud = GameObject.Find("_SelPenaltyHUD");
        if (hud != null) Destroy(hud);
    }

    // ── Caso ─────────────────────────────────────────────────────────────
    void ApplyCaseReminder()
    {
        if (txtCaseReminder == null) return;
        var d = PatientCaseManager.Instance?.CurrentCase;
        if (d == null) return;
        bool isHemo = d.caseType == CaseType.HemorragiaActiva;
        txtCaseReminder.text = isHemo
            ? "CASO: HEMORRAGIA ACTIVA — Selecciona las 5 herramientas correctas"
            : "CASO: VÍA AÉREA BLOQUEADA — Selecciona las 5 herramientas correctas";
        txtCaseReminder.color = isHemo ? C_RED : new Color(0.25f, 0.60f, 1.00f);
    }

    // ── Clic en herramienta ──────────────────────────────────────────────
    public void OnToolClicked(int index)
    {
        if (index < 0 || index >= toolCards.Count) return;
        if (_penaltyFlashing) return;

        var mgr = PatientCaseManager.Instance;
        string tid = toolCards[index].toolId;

        if (mgr.IsCorrectTool(tid))
        {
            mgr.ToggleTool(tid);
            RefreshAll();
        }
        else
        {
            StartCoroutine(FlashWrong(index));
            SessionTimer.Instance?.ApplyPenalty();
        }
    }

    IEnumerator FlashWrong(int index)
    {
        _penaltyFlashing = true;
        var card = toolCards[index];

        // Flash rojo
        if (card.cardBg      != null) card.cardBg.color      = C_WRONG_BG;
        if (card.checkOverlay != null) card.checkOverlay.color = C_WRONG_OVR;
        if (card.checkIcon   != null) card.checkIcon.color   = C_RED;
        foreach (var t in card.cardRoot.GetComponentsInChildren<TextMeshProUGUI>())
            t.color = C_RED;

        // Mostrar panel de penalización
        if (_penaltyCG != null) _penaltyCG.alpha = 1f;

        yield return new WaitForSeconds(0.85f);

        // Restaurar carta
        RefreshCard(index);

        // Fade out del panel de penalización
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            if (_penaltyCG != null) _penaltyCG.alpha = 1f - elapsed / 0.4f;
            yield return null;
        }
        if (_penaltyCG != null) _penaltyCG.alpha = 0f;

        _penaltyFlashing = false;
    }

    // ── Refresh visual ───────────────────────────────────────────────────
    void RefreshAll()
    {
        for (int i = 0; i < toolCards.Count; i++)
            RefreshCard(i);

        var mgr      = PatientCaseManager.Instance;
        int count    = mgr.SelectedTools.Count;
        int required = PatientCaseManager.RequiredToolCount;
        bool ready   = count == required;

        if (txtCounter != null)
        {
            txtCounter.text = ready
                ? $"✓  SELECCIÓN COMPLETA — {count}/{required} herramientas"
                : $"⚠  Selecciona {required} herramientas correctas  ({count}/{required})";
            txtCounter.color = ready ? C_GREEN : C_YELLOW;
        }

        if (btnIngresar != null)
            btnIngresar.SetActive(mgr.ReadyToSimulate());
    }

    void RefreshCard(int i)
    {
        var mgr  = PatientCaseManager.Instance;
        var card = toolCards[i];
        bool sel = mgr.IsToolSelected(card.toolId);

        if (card.cardBg      != null) card.cardBg.color      = sel ? C_CORRECT_BG  : C_NORMAL_BG;
        if (card.checkOverlay != null) card.checkOverlay.color = sel ? C_CORRECT_OVR : C_HIDDEN;
        if (card.checkIcon   != null) card.checkIcon.color   = sel ? C_CORRECT_CHK : C_HIDDEN;
        foreach (var t in card.cardRoot.GetComponentsInChildren<TextMeshProUGUI>())
            t.color = sel ? C_GREEN : new Color(0.90f, 0.95f, 1.00f);
    }

    void OnTimerExpired()
    {
        // Sin tiempo → bloquear selección y mostrar mensaje
        if (btnIngresar != null) btnIngresar.SetActive(false);
        if (txtCounter  != null)
        {
            txtCounter.text  = "⏱  TIEMPO AGOTADO";
            txtCounter.color = C_RED;
        }
        if (_penaltyCG != null) _penaltyCG.alpha = 0f;
    }

    // ── Botones ──────────────────────────────────────────────────────────
    public void BtnIngresar()
    {
        if (!PatientCaseManager.Instance.ReadyToSimulate()) return;
        SceneManager.LoadScene("Piso Quirofano");
    }

    public void BtnDiagnostico()
    {
        SceneManager.LoadScene("ReportePaciente");
    }

    // ── Helpers UI procedural ────────────────────────────────────────────
    static GameObject MkPanel(Transform parent, Color color)
    {
        var go  = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    static TextMeshProUGUI MkTMP(Transform parent, string text, float size, Color color, FontStyles style)
    {
        var go  = new GameObject("TMP");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = false;
        return tmp;
    }
}
