using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultsController : MonoBehaviour
{
    [Header("Texts")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI feedbackText;

    [Header("Buttons")]
    public Button btnReiniciar;
    public Button btnMenu;

    [Header("Colors")]
    public Color successColor = new Color(0f, 1f, 0.25f);
    public Color failureColor = new Color(0.55f, 0f, 0f);

    void OnEnable()
    {
        if (btnReiniciar != null)
        {
            btnReiniciar.onClick.RemoveAllListeners();
            btnReiniciar.onClick.AddListener(OnReiniciar);
        }
        if (btnMenu != null)
        {
            btnMenu.onClick.RemoveAllListeners();
            btnMenu.onClick.AddListener(OnMenu);
        }
    }

    void OnDisable()
    {
        if (btnReiniciar != null) btnReiniciar.onClick.RemoveListener(OnReiniciar);
        if (btnMenu != null) btnMenu.onClick.RemoveListener(OnMenu);
    }

    void OnReiniciar()
    {
        if (GameFlowController.Instance != null) GameFlowController.Instance.BtnReiniciar();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnMenu() => SceneManager.LoadScene("MenuPrincipal");

    public void ShowResult(bool success, float elapsedTime, int errorCount)
    {
        Color c = success ? successColor : failureColor;

        if (titleText != null)
        {
            titleText.text = success ? "PACIENTE ESTABILIZADO" : "PACIENTE FALLECIDO";
            titleText.color = c;
        }
        if (subtitleText != null)
        {
            subtitleText.text = success
                ? "Excelente trabajo. Los procedimientos aplicados fueron correctos y oportunos."
                : "El paciente no pudo ser estabilizado. Revise los protocolos e intente nuevamente.";
            subtitleText.color = c;
        }
        if (statsText != null)
        {
            int min = Mathf.FloorToInt(elapsedTime / 60f);
            int sec = Mathf.FloorToInt(elapsedTime % 60f);
            statsText.text = $"Tiempo: {min:00}:{sec:00}   |   Errores: {errorCount}   |   Penalización: {errorCount * 5}s";
        }
        if (feedbackText != null)
        {
            feedbackText.text = success
                ? "Monitores en VERDE. Signos vitales estabilizados. ¡Procedimiento exitoso!"
                : $"Se aplicaron procedimientos incorrectos o el tiempo se agotó. Total penalización: {errorCount * 5}s";
        }
    }
}
