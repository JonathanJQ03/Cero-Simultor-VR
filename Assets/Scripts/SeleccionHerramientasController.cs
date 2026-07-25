using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SeleccionHerramientasController : MonoBehaviour
{
    [System.Serializable]
    public class ToolCard
    {
        public string toolId;
        public GameObject cardRoot;
        public Image cardBg;
        public Image checkOverlay;   // green overlay when selected
        public Image checkIcon;      // checkmark icon
    }

    [Header("Tool Cards")]
    public List<ToolCard> toolCards = new List<ToolCard>();

    [Header("UI")]
    public TextMeshProUGUI txtCounter;
    public GameObject btnIngresar;

    static readonly Color selectedBg      = new Color(0.04f, 0.22f, 0.10f, 1.00f);
    static readonly Color deselectedBg    = new Color(0.10f, 0.14f, 0.18f, 0.95f);
    static readonly Color selectedOverlay = new Color(0.10f, 0.90f, 0.45f, 0.18f);
    static readonly Color hiddenOverlay   = new Color(0,0,0,0);
    static readonly Color selectedCheck   = new Color(0.15f, 0.95f, 0.50f, 1.00f);
    static readonly Color hiddenCheck     = new Color(0,0,0,0);
    static readonly Color selectedBorder  = new Color(0.15f, 0.95f, 0.50f, 0.80f);
    static readonly Color deselectedBorder= new Color(0.20f, 0.30f, 0.40f, 0.70f);

    void Start()
    {
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();

        RefreshAll();
    }

    public void OnToolClicked(int index)
    {
        if (index < 0 || index >= toolCards.Count) return;
        PatientCaseManager.Instance.ToggleTool(toolCards[index].toolId);
        RefreshAll();
    }

    void RefreshAll()
    {
        var mgr = PatientCaseManager.Instance;
        int count   = mgr.SelectedTools.Count;
        int required = PatientCaseManager.RequiredToolCount;

        for (int i = 0; i < toolCards.Count; i++)
        {
            var card = toolCards[i];
            bool selected = mgr.IsToolSelected(card.toolId);

            if (card.cardBg != null)
                card.cardBg.color = selected ? selectedBg : deselectedBg;

            if (card.checkOverlay != null)
                card.checkOverlay.color = selected ? selectedOverlay : hiddenOverlay;

            if (card.checkIcon != null)
                card.checkIcon.color = selected ? selectedCheck : hiddenCheck;

            // Tint all text children green when selected
            foreach (var t in card.cardRoot.GetComponentsInChildren<TextMeshProUGUI>())
                t.color = selected
                    ? new Color(0.15f, 0.95f, 0.50f, 1f)
                    : new Color(0.90f, 0.95f, 1.00f, 1f);
        }

        if (txtCounter != null)
        {
            bool ready = count == required;
            txtCounter.text = ready
                ? $"✓  SELECCIÓN COMPLETA — {count} / {required} HERRAMIENTAS"
                : $"⚠  SELECCIONA EXACTAMENTE {required} DE {toolCards.Count} HERRAMIENTAS   ({count}/{required})";
            txtCounter.color = ready
                ? new Color(0.15f, 0.95f, 0.50f, 1f)
                : new Color(1.00f, 0.75f, 0.10f, 1f);
        }

        if (btnIngresar != null)
            btnIngresar.SetActive(mgr.ReadyToSimulate());
    }

    public void BtnIngresar()
    {
        if (!PatientCaseManager.Instance.ReadyToSimulate()) return;
        SceneManager.LoadScene("Piso Quirofano");
    }

    public void BtnDiagnostico()
    {
        SceneManager.LoadScene("ReportePaciente");
    }
}
