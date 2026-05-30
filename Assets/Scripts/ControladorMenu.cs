using UnityEngine;
// Esta librería es obligatoria para poder cambiar de escenas
using UnityEngine.SceneManagement; 

public class ControladorMenu : MonoBehaviour
{
    // 1. Función para ir al Quirófano (Escena 1)
    public void IniciarJuego()
    {
        Debug.Log("Cargando el quirófano...");
        SceneManager.LoadScene("Piso Quirofano");
    }

    // 2. Función para ir a los Créditos (Escena 2)
    public void VerCreditos()
    {
        Debug.Log("Cargando la escena de créditos...");
        SceneManager.LoadScene("Creditos");
    }

    // 3. Función para cerrar el simulador
    public void SalirDelJuego()
    {
        Debug.Log("Cerrando el simulador...");
        Application.Quit(); // Nota: Esto solo funciona en el juego ya compilado (.exe)
    }
}