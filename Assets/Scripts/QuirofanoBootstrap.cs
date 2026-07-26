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
        bool ready = PatientCaseManager.Instance != null
                     && PatientCaseManager.Instance.ReadyToSimulate();

        if (!ready)
        {
            Debug.Log("[QuirofanoBootstrap] Flujo incompleto — redirigiendo a MenuPrincipal.");
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}
