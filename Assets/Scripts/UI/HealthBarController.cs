using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarController : MonoBehaviour
{
    [Header("UI")]
    public Image healthFill;
    public TextMeshProUGUI healthPercentText;
    public TextMeshProUGUI shockIndexText;
    public TextMeshProUGUI hypoxiaAlertText;

    [Header("Colors")]
    public Color healthyColor = new Color(0f, 1f, 0.25f);
    public Color warningColor = new Color(1f, 0.6f, 0f);
    public Color criticalColor = new Color(0.55f, 0f, 0f);

    private PatientFSM fsm;
    private PatientController patient;
    private float currentHealth = 100f;

    void Start()
    {
        fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        patient = FindObjectOfType<PatientController>();

        if (fsm != null)
        {
            fsm.OnCorrectTool += HandleCorrectTool;
            fsm.OnCriticalError += HandleCriticalError;
            fsm.OnSimulationEnd += HandleSimulationEnd;
        }

        if (hypoxiaAlertText != null) hypoxiaAlertText.gameObject.SetActive(false);
        UpdateDisplay();
    }

    void OnDestroy()
    {
        if (fsm == null) return;
        fsm.OnCorrectTool -= HandleCorrectTool;
        fsm.OnCriticalError -= HandleCriticalError;
        fsm.OnSimulationEnd -= HandleSimulationEnd;
    }

    void HandleCorrectTool(string _) { currentHealth = Mathf.Min(currentHealth + 8f, 100f); }
    void HandleCriticalError(string _) { currentHealth = Mathf.Max(currentHealth - 25f, 0f); }
    void HandleSimulationEnd(bool success) { currentHealth = success ? 82f : 0f; UpdateDisplay(); }

    void Update()
    {
        if (fsm == null || patient == null || fsm.IsFinished) return;

        switch (fsm.CurrentCondition)
        {
            case PatientCondition.HemorragiaActiva:
                currentHealth = Mathf.Max(0f, currentHealth - Time.deltaTime * 2.5f);
                break;
            case PatientCondition.ParoCardiaco:
                currentHealth = Mathf.Max(0f, currentHealth - Time.deltaTime * 7f);
                break;
        }

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        float pct = currentHealth / 100f;
        Color c = pct > 0.6f ? healthyColor : pct > 0.3f ? warningColor : criticalColor;

        if (healthFill != null) { healthFill.fillAmount = pct; healthFill.color = c; }
        if (healthPercentText != null) { healthPercentText.text = $"PHYS: {Mathf.RoundToInt(currentHealth)}%"; healthPercentText.color = c; }

        if (shockIndexText != null && patient != null)
        {
            float si = patient.bloodPressureSystolic > 0 ? patient.heartRate / patient.bloodPressureSystolic : 99f;
            shockIndexText.text = $"SI: {si:F1}";
            shockIndexText.color = si > 1f ? criticalColor : si > 0.7f ? warningColor : healthyColor;
        }

        if (hypoxiaAlertText != null && patient != null)
            hypoxiaAlertText.gameObject.SetActive(patient.spo2 < 90f);
    }

    public float GetHealthPercent() => currentHealth;
}
