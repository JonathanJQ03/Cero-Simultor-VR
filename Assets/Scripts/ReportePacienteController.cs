using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ReportePacienteController : MonoBehaviour
{
    [Header("Vitals")]
    public TextMeshProUGUI txtHeartRate;
    public TextMeshProUGUI txtHeartRateLabel;
    public TextMeshProUGUI txtSpo2;
    public TextMeshProUGUI txtSpo2Label;
    public TextMeshProUGUI txtBP;
    public TextMeshProUGUI txtBPLabel;
    public TextMeshProUGUI txtShockLevel;
    public TextMeshProUGUI txtShockLabel;

    [Header("Case Type")]
    public TextMeshProUGUI txtCaseType;
    public UnityEngine.UI.Image imgCaseTypeBg;

    [Header("Patient Report")]
    public TextMeshProUGUI txtPatientHeader;
    public TextMeshProUGUI txtDemographics;
    public TextMeshProUGUI txtCause;
    public TextMeshProUGUI txtFinding;

    void Start()
    {
        if (PatientCaseManager.Instance == null)
        {
            // Fallback: create manager if not present (e.g. scene loaded directly in editor)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();
        }

        var d = PatientCaseManager.Instance.CurrentCase;
        ApplyVitals(d);
    }

    void ApplyVitals(PatientData d)
    {
        if (txtHeartRate != null)
        {
            txtHeartRate.text = $"{d.heartRate} bpm";
            txtHeartRate.color = d.heartRateColor;
        }
        if (txtHeartRateLabel != null)
        {
            txtHeartRateLabel.text = d.heartRateLabel;
            txtHeartRateLabel.color = d.heartRateColor;
        }

        if (txtSpo2 != null)
        {
            txtSpo2.text = $"{d.spo2} %";
            txtSpo2.color = d.spo2Color;
        }
        if (txtSpo2Label != null)
        {
            txtSpo2Label.text = d.spo2Label;
            txtSpo2Label.color = d.spo2Color;
        }

        if (txtBP != null)
        {
            txtBP.text = $"{d.systolicBP}/{d.diastolicBP}";
            txtBP.color = d.bpColor;
        }
        if (txtBPLabel != null)
        {
            txtBPLabel.text = d.bpLabel;
            txtBPLabel.color = d.bpColor;
        }

        if (txtShockLevel != null)
        {
            txtShockLevel.text = ToRoman(d.shockLevel);
            txtShockLevel.color = d.shockColor;
        }
        if (txtShockLabel != null)
        {
            txtShockLabel.text = d.shockLabel;
            txtShockLabel.color = d.shockColor;
        }

        // Case type badge
        bool isHemorragia = d.caseType == CaseType.HemorragiaActiva;
        if (txtCaseType != null)
        {
            txtCaseType.text = isHemorragia
                ? "HEMORRAGIA ACTIVA"
                : "VÍA AÉREA BLOQUEADA";
        }
        if (imgCaseTypeBg != null)
        {
            imgCaseTypeBg.color = isHemorragia
                ? new UnityEngine.Color(0.55f, 0.05f, 0.05f, 0.90f)   // red-dark for hemorrhage
                : new UnityEngine.Color(0.05f, 0.20f, 0.55f, 0.90f);  // blue-dark for airway
        }

        if (txtPatientHeader != null)
            txtPatientHeader.text = $"REPORTE DE INGRESO — PACIENTE {d.patientNumber:D2}";

        if (txtDemographics != null)
            txtDemographics.text = $"Edad: {d.age} años  |  Sexo: {d.sex}  |  Peso: {d.weight} kg";

        if (txtCause != null)
            txtCause.text = $"Causa: {d.cause}";

        if (txtFinding != null)
            txtFinding.text = d.finding;
    }

    string ToRoman(int n)
    {
        switch (n)
        {
            case 1: return "I";
            case 2: return "II";
            case 3: return "III";
            case 4: return "IV";
            default: return n.ToString();
        }
    }

    public void BtnProceder() => SceneManager.LoadScene("SeleccionHerramientas");
    public void BtnMenu()    => SceneManager.LoadScene("MenuPrincipal");
}
