using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DemoController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI stateText;
    public TextMeshProUGUI vitalsText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI errorText;
    public Image patientColorIndicator;
    public Button[] toolButtons;

    [Header("Patient")]
    public PatientController patient;
    public PatientFSM fsm;

    [Header("Colors")]
    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    public Color deadColor = Color.gray;

    private string[] toolIds = {
        "Bisturi", "TijerasDeTrauma", "VendasHemo", "Gasas",
        "Torniquete", "Epinefrina", "Desfibrilador",
        "Laringoscopio", "CanulaDeGuedel"
    };

    void Start()
    {
        if (fsm == null) fsm = FindObjectOfType<PatientFSM>();
        if (patient == null) patient = FindObjectOfType<PatientController>();

        fsm.OnStateEnter += UpdateUI;
        fsm.OnCorrectTool += OnCorrect;
        fsm.OnWrongTool += OnWrong;
        fsm.OnCriticalError += OnCritical;
        fsm.OnSimulationEnd += OnEnd;

        SetupButtons();

        StartCoroutine(DelayedStart());
    }

    System.Collections.IEnumerator DelayedStart()
    {
        yield return null;
        fsm.StartSimulation();
        if (patient != null) patient.InitializePatient();
        UpdateUI(fsm.CurrentState.id);
    }

    void SetupButtons()
    {
        for (int i = 0; i < toolButtons.Length && i < toolIds.Length; i++)
        {
            int index = i;
            toolButtons[i].onClick.AddListener(() => UseTool(toolIds[index]));
            var text = toolButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = toolIds[index];
        }
    }

    public void UseTool(string toolId)
    {
        if (fsm != null && !fsm.IsFinished)
            fsm.ProcessTool(toolId);
    }

    void UpdateUI(string stateId)
    {
        var state = fsm.GetState(stateId);
        if (state == null) return;

        if (stateText != null)
        {
            stateText.text = $"Estado: {state.displayName}\nCondicion: {FormatCondition(state.condition)}";
            stateText.color = GetStateColor(state.condition);
        }

        if (patient != null)
        {
            if (vitalsText != null)
                vitalsText.text = $"FC: {patient.heartRate:F0} BPM\nSpO2: {patient.spo2:F0}%\nPA: {patient.bloodPressureSystolic:F0}/{patient.bloodPressureDiastolic:F0}";
        }

        if (patientColorIndicator != null)
            patientColorIndicator.color = GetStateColor(state.condition);

        if (errorText != null)
            errorText.text = $"Errores: {fsm.ErrorCount} | Penalizacion: {fsm.TotalPenaltyTime:F1}s";
    }

    void OnCorrect(string toolId)
    {
        if (messageText != null)
        {
            messageText.text = $"✓ {GetSuccessMessage(toolId)}";
            messageText.color = normalColor;
        }
        UpdateUI(fsm.CurrentState.id);
    }

    void OnWrong(string toolId, string currentState)
    {
        if (messageText != null)
        {
            messageText.text = $"✗ {toolId} no es correcta aqui (-5s)";
            messageText.color = warningColor;
        }
        UpdateUI(fsm.CurrentState.id);
    }

    void OnCritical(string toolId)
    {
        if (messageText != null)
        {
            messageText.text = $"⚠ ERROR CRITICO: {toolId}. Paciente en PARO!";
            messageText.color = criticalColor;
        }
        UpdateUI(fsm.CurrentState.id);
    }

    void OnEnd(bool success)
    {
        if (messageText != null)
        {
            messageText.text = success ? "PACIENTE ESTABILIZADO! ✓" : "PACIENTE FALLECIDO ✗";
            messageText.color = success ? normalColor : criticalColor;
        }
        if (stateText != null)
        {
            stateText.text = success ? "EXITO" : "FRACASO";
            stateText.color = success ? normalColor : criticalColor;
        }
    }

    void Update()
    {
        if (fsm != null && !fsm.IsFinished && timerText != null)
        {
            var state = fsm.CurrentState;
            if (state != null && state.timeLimitSeconds > 0)
            {
                float remaining = Mathf.Max(0, state.timeLimitSeconds - fsm.TimeInState);
                timerText.text = $"Tiempo restante: {remaining:F1}s";
                timerText.color = remaining < 5f ? criticalColor : remaining < 10f ? warningColor : Color.white;
            }
            else
            {
                timerText.text = "Sin limite de tiempo";
            }
        }
    }

    string GetSuccessMessage(string toolId)
    {
        switch (toolId)
        {
            case "Bisturi":
            case "TijerasDeTrauma": return "Herida expuesta";
            case "VendasHemo":
            case "Gasas": return "Campo limpio";
            case "Torniquete": return "Hemorragia controlada";
            case "Epinefrina": return "Adrenalina administrada";
            case "Desfibrilador": return "Desfibrilacion exitosa";
            default: return "Correcto";
        }
    }

    string FormatCondition(PatientCondition c)
    {
        switch (c)
        {
            case PatientCondition.HemorragiaActiva: return "HEMORRAGIA ACTIVA";
            case PatientCondition.ParoCardiaco: return "PARO CARDIACO";
            case PatientCondition.Estabilizado: return "ESTABLE";
            case PatientCondition.Fallecido: return "FALLECIDO";
            default: return c.ToString();
        }
    }

    Color GetStateColor(PatientCondition c)
    {
        switch (c)
        {
            case PatientCondition.HemorragiaActiva: return warningColor;
            case PatientCondition.ParoCardiaco: return criticalColor;
            case PatientCondition.Estabilizado: return normalColor;
            case PatientCondition.Fallecido: return Color.gray;
            default: return Color.white;
        }
    }

    public void RestartDemo()
    {
        fsm.StartSimulation();
        if (patient != null) patient.InitializePatient();
        UpdateUI(fsm.CurrentState.id);
        if (messageText != null) messageText.text = "";
    }
}
