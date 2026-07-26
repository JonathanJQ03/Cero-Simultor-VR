using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FeedbackFlashController : MonoBehaviour
{
    public static FeedbackFlashController Instance { get; private set; }

    [Header("Flash Overlay")]
    public Image flashOverlay;
    public float flashDuration = 0.5f;

    [Header("Feedback Icon")]
    public TextMeshProUGUI feedbackIcon;
    public float iconHoldDuration = 0.8f;
    public float iconFadeDuration = 0.4f;

    [Header("Warning Border")]
    public CanvasGroup warningBorderGroup;
    public float warningBlinkInterval = 0.25f;
    public int warningBlinkCount = 5;

    [Header("Colors")]
    public Color correctColor   = new Color(0f,   1f,    0.25f, 0.35f);
    public Color incorrectColor = new Color(0.7f,  0f,    0f,   0.40f);
    public Color warningColor   = new Color(1f,    0.85f, 0f,   0.70f);

    private Coroutine _flashRoutine;
    private Coroutine _iconRoutine;
    private Coroutine _borderRoutine;
    private PatientFSM _fsm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ResetAll();
        _fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        if (_fsm != null) SubscribeFSM();
    }

    void SubscribeFSM()
    {
        _fsm.OnCorrectTool   += HandleCorrectTool;
        _fsm.OnWrongTool     += HandleWrongTool;
        _fsm.OnCriticalError += HandleCriticalError;
    }

    void OnDestroy()
    {
        if (_fsm == null) return;
        _fsm.OnCorrectTool   -= HandleCorrectTool;
        _fsm.OnWrongTool     -= HandleWrongTool;
        _fsm.OnCriticalError -= HandleCriticalError;
    }

    void HandleCorrectTool(string _)           => ShowCorrect();
    void HandleWrongTool(string _a, string _b)  => ShowIncorrect();
    void HandleCriticalError(string _)          => ShowWarning();

    // ── Public API ─────────────────────────────────────────────────────────

    public void ShowCorrect()
    {
        StartFlash(correctColor);
        StartIcon("✓", new Color(0.1f, 1f, 0.3f, 1f));
    }

    public void ShowIncorrect()
    {
        StartFlash(incorrectColor);
        StartIcon("✗", new Color(1f, 0.15f, 0.1f, 1f));
    }

    public void ShowWarning()
    {
        StartFlash(incorrectColor);
        StartBorder();
    }

    // Legacy entry point — keeps any existing callers working
    public void Flash(Color color) => StartFlash(color);

    // ── Private helpers ────────────────────────────────────────────────────

    void ResetAll()
    {
        if (flashOverlay       != null) flashOverlay.color       = Color.clear;
        if (feedbackIcon       != null) feedbackIcon.color       = Color.clear;
        if (warningBorderGroup != null) warningBorderGroup.alpha  = 0f;
    }

    void StartFlash(Color color)
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(DoFlash(color));
    }

    void StartIcon(string symbol, Color color)
    {
        if (_iconRoutine != null) StopCoroutine(_iconRoutine);
        _iconRoutine = StartCoroutine(DoIcon(symbol, color));
    }

    void StartBorder()
    {
        if (_borderRoutine != null) StopCoroutine(_borderRoutine);
        _borderRoutine = StartCoroutine(DoBorder());
    }

    IEnumerator DoFlash(Color color)
    {
        if (flashOverlay == null) yield break;
        flashOverlay.color = color;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            flashOverlay.color = Color.Lerp(color, Color.clear, t / flashDuration);
            yield return null;
        }
        flashOverlay.color = Color.clear;
    }

    IEnumerator DoIcon(string symbol, Color color)
    {
        if (feedbackIcon == null) yield break;
        feedbackIcon.text  = symbol;
        feedbackIcon.color = color;
        yield return new WaitForSeconds(iconHoldDuration);
        float t = 0f;
        while (t < iconFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / iconFadeDuration);
            feedbackIcon.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        feedbackIcon.color = Color.clear;
    }

    IEnumerator DoBorder()
    {
        if (warningBorderGroup == null) yield break;
        for (int i = 0; i < warningBlinkCount; i++)
        {
            warningBorderGroup.alpha = 1f;
            yield return new WaitForSeconds(warningBlinkInterval);
            warningBorderGroup.alpha = 0f;
            yield return new WaitForSeconds(warningBlinkInterval);
        }
        warningBorderGroup.alpha = 0f;
    }
}
