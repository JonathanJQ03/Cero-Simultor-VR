using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public PatientFSM fsm;
    public PatientController patient;
    public GameTimer gameTimer;
    public MessageController messageController;
    public InjuryMarkerController injuryMarkerController;
    public ClockCountdown clockCountdown;

    [Header("UI References")]
    public GameObject diagnosticScreen;
    public GameObject toolSelectionPanel;
    public GameObject resultsPanel;
    public GameObject resultsSuccessText;
    public GameObject resultsFailText;

    [Header("Audio")]
    public AudioSource alarmAudioSource;
    public AudioClip alarmAmberClip;
    public AudioClip alarmRedClip;
    public AudioClip successClip;

    [Header("Settings")]
    public int requiredToolCount = 5;

    private int selectedTools;
    private bool simulationStarted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (fsm == null) fsm = FindObjectOfType<PatientFSM>();
        if (patient == null) patient = FindObjectOfType<PatientController>();
        if (gameTimer == null) gameTimer = FindObjectOfType<GameTimer>();
        if (messageController == null) messageController = FindObjectOfType<MessageController>();
        if (injuryMarkerController == null) injuryMarkerController = FindObjectOfType<InjuryMarkerController>();
        if (clockCountdown == null) clockCountdown = FindObjectOfType<ClockCountdown>();
    }

    void Start()
    {
        if (fsm == null) return;

        fsm.OnStateEnter += OnFSMStateEnter;
        fsm.OnCorrectTool += OnCorrectTool;
        fsm.OnWrongTool += OnWrongTool;
        fsm.OnCriticalError += OnCriticalError;
        fsm.OnSimulationEnd += OnSimulationEnd;

        if (diagnosticScreen != null) diagnosticScreen.SetActive(false);
        if (toolSelectionPanel != null) toolSelectionPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        // Auto-start when arriving from the full selection flow
        if (PatientCaseManager.Instance != null && PatientCaseManager.Instance.ReadyToSimulate())
            StartSimulation();
    }

    public void StartSimulation()
    {
        Debug.Log("Iniciando simulacion...");

        if (diagnosticScreen != null)
            diagnosticScreen.SetActive(false);
        if (toolSelectionPanel != null)
            toolSelectionPanel.SetActive(false);

        if (patient != null)
            patient.InitializePatient();

        fsm.StartSimulation();

        // Activar marker DESPUÉS de que el FSM fijó el estado inicial
        if (injuryMarkerController != null)
            injuryMarkerController.Initialize();

        if (gameTimer != null)
            gameTimer.StartTimer();

        if (clockCountdown != null)
        {
            clockCountdown.OnTimeUp += OnClockTimeUp;
            clockCountdown.StartCountdown();
        }

        simulationStarted = true;

        if (messageController != null)
            messageController.ShowInfo(fsm.CurrentState?.description ?? "Intervención iniciada.");
    }

    void OnClockTimeUp()
    {
        if (clockCountdown != null)
            clockCountdown.OnTimeUp -= OnClockTimeUp;
        if (fsm != null)
            fsm.HandleGlobalTimeout();
    }

public float GetElapsedTime()
    {
        if (gameTimer != null) return gameTimer.GetElapsedTime();
        return 0f;
    }


    public void StartDiagnosticPhase()
    {
        if (diagnosticScreen != null)
            diagnosticScreen.SetActive(true);
    }

    public void StartToolSelection()
    {
        if (diagnosticScreen != null)
            diagnosticScreen.SetActive(false);
        if (toolSelectionPanel != null)
            toolSelectionPanel.SetActive(true);
    }

    public void OnToolSelected()
    {
        selectedTools++;
        if (selectedTools >= requiredToolCount)
        {
            StartSimulation();
        }
    }

    void OnFSMStateEnter(string stateId)
    {
        var state = fsm.GetState(stateId);
        if (state == null) return;

        if (messageController != null)
            messageController.ShowInfo(state.description);

        // Cardiac arrest: switch injury markers to muslo + torax
        if (stateId == "PARO_EPINEFRINA" && injuryMarkerController != null)
            injuryMarkerController.ShowCardiacMarkers();

        if (alarmAudioSource != null)
        {
            if (state.condition == PatientCondition.ParoCardiaco)
            {
                alarmAudioSource.clip = alarmRedClip;
                alarmAudioSource.Play();
            }
            else if (state.condition == PatientCondition.HemorragiaActiva && fsm.ErrorCount > 1)
            {
                alarmAudioSource.clip = alarmAmberClip;
                alarmAudioSource.Play();
            }
        }
    }

    void OnCorrectTool(string toolId)
    {
        if (messageController != null)
        {
            string msg = GetSuccessMessage(toolId);
            messageController.ShowSuccess(msg);
        }
    }

    void OnWrongTool(string toolId, string stateId)
    {
        if (messageController != null)
        {
            string msg = $"Error: {toolId} no es la herramienta correcta.";
            messageController.ShowError(msg);
        }

        if (alarmAudioSource != null && alarmAmberClip != null)
        {
            alarmAudioSource.PlayOneShot(alarmAmberClip);
        }
    }

    void OnCriticalError(string toolId)
    {
        if (messageController != null)
        {
            messageController.ShowError($"Error critico: {toolId}. Paciente en paro!");
        }

        if (alarmAudioSource != null && alarmRedClip != null)
        {
            alarmAudioSource.clip = alarmRedClip;
            alarmAudioSource.Play();
        }
    }

    void OnSimulationEnd(bool success)
    {
        if (gameTimer != null) gameTimer.StopTimer();
        if (clockCountdown != null)
        {
            clockCountdown.StopCountdown();
            clockCountdown.OnTimeUp -= OnClockTimeUp;
        }

        if (messageController != null)
        {
            if (success) messageController.ShowSuccess("Paciente estabilizado!");
            else messageController.ShowError("El paciente ha fallecido.");
        }

        if (success && alarmAudioSource != null && successClip != null)
        {
            alarmAudioSource.clip = successClip;
            alarmAudioSource.Play();
        }

        if (GameFlowController.Instance != null)
            GameFlowController.Instance.OnSimulationEnd(success);
    }

    string GetSuccessMessage(string toolId)
    {
        switch (toolId)
        {
            case "Bisturi":
            case "TijerasDeTrauma":
                return "Herida expuesta. Limpie el exceso de sangre.";
            case "VendasHemo":
            case "Gasas":
                return "Campo limpio. Aplique presion con torniquete.";
            case "Torniquete":
                return "Hemorragia controlada. Paciente estable.";
            case "Epinefrina":
                return "Adrenalina administrada. Use desfibrilador.";
            case "Desfibrilador":
                return "Desfibrilacion exitosa. Paciente reanimado.";
            default:
                return "Herramienta usada correctamente.";
        }
    }

    public void RestartSimulation()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }

    void OnDestroy()
    {
        if (fsm != null)
        {
            fsm.OnStateEnter -= OnFSMStateEnter;
            fsm.OnCorrectTool -= OnCorrectTool;
            fsm.OnWrongTool -= OnWrongTool;
            fsm.OnCriticalError -= OnCriticalError;
            fsm.OnSimulationEnd -= OnSimulationEnd;
        }
    }
}
