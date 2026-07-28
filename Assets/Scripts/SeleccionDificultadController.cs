using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[DefaultExecutionOrder(-100)]
public class SeleccionDificultadController : MonoBehaviour
{
    static readonly Color C_BG       = new Color(0.04f, 0.07f, 0.12f);
    static readonly Color C_BASICO   = new Color(0.04f, 0.26f, 0.14f);
    static readonly Color C_AVANZADO = new Color(0.26f, 0.05f, 0.04f);
    static readonly Color C_BTN_MENU = new Color(0.10f, 0.14f, 0.20f);
    static readonly Color C_GREEN    = new Color(0.15f, 0.95f, 0.50f);
    static readonly Color C_RED      = new Color(0.95f, 0.30f, 0.20f);
    static readonly Color C_MUTED    = new Color(0.55f, 0.65f, 0.75f);

    void Awake() { BuildUI(); SetupButtonAudio(); }

    void BuildUI()
    {
        // EventSystem requerido para que los botones detecten clics
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // Cámara
        var camGO = new GameObject("Camera");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = C_BG;
        cam.orthographic    = true;
        if (FindObjectOfType<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();

        // Canvas raíz
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        var root = canvasGO.GetComponent<RectTransform>();

        // Fondo
        var bg = new GameObject("BG");
        bg.transform.SetParent(root, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = C_BG;

        // Título
        var tit = MkTMP(root, "Titulo", "SELECCIÓN DE DIFICULTAD", 78f, Color.white, FontStyles.Bold);
        SetRT(tit, 0f, 0.84f, 1f, 1f, 20f, 0f, -20f, -8f);
        tit.alignment = TextAlignmentOptions.Center;

        // Botón BÁSICO (izquierda)
        MkCard(root,
            axMin: 0.05f, axMax: 0.47f, ayMin: 0.14f, ayMax: 0.80f,
            bg:    C_BASICO,
            label: "BÁSICO",      lColor: C_GREEN,
            sub:   "Recomendado para principiantes",
            onClick: OnBasicoClicked);

        // Botón AVANZADO (derecha)
        MkCard(root,
            axMin: 0.53f, axMax: 0.95f, ayMin: 0.14f, ayMax: 0.80f,
            bg:    C_AVANZADO,
            label: "AVANZADO",    lColor: C_RED,
            sub:   "Requiere experiencia previa",
            onClick: OnAvanzadoClicked);

        // Botón Menú Principal (centro inferior)
        var btnMenu = MkSimpleBtn(root, C_BTN_MENU, "← Menú Principal", C_MUTED, 32f, OnMenuClicked);
        SetRT(btnMenu, 0.38f, 0.02f, 0.62f, 0.10f, 0f, 0f, 0f, 0f);
    }

    void MkCard(Transform parent,
        float axMin, float axMax, float ayMin, float ayMax,
        Color bg, string label, Color lColor, string sub,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Card_" + label);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        SetRT(go, axMin, ayMin, axMax, ayMax, 0f, 0f, 0f, 0f);
        var img = go.AddComponent<Image>();
        img.color = bg;

        // Texto grande (BÁSICO / AVANZADO)
        var lbl = MkTMP(go.transform, "Lbl", label, 110f, lColor, FontStyles.Bold);
        SetRT(lbl, 0f, 0.45f, 1f, 0.92f, 10f, 0f, -10f, 0f);
        lbl.alignment = TextAlignmentOptions.Center;

        // Subtexto
        var st = MkTMP(go.transform, "Sub", sub, 38f, Color.white, FontStyles.Normal);
        SetRT(st, 0f, 0.10f, 1f, 0.44f, 14f, 0f, -14f, 0f);
        st.alignment          = TextAlignmentOptions.Center;
        st.enableWordWrapping = true;

        // Button sobre la card
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(1.22f, 1.22f, 1.22f, 1f);
        c.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = c;
        btn.onClick.AddListener(onClick);
    }

    GameObject MkSimpleBtn(Transform parent, Color bg, string text, Color fg, float fs,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.normalColor      = Color.white;
        c.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        c.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = c;
        btn.onClick.AddListener(onClick);

        var lbl = MkTMP(go.transform, "Lbl", text, fs, fg, FontStyles.Bold);
        SetRT(lbl, 0f, 0f, 1f, 1f, 8f, 4f, -8f, -4f);
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.enableWordWrapping = false;

        return go;
    }

    void OnBasicoClicked()
    {
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();
        PatientCaseManager.Instance.GenerateCaseForType(CaseType.HemorragiaActiva);
        SceneManager.LoadScene("ReportePaciente");
    }

    void OnAvanzadoClicked()
    {
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();
        PatientCaseManager.Instance.GenerateCaseForType(CaseType.ViaAereaBlockeada);
        SceneManager.LoadScene("ReportePaciente");
    }

    void OnMenuClicked() => SceneManager.LoadScene("MenuPrincipal");

    void SetupButtonAudio()
    {
        if (UIAudioManager.Instance == null)
            new GameObject("_UIAudioManager").AddComponent<UIAudioManager>();

        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.GetComponent<MenuButtonAudio>() == null)
                btn.gameObject.AddComponent<MenuButtonAudio>();
        }
    }

    // Helpers
    static TextMeshProUGUI MkTMP(Transform parent, string name, string text,
        float size, Color color, FontStyles style)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text               = text;
        tmp.fontSize           = size;
        tmp.color              = color;
        tmp.fontStyle          = style;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    static void SetRT(Component c, float axMin, float ayMin, float axMax, float ayMax,
        float oxMin, float oyMin, float oxMax, float oyMax)
    {
        var rt = c.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(axMin, ayMin); rt.anchorMax = new Vector2(axMax, ayMax);
        rt.offsetMin = new Vector2(oxMin, oyMin); rt.offsetMax = new Vector2(oxMax, oyMax);
    }

    static void SetRT(GameObject go, float axMin, float ayMin, float axMax, float ayMax,
        float oxMin, float oyMin, float oxMax, float oyMax)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(axMin, ayMin); rt.anchorMax = new Vector2(axMax, ayMax);
        rt.offsetMin = new Vector2(oxMin, oyMin); rt.offsetMax = new Vector2(oxMax, oyMax);
    }
}
