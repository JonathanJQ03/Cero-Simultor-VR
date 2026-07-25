using System.Collections.Generic;

public enum PatientCondition
{
    HemorragiaActiva,
    ViaAereaBlockeada,
    ParoCardiaco,
    Estabilizado,
    Fallecido
}

[System.Serializable]
public class PatientState
{
    public string id;
    public string displayName;
    public PatientCondition condition;
    public string description;
    public float timeLimitSeconds;
    public List<string> allowedTools;
    public Dictionary<string, string> toolTransitions;
    public string timeoutTransition;
    public string criticalErrorTransition;
    public List<string> criticalErrorTools;

    public PatientState(string id, string displayName, PatientCondition condition, string description,
        float timeLimitSeconds, List<string> allowedTools, Dictionary<string, string> toolTransitions,
        string timeoutTransition, string criticalErrorTransition = null, List<string> criticalErrorTools = null)
    {
        this.id = id;
        this.displayName = displayName;
        this.condition = condition;
        this.description = description;
        this.timeLimitSeconds = timeLimitSeconds;
        this.allowedTools = allowedTools ?? new List<string>();
        this.toolTransitions = toolTransitions ?? new Dictionary<string, string>();
        this.timeoutTransition = timeoutTransition;
        this.criticalErrorTransition = criticalErrorTransition;
        this.criticalErrorTools = criticalErrorTools ?? new List<string>();
    }
}
