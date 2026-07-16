using UnityEngine;
using TMPro;

public class MonitorDisplay : MonoBehaviour
{
    public TextMeshPro heartRateText;
    public TextMeshPro spo2Text;
    public TextMeshPro bloodPressureText;
    public TextMeshPro statusText;

    public LineRenderer ecgLine;
    public int ecgPointCount = 128;

    public Color normalColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;

    private PatientFSM fsm;
    private PatientController patient;
    private float ecgTime;

    void Start()
    {
        fsm = PatientFSM.Instance;
        patient = FindObjectOfType<PatientController>();

        if (ecgLine != null)
        {
            ecgLine.positionCount = ecgPointCount;
            ecgLine.startColor = normalColor;
            ecgLine.endColor = normalColor;
        }
    }

    void Update()
    {
        if (fsm == null || patient == null) return;

        UpdateVitalsText();
        UpdateECG();
        UpdateStatus();
    }

    void UpdateVitalsText()
    {
        if (fsm.IsFinished)
        {
            if (heartRateText != null)
                heartRateText.text = fsm.IsSuccess ? "FC: 75 BPM" : "FC: ---";
            if (spo2Text != null)
                spo2Text.text = fsm.IsSuccess ? "SpO2: 97%" : "SpO2: 0%";
            if (bloodPressureText != null)
                bloodPressureText.text = fsm.IsSuccess ? "PA: 120/80" : "PA: 0/0";
            return;
        }

        if (fsm.CurrentCondition == PatientCondition.ParoCardiaco)
        {
            if (heartRateText != null) heartRateText.text = "FC: 0 BPM";
            if (spo2Text != null) spo2Text.text = "SpO2: 40%";
            if (bloodPressureText != null) bloodPressureText.text = "PA: 0/0";
            SetVitalsColor(criticalColor);
            return;
        }

        int hr = Mathf.RoundToInt(patient.heartRate);
        int sp = Mathf.RoundToInt(patient.spo2);
        int sys = Mathf.RoundToInt(patient.bloodPressureSystolic);
        int dia = Mathf.RoundToInt(patient.bloodPressureDiastolic);

        if (heartRateText != null) heartRateText.text = $"FC: {hr} BPM";
        if (spo2Text != null) spo2Text.text = $"SpO2: {sp}%";
        if (bloodPressureText != null) bloodPressureText.text = $"PA: {sys}/{dia}";

        if (hr > 120 || sp < 85)
            SetVitalsColor(warningColor);
        else if (hr > 140 || sp < 70)
            SetVitalsColor(criticalColor);
        else
            SetVitalsColor(normalColor);
    }

    void SetVitalsColor(Color color)
    {
        if (heartRateText != null) heartRateText.color = color;
        if (spo2Text != null) spo2Text.color = color;
        if (bloodPressureText != null) bloodPressureText.color = color;
    }

    void UpdateECG()
    {
        if (ecgLine == null) return;

        ecgTime += Time.deltaTime * (fsm.CurrentCondition == PatientCondition.ParoCardiaco ? 0.5f : 3f);

        float hr = fsm.CurrentCondition == PatientCondition.ParoCardiaco ? 0 : patient.heartRate;
        float amplitude = fsm.CurrentCondition == PatientCondition.ParoCardiaco ? 0.02f : 0.15f;
        float frequency = Mathf.Lerp(0.5f, 2f, hr / 160f);

        Color lineColor = normalColor;
        if (fsm.CurrentCondition == PatientCondition.ParoCardiaco)
            lineColor = criticalColor;
        else if (hr > 120)
            lineColor = warningColor;

        ecgLine.startColor = lineColor;
        ecgLine.endColor = lineColor;

        Vector3[] points = new Vector3[ecgPointCount];
        for (int i = 0; i < ecgPointCount; i++)
        {
            float t = (float)i / ecgPointCount;
            float x = t * 10f - 5f;

            float y = Mathf.Sin(ecgTime + t * Mathf.PI * frequency) * amplitude;

            if (i > ecgPointCount / 3 && i < ecgPointCount / 3 + 3)
            {
                float spike = Mathf.Sin((t - 1f / 3f) * Mathf.PI * 30f) * amplitude * 3f;
                y += Mathf.Max(0, spike);
            }

            points[i] = new Vector3(x, y, 0);
        }

        ecgLine.SetPositions(points);
    }

    void UpdateStatus()
    {
        if (statusText == null) return;

        if (fsm.IsFinished)
        {
            statusText.text = fsm.IsSuccess ? "ESTABLE" : "FALLECIDO";
            statusText.color = fsm.IsSuccess ? normalColor : criticalColor;
            return;
        }

        var state = fsm.CurrentState;
        if (state == null) return;

        switch (state.condition)
        {
            case PatientCondition.HemorragiaActiva:
                statusText.text = "HEMORRAGIA ACTIVA";
                statusText.color = warningColor;
                break;
            case PatientCondition.ParoCardiaco:
                statusText.text = "PARO CARDIACO";
                statusText.color = criticalColor;
                break;
            default:
                statusText.text = state.displayName.ToUpper();
                statusText.color = normalColor;
                break;
        }
    }
}
