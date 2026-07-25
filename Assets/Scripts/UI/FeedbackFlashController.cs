using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FeedbackFlashController : MonoBehaviour
{
    public static FeedbackFlashController Instance { get; private set; }

    [Header("Flash Overlay")]
    public Image flashOverlay;
    public float flashDuration = 0.5f;

    [Header("Colors")]
    public Color correctColor = new Color(0f, 1f, 0.25f, 0.3f);
    public Color incorrectColor = new Color(0.55f, 0f, 0f, 0.35f);
    public Color warningColor = new Color(1f, 0.6f, 0f, 0.3f);

    private Coroutine activeFlash;
    private PatientFSM fsm;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (flashOverlay != null) flashOverlay.color = Color.clear;
        fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        if (fsm != null) SubscribeFSM();
    }

    void SubscribeFSM()
    {
        fsm.OnCorrectTool += HandleCorrectTool;
        fsm.OnWrongTool += HandleWrongTool;
        fsm.OnCriticalError += HandleCriticalError;
    }

    void OnDestroy()
    {
        if (fsm == null) return;
        fsm.OnCorrectTool -= HandleCorrectTool;
        fsm.OnWrongTool -= HandleWrongTool;
        fsm.OnCriticalError -= HandleCriticalError;
    }

    void HandleCorrectTool(string _) => Flash(correctColor);
    void HandleWrongTool(string _a, string _b) => Flash(incorrectColor);
    void HandleCriticalError(string _) => Flash(warningColor);

    public void Flash(Color color)
    {
        if (activeFlash != null) StopCoroutine(activeFlash);
        activeFlash = StartCoroutine(DoFlash(color));
    }

    IEnumerator DoFlash(Color color)
    {
        if (flashOverlay == null) yield break;
        flashOverlay.color = color;
        float elapsed = 0;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            flashOverlay.color = Color.Lerp(color, Color.clear, elapsed / flashDuration);
            yield return null;
        }
        flashOverlay.color = Color.clear;
    }
}
