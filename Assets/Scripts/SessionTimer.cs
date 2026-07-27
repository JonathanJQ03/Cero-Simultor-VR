using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Timer global de sesión: empieza en ReportePaciente, persiste hasta el Quirófano.
// Construye su propio HUD (abajo-centro) que se ve en todas las escenas intermedias.
public class SessionTimer : MonoBehaviour
{
    public static SessionTimer Instance { get; private set; }

    public const float TotalSeconds = 300f;
    public const float PenaltySecs  = 5f;

    public float Remaining { get; private set; } = TotalSeconds;
    public bool  IsRunning { get; private set; }
    public bool  Expired   => Remaining <= 0f;

    public event System.Action        OnExpired;
    public event System.Action<float> OnPenalty;

    GameObject      _hudCanvas;
    TextMeshProUGUI _hudText;

    static readonly Color C_TEAL   = new Color(0.18f, 0.78f, 0.88f);
    static readonly Color C_YELLOW = new Color(1.00f, 0.75f, 0.10f);
    static readonly Color C_RED    = new Color(0.95f, 0.25f, 0.25f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Remaining = TotalSeconds;
        BuildHUD();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (_hudCanvas != null) Destroy(_hudCanvas);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Piso Quirofano")
        {
            // ClockCountdown toma el control — apagar HUD
            StopTimer();
            if (_hudCanvas != null) { Destroy(_hudCanvas); _hudCanvas = null; _hudText = null; }
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        else if (scene.name == "MenuPrincipal")
        {
            // Volver al menú → destruir timer por completo para que la próxima
            // sesión empiece desde cero (OnDestroy limpia el HUD automáticamente)
            Destroy(gameObject);
        }
    }

    void BuildHUD()
    {
        _hudCanvas = new GameObject("_SessionTimerHUD");
        DontDestroyOnLoad(_hudCanvas);

        var canvas = _hudCanvas.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = _hudCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        _hudCanvas.AddComponent<GraphicRaycaster>();

        // Fondo del panel (abajo-centro)
        var panel = new GameObject("Panel");
        panel.transform.SetParent(_hudCanvas.transform, false);
        panel.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.13f, 0.88f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin        = new Vector2(0.5f, 0f);
        pRT.anchorMax        = new Vector2(0.5f, 0f);
        pRT.pivot            = new Vector2(0.5f, 0f);
        pRT.sizeDelta        = new Vector2(340f, 105f);
        pRT.anchoredPosition = new Vector2(0f, 18f);

        // Etiqueta superior
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(panel.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0f, 0.58f); lblRT.anchorMax = new Vector2(1f, 1f);
        lblRT.offsetMin = new Vector2(8f, 0f);    lblRT.offsetMax = new Vector2(-8f, -4f);
        var lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text               = "TIEMPO RESTANTE";
        lbl.fontSize           = 15f;
        lbl.fontStyle          = FontStyles.Bold;
        lbl.alignment          = TextAlignmentOptions.Center;
        lbl.color              = new Color(0.50f, 0.58f, 0.68f);
        lbl.enableWordWrapping = false;

        // Valor del timer (grande y visible)
        var valGO = new GameObject("Value");
        valGO.transform.SetParent(panel.transform, false);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = new Vector2(0f, 0f);  valRT.anchorMax = new Vector2(1f, 0.62f);
        valRT.offsetMin = new Vector2(8f, 4f);  valRT.offsetMax = new Vector2(-8f, 0f);
        _hudText = valGO.AddComponent<TextMeshProUGUI>();
        _hudText.text               = FormatTime();
        _hudText.fontSize           = 52f;
        _hudText.fontStyle          = FontStyles.Bold;
        _hudText.alignment          = TextAlignmentOptions.Center;
        _hudText.color              = C_TEAL;
        _hudText.enableWordWrapping = false;
    }

    public static SessionTimer EnsureExists()
    {
        if (Instance == null)
            new GameObject("_SessionTimer").AddComponent<SessionTimer>();
        return Instance;
    }

    public void StartTimer()
    {
        if (IsRunning) return;
        IsRunning = true;
    }

    public void StopTimer() => IsRunning = false;

    public void ApplyPenalty()
    {
        if (Expired) return;
        Remaining = Mathf.Max(0f, Remaining - PenaltySecs);
        OnPenalty?.Invoke(PenaltySecs);
        if (Remaining <= 0f)
        {
            IsRunning = false;
            OnExpired?.Invoke();
        }
    }

    void Update()
    {
        if (IsRunning && !Expired)
        {
            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                IsRunning = false;
                OnExpired?.Invoke();
            }
        }

        if (_hudText != null)
        {
            _hudText.text  = FormatTime();
            float rem = Remaining;
            _hudText.color = rem < 30f ? C_RED : rem < 60f ? C_YELLOW : C_TEAL;
        }
    }

    public string FormatTime()
    {
        int m = Mathf.FloorToInt(Remaining / 60f);
        int s = Mathf.FloorToInt(Remaining % 60f);
        return $"{m:00}:{s:00}";
    }
}
