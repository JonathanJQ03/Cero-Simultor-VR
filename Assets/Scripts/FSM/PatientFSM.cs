using System;
using System.Collections.Generic;
using UnityEngine;

public class PatientFSM : MonoBehaviour
{
    public static PatientFSM Instance { get; private set; }

    public PatientState CurrentState { get; private set; }
    public PatientCondition CurrentCondition { get; private set; }
    public float TimeInState { get; private set; }
    public float TotalPenaltyTime { get; private set; }
    public int ErrorCount { get; private set; }
    public bool IsFinished { get; private set; }
    public bool IsSuccess { get; private set; }

    public event Action<string> OnStateEnter;
    public event Action<string> OnStateExit;
    public event Action<string> OnCorrectTool;
    public event Action<string, string> OnWrongTool;
    public event Action<string> OnCriticalError;
    public event Action<string> OnTimeout;
    public event Action<bool> OnSimulationEnd;

    private Dictionary<string, PatientState> states;
    private string startStateId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitializeFSM();
    }

    public void InitializeFSM()
    {
        states = new Dictionary<string, PatientState>();
        BuildHemorragiaStates();
        BuildViaAereaStates();
        BuildParoCardiacoStates();
        BuildTerminalStates();

        // Select starting state based on the case assigned by PatientCaseManager
        if (PatientCaseManager.Instance != null &&
            PatientCaseManager.Instance.CurrentCase.caseType == CaseType.ViaAereaBlockeada)
            startStateId = "ESPERANDO_CANULA";
        else
            startStateId = "ESPERANDO_BISTURI";
    }

    void BuildHemorragiaStates()
    {
        var hemoA = new PatientState(
            id: "ESPERANDO_BISTURI",
            displayName: "Corte",
            condition: PatientCondition.HemorragiaActiva,
            description: "Use Bisturí o Tijeras de Trauma para exponer la herida",
            timeLimitSeconds: 0,
            allowedTools: new List<string> { "Bisturi", "TijerasDeTrauma" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Bisturi", "ESPERANDO_GASAS" },
                { "TijerasDeTrauma", "ESPERANDO_GASAS" }
            },
            timeoutTransition: null,
            criticalErrorTransition: "PARO_EPINEFRINA",
            criticalErrorTools: new List<string> { "Laringoscopio", "CanulaDeGuedel" }
        );
        states[hemoA.id] = hemoA;

        var hemoB = new PatientState(
            id: "ESPERANDO_GASAS",
            displayName: "Limpieza",
            condition: PatientCondition.HemorragiaActiva,
            description: "Use Vendas Hemostáticas o Gasas para limpiar la sangre",
            timeLimitSeconds: 15f,
            allowedTools: new List<string> { "VendasHemo", "Gasas" },
            toolTransitions: new Dictionary<string, string>
            {
                { "VendasHemo", "ESPERANDO_TORNIQUETE" },
                { "Gasas", "ESPERANDO_TORNIQUETE" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: "PARO_EPINEFRINA",
            criticalErrorTools: new List<string> { "Laringoscopio", "Desfibrilador" }
        );
        states[hemoB.id] = hemoB;

        var hemoC = new PatientState(
            id: "ESPERANDO_TORNIQUETE",
            displayName: "Compresión",
            condition: PatientCondition.HemorragiaActiva,
            description: "Use Torniquete proximal a la herida para controlar hemorragia",
            timeLimitSeconds: 20f,
            allowedTools: new List<string> { "Torniquete" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Torniquete", "ESTABILIZADO" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: null,
            criticalErrorTools: null
        );
        states[hemoC.id] = hemoC;

    }

    void BuildViaAereaStates()
    {
        var viaA = new PatientState(
            id: "ESPERANDO_CANULA",
            displayName: "Vía Aérea Básica",
            condition: PatientCondition.ViaAereaBlockeada,
            description: "Use Cánula de Guedel para abrir la vía aérea",
            timeLimitSeconds: 15f,
            allowedTools: new List<string> { "CanulaDeGuedel" },
            toolTransitions: new Dictionary<string, string>
            {
                { "CanulaDeGuedel", "ESPERANDO_LARINGOSCOPIO" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: "PARO_EPINEFRINA",
            criticalErrorTools: new List<string> { "Bisturi", "Desfibrilador" }
        );
        states[viaA.id] = viaA;

        var viaB = new PatientState(
            id: "ESPERANDO_LARINGOSCOPIO",
            displayName: "Intubación",
            condition: PatientCondition.ViaAereaBlockeada,
            description: "Use Laringoscopio para asegurar la vía aérea e intubar",
            timeLimitSeconds: 20f,
            allowedTools: new List<string> { "Laringoscopio" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Laringoscopio", "ESTABILIZADO" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: "PARO_EPINEFRINA",
            criticalErrorTools: new List<string> { "Desfibrilador", "Torniquete" }
        );
        states[viaB.id] = viaB;
    }

    void BuildParoCardiacoStates()
    {
        var paroD = new PatientState(
            id: "PARO_EPINEFRINA",
            displayName: "Epinefrina",
            condition: PatientCondition.ParoCardiaco,
            description: "Administre Epinefrina para reanimar al paciente",
            timeLimitSeconds: 10f,
            allowedTools: new List<string> { "Epinefrina" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Epinefrina", "PARO_DESFIBRILADOR" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: "FALLECIDO",
            criticalErrorTools: new List<string> { "Desfibrilador" }
        );
        states[paroD.id] = paroD;

        var paroE = new PatientState(
            id: "PARO_DESFIBRILADOR",
            displayName: "Desfibrilación",
            condition: PatientCondition.ParoCardiaco,
            description: "Use Desfibrilador (dos manos, carga previa)",
            timeLimitSeconds: 10f,
            allowedTools: new List<string> { "Desfibrilador" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Desfibrilador", "RECUPERADO_PARO" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: null,
            criticalErrorTools: null
        );
        states[paroE.id] = paroE;

        var recuperado = new PatientState(
            id: "RECUPERADO_PARO",
            displayName: "Recuperación Post-Paro",
            condition: PatientCondition.HemorragiaActiva,
            description: "Paciente reanimado. Aplique torniquete para controlar hemorragia.",
            timeLimitSeconds: 15f,
            allowedTools: new List<string> { "Torniquete" },
            toolTransitions: new Dictionary<string, string>
            {
                { "Torniquete", "ESTABILIZADO" }
            },
            timeoutTransition: "FALLECIDO",
            criticalErrorTransition: null,
            criticalErrorTools: null
        );
        states[recuperado.id] = recuperado;
    }

    void BuildTerminalStates()
    {
        var estabilizado = new PatientState(
            id: "ESTABILIZADO",
            displayName: "Estable",
            condition: PatientCondition.Estabilizado,
            description: "Paciente estabilizado con éxito.",
            timeLimitSeconds: 0,
            allowedTools: new List<string>(),
            toolTransitions: new Dictionary<string, string>(),
            timeoutTransition: null,
            criticalErrorTransition: null,
            criticalErrorTools: null
        );
        states[estabilizado.id] = estabilizado;

        var fallecido = new PatientState(
            id: "FALLECIDO",
            displayName: "Fallecido",
            condition: PatientCondition.Fallecido,
            description: "El paciente ha fallecido.",
            timeLimitSeconds: 0,
            allowedTools: new List<string>(),
            toolTransitions: new Dictionary<string, string>(),
            timeoutTransition: null,
            criticalErrorTransition: null,
            criticalErrorTools: null
        );
        states[fallecido.id] = fallecido;
    }

    public void StartSimulation()
    {
        if (states == null) InitializeFSM();
        IsFinished = false;
        IsSuccess = false;
        ErrorCount = 0;
        TotalPenaltyTime = 0;
        TransitionTo(startStateId);
    }

    void Update()
    {
        if (IsFinished || CurrentState == null) return;

        if (CurrentState.timeLimitSeconds > 0)
        {
            TimeInState += Time.deltaTime;
            if (TimeInState >= CurrentState.timeLimitSeconds)
            {
                HandleTimeout();
            }
        }
    }

    public bool ProcessTool(string toolId)
    {
        if (IsFinished || CurrentState == null) return false;

        if (CurrentState.allowedTools.Contains(toolId))
        {
            string nextStateId;
            if (CurrentState.toolTransitions.TryGetValue(toolId, out nextStateId))
            {
                OnCorrectTool?.Invoke(toolId);
                TransitionTo(nextStateId);
                return true;
            }
            return false;
        }

        if (CurrentState.criticalErrorTools != null && CurrentState.criticalErrorTools.Contains(toolId))
        {
            ErrorCount++;
            OnCriticalError?.Invoke(toolId);
            if (!string.IsNullOrEmpty(CurrentState.criticalErrorTransition))
            {
                TransitionTo(CurrentState.criticalErrorTransition);
            }
            return false;
        }

        ErrorCount++;
        TotalPenaltyTime += 5f;
        OnWrongTool?.Invoke(toolId, CurrentState.id);
        return false;
    }

    void HandleTimeout()
    {
        OnTimeout?.Invoke(CurrentState.id);
        if (!string.IsNullOrEmpty(CurrentState.timeoutTransition))
        {
            TransitionTo(CurrentState.timeoutTransition);
        }
    }

    void TransitionTo(string stateId)
    {
        if (string.IsNullOrEmpty(stateId) || !states.ContainsKey(stateId))
        {
            Debug.LogError($"FSM: Estado '{stateId}' no encontrado");
            return;
        }

        if (CurrentState != null)
        {
            OnStateExit?.Invoke(CurrentState.id);
        }

        CurrentState = states[stateId];
        CurrentCondition = CurrentState.condition;
        TimeInState = 0;

        OnStateEnter?.Invoke(CurrentState.id);

        if (CurrentState.condition == PatientCondition.Estabilizado)
        {
            IsFinished = true;
            IsSuccess = true;
            OnSimulationEnd?.Invoke(true);
        }
        else if (CurrentState.condition == PatientCondition.Fallecido)
        {
            IsFinished = true;
            IsSuccess = false;
            OnSimulationEnd?.Invoke(false);
        }
    }

    public PatientState GetState(string id)
    {
        if (states.ContainsKey(id)) return states[id];
        return null;
    }

    public float GetTotalElapsedTime()
    {
        float total = 0;
        foreach (var kvp in states)
        {
            if (kvp.Value.condition == CurrentCondition)
                break;
        }
        return TimeInState + TotalPenaltyTime;
    }
}
