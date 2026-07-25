using UnityEngine;
using UnityEngine.SceneManagement;

/// Cuando se inicia la escena "Piso Quirofano" directamente (sin pasar
/// por la seleccion de herramientas), redirige a "SeleccionHerramientas"
/// para que el flujo de juego sea correcto.
[DefaultExecutionOrder(-200)]
public class QuirofanoBootstrap : MonoBehaviour
{
    void Awake()
    {
        bool cameFromSelector = PatientCaseManager.Instance != null
                                && PatientCaseManager.Instance.SelectedTools.Count > 0;

        if (!cameFromSelector)
        {
            Debug.Log("[QuirofanoBootstrap] Inicio directo detectado — redirigiendo a SeleccionHerramientas.");
            SceneManager.LoadScene("SeleccionHerramientas");
        }
    }
}
