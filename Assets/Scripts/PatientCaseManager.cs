using UnityEngine;

public enum CaseType { HemorragiaActiva, ViaAereaBlockeada }

public enum InjuryLocation { Boca, BrazoIzq, BrazoDer, PiernaIzq, PiernaDer, Torax, Muslo }

[System.Serializable]
public class PatientData
{
    public CaseType caseType;
    public InjuryLocation injuryLocation;

    // Vitals
    public int heartRate;
    public string heartRateLabel;
    public UnityEngine.Color heartRateColor;

    public int spo2;
    public string spo2Label;
    public UnityEngine.Color spo2Color;

    public int systolicBP;
    public int diastolicBP;
    public string bpLabel;
    public UnityEngine.Color bpColor;

    public int shockLevel;
    public string shockLabel;
    public UnityEngine.Color shockColor;

    // Patient demographics
    public int age;
    public string sex;
    public int weight;
    public int patientNumber;

    // Case narrative
    public string cause;
    public string finding;
}

public class PatientCaseManager : MonoBehaviour
{
    public static PatientCaseManager Instance { get; private set; }

    public PatientData CurrentCase { get; private set; }
    public System.Collections.Generic.List<string> SelectedTools { get; private set; } = new System.Collections.Generic.List<string>();
    public const int RequiredToolCount = 5;

    // Herramientas correctas por tipo de caso
    public static readonly string[] HemorragiaTools =
        { "Bisturi", "VendasHemo", "Torniquete", "Desfibrilador", "Epinefrina" };
    public static readonly string[] ViaAereaTools =
        { "CanulaDeGuedel", "Laringoscopio", "Epinefrina", "Desfibrilador", "Bisturi" };

    public string[] CorrectTools =>
        CurrentCase?.caseType == CaseType.ViaAereaBlockeada ? ViaAereaTools : HemorragiaTools;

    public bool IsCorrectTool(string toolId)
    {
        foreach (var t in CorrectTools)
            if (t == toolId) return true;
        return false;
    }

    public bool ToggleTool(string toolId)
    {
        if (SelectedTools.Contains(toolId)) { SelectedTools.Remove(toolId); return false; }
        if (SelectedTools.Count < RequiredToolCount)  { SelectedTools.Add(toolId); return true; }
        return false;
    }
    public bool IsToolSelected(string toolId) => SelectedTools.Contains(toolId);
    public bool ReadyToSimulate() => SelectedTools.Count == RequiredToolCount;

    static readonly Color colorNormal   = new Color(0.20f, 0.90f, 0.60f, 1f);
    static readonly Color colorWarning  = new Color(1.00f, 0.75f, 0.10f, 1f);
    static readonly Color colorCritical = new Color(0.95f, 0.25f, 0.25f, 1f);

    static readonly string[] causeHemorragia = {
        "Accidente de tránsito — trauma múltiples",
        "Herida por arma de fuego — extremidad inferior",
        "Caída de altura — fractura expuesta"
    };
    static readonly string[] findingHemorragia = {
        "Hemorragia activa en extremidad inferior derecha",
        "Sangrado masivo en región femoral izquierda",
        "Hemorragia activa en miembro inferior — fractura abierta"
    };

    static readonly string[] causeViaAerea = {
        "Obstrucción de vía aérea — cuerpo extraño",
        "Anafilaxia severa — edema laríngeo",
        "Trauma cervical — obstrucción por edema"
    };
    static readonly string[] findingViaAerea = {
        "Obstrucción parcial de vía aérea superior — cianosis evidente",
        "Estridor laríngeo severo — incapacidad ventilatoria",
        "Vía aérea comprometida — SpO2 en descenso crítico"
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GenerateNewCase();
    }

    public void GenerateNewCase()
    {
        SelectedTools.Clear();
        var data = new PatientData();

        // Random case type
        data.caseType = (CaseType)Random.Range(0, 2);

        // Random demographics
        data.age    = Random.Range(20, 62);
        data.sex    = Random.value > 0.4f ? "M" : "F";
        data.weight = Random.Range(55, 96);
        data.patientNumber = Random.Range(1, 100);

        if (data.caseType == CaseType.HemorragiaActiva)
            FillHemorragiaVitals(data);
        else
            FillViaAereaVitals(data);

        CurrentCase = data;
    }

    void FillHemorragiaVitals(PatientData d)
    {
        // HR: 115-145 taquicardia por pérdida de sangre
        d.heartRate = Random.Range(115, 146);
        if (d.heartRate >= 140)
        { d.heartRateLabel = "TAQUICARDIA SEVERA"; d.heartRateColor = colorCritical; }
        else
        { d.heartRateLabel = "TAQUICARDIA";        d.heartRateColor = colorWarning;  }

        // SpO2: 87-94 hipoxia moderada
        d.spo2 = Random.Range(87, 95);
        if (d.spo2 < 90)
        { d.spo2Label = "HIPOXIA CRÍTICA";   d.spo2Color = colorCritical; }
        else
        { d.spo2Label = "HIPOXIA MODERADA";  d.spo2Color = colorWarning;  }

        // BP: hypotension
        d.systolicBP  = Random.Range(75, 96);
        d.diastolicBP = Random.Range(45, 66);
        d.bpLabel = "HIPOTENSIÓN";
        d.bpColor = colorWarning;

        // Shock II-III
        d.shockLevel = Random.Range(2, 4);
        d.shockLabel = d.shockLevel == 3 ? "SEVERO" : "MODERADO";
        d.shockColor = d.shockLevel == 3 ? colorCritical : colorWarning;

        // Random injury location: arm or leg, left or right
        InjuryLocation[] hemLocs = { InjuryLocation.BrazoIzq, InjuryLocation.BrazoDer,
                                     InjuryLocation.PiernaIzq, InjuryLocation.PiernaDer };
        d.injuryLocation = hemLocs[Random.Range(0, hemLocs.Length)];

        int idx = Random.Range(0, causeHemorragia.Length);
        d.cause   = causeHemorragia[idx];
        d.finding = findingHemorragia[idx];
    }

    void FillViaAereaVitals(PatientData d)
    {
        // HR: 95-125
        d.heartRate = Random.Range(95, 126);
        d.heartRateLabel = d.heartRate > 110 ? "TAQUICARDIA" : "NORMAL ALTO";
        d.heartRateColor = d.heartRate > 110 ? colorWarning : colorNormal;

        // SpO2: 72-84 hipoxia crítica
        d.spo2 = Random.Range(72, 85);
        d.spo2Label = "HIPOXIA CRÍTICA";
        d.spo2Color = colorCritical;

        // BP: normal or slightly elevated from hypoxia response
        d.systolicBP  = Random.Range(100, 131);
        d.diastolicBP = Random.Range(65, 86);
        bool bpHigh = d.systolicBP > 120;
        d.bpLabel = bpHigh ? "HIPERTENSIÓN REACTIVA" : "PRESIÓN NORMAL";
        d.bpColor = bpHigh ? colorWarning : colorNormal;

        // Shock I-II
        d.shockLevel = Random.Range(1, 3);
        d.shockLabel = d.shockLevel == 2 ? "MODERADO" : "LEVE";
        d.shockColor = d.shockLevel == 2 ? colorWarning : colorNormal;

        d.injuryLocation = InjuryLocation.Boca;

        int idx = Random.Range(0, causeViaAerea.Length);
        d.cause   = causeViaAerea[idx];
        d.finding = findingViaAerea[idx];
    }
}
