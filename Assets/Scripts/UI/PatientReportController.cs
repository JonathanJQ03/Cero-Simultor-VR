using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PatientReportController : MonoBehaviour
{
    [Header("Vital Signs")]
    public TextMeshProUGUI fcValueText;
    public TextMeshProUGUI fcStatusText;
    public TextMeshProUGUI spo2ValueText;
    public TextMeshProUGUI spo2StatusText;
    public TextMeshProUGUI paValueText;
    public TextMeshProUGUI paStatusText;
    public TextMeshProUGUI shockValueText;
    public TextMeshProUGUI shockStatusText;

    [Header("Patient Info")]
    public TextMeshProUGUI patientInfoText;

    [Header("Button")]
    public Button btnProceder;

    [Header("Colors")]
    public Color warningColor = new Color(1f, 0.6f, 0f);
    public Color criticalColor = new Color(0.55f, 0f, 0f);

    void OnEnable()
    {
        SetVital(fcValueText, fcStatusText, "128 bpm", "TAQUICARDIA", warningColor);
        SetVital(spo2ValueText, spo2StatusText, "88 %", "HIPOXIA CRITICA", criticalColor);
        SetVital(paValueText, paStatusText, "90/60", "HIPOTENSION", warningColor);
        SetVital(shockValueText, shockStatusText, "III", "SEVERO", criticalColor);

        if (patientInfoText != null)
            patientInfoText.text =
                "⚠ REPORTE DE INGRESO — PACIENTE 01\n" +
                "● Edad: 34 años | Sexo: M | Peso: 78 kg\n" +
                "● Causa: Accidente de tránsito — trauma múltiple\n" +
                "● Hemorragia activa en extremidad inferior derecha";

        if (btnProceder != null)
        {
            btnProceder.onClick.RemoveAllListeners();
            btnProceder.onClick.AddListener(OnProcederClicked);
        }
    }

    void OnDisable()
    {
        if (btnProceder != null)
            btnProceder.onClick.RemoveListener(OnProcederClicked);
    }

    void OnProcederClicked()
    {
        if (GameFlowController.Instance != null)
            GameFlowController.Instance.BtnProcederSeleccion();
    }

    void SetVital(TextMeshProUGUI valText, TextMeshProUGUI statusText, string val, string status, Color color)
    {
        if (valText != null) { valText.text = val; valText.color = color; }
        if (statusText != null) { statusText.text = status; statusText.color = color; }
    }
}
