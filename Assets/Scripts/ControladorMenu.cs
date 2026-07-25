using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorMenu : MonoBehaviour
{
    public void IniciarJuego()
    {
        Debug.Log("Cargando escena accidente...");
        SceneManager.LoadScene("EscenaAccidente");
    }

    public void ExplorarHerramientas()
    {
        Debug.Log("Abriendo exploración de herramientas...");
        SceneManager.LoadScene("ExplorarHerramientas");
    }

    public void AbrirConfiguracion()
    {
        Debug.Log("Abriendo configuración...");
        SceneManager.LoadScene("Configuracion");
    }

    public void VerCreditos()
    {
        Debug.Log("Cargando la escena de créditos...");
        SceneManager.LoadScene("Creditos");
    }

    public void SalirDelJuego()
    {
        Debug.Log("Cerrando el simulador...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void VolverAlMenu()
    {
        Debug.Log("Volviendo al menú principal...");
        SceneManager.LoadScene("MenuPrincipal");
    }
}
