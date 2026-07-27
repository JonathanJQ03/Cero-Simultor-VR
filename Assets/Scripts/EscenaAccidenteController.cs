using UnityEngine;
using UnityEngine.SceneManagement;

public class EscenaAccidenteController : MonoBehaviour
{
    void Start()
    {
        // Ensure a fresh case is generated each time this scene loads
        if (PatientCaseManager.Instance == null)
            new GameObject("PatientCaseManager").AddComponent<PatientCaseManager>();
        else
            PatientCaseManager.Instance.GenerateNewCase();
    }

    public void BtnSiguiente()
    {
        SceneManager.LoadScene("SeleccionDificultad");
    }
}
