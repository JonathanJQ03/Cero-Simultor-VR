using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public enum GamePhase { EscenaAccidente, ReportePaciente, SeleccionHerramientas, Simulacion, Resultado }

    [Header("Phase Panels")]
    public GameObject panelEscenaAccidente;
    public GameObject panelReportePaciente;
    public GameObject panelSeleccionHerramientas;
    public GameObject panelResultado;

    [Header("Simulation HUD")]
    public GameObject simulationHUD;

    [Header("Tool Selection")]
    public int requiredToolCount = 5;

    public GamePhase CurrentPhase { get; private set; }
    public List<string> SelectedTools { get; private set; } = new List<string>();

    public event System.Action<GamePhase> OnPhaseChanged;
    public event System.Action<List<string>> OnSelectionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start() => GoToPhase(GamePhase.ReportePaciente);

    public void GoToPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        SetPanel(panelEscenaAccidente,        phase == GamePhase.EscenaAccidente);
        SetPanel(panelReportePaciente,        phase == GamePhase.ReportePaciente);
        SetPanel(panelSeleccionHerramientas,  phase == GamePhase.SeleccionHerramientas);
        SetPanel(simulationHUD,              phase == GamePhase.Simulacion);
        SetPanel(panelResultado,             phase == GamePhase.Resultado);
        OnPhaseChanged?.Invoke(phase);
    }

    void SetPanel(GameObject panel, bool active) { if (panel != null) panel.SetActive(active); }

    public void BtnSiguiente() => GoToPhase(GamePhase.ReportePaciente);
    public void BtnProcederSeleccion() => GoToPhase(GamePhase.SeleccionHerramientas);

    public void BtnIngresarQuirofano()
    {
        if (!ReadyToSimulate()) return;
        GoToPhase(GamePhase.Simulacion);
        if (GameManager.Instance != null)
            GameManager.Instance.StartSimulation();
    }

    public void BtnReiniciar()
    {
        SelectedTools.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BtnMenu() => SceneManager.LoadScene("MenuPrincipal");

    public bool ToggleTool(string toolId)
    {
        if (SelectedTools.Contains(toolId))
        {
            SelectedTools.Remove(toolId);
            OnSelectionChanged?.Invoke(SelectedTools);
            return false;
        }
        if (SelectedTools.Count < requiredToolCount)
        {
            SelectedTools.Add(toolId);
            OnSelectionChanged?.Invoke(SelectedTools);
            return true;
        }
        return false;
    }

    public bool IsToolSelected(string toolId) => SelectedTools.Contains(toolId);
    public bool ReadyToSimulate() => SelectedTools.Count == requiredToolCount;

    public void OnSimulationEnd(bool success)
    {
        GoToPhase(GamePhase.Resultado);
        var results = FindObjectOfType<ResultsController>();
        if (results != null)
        {
            float elapsed = GameManager.Instance != null ? GameManager.Instance.GetElapsedTime() : 0f;
            int errors = PatientFSM.Instance != null ? PatientFSM.Instance.ErrorCount : 0;
            results.ShowResult(success, elapsed, errors);
        }
    }
}
