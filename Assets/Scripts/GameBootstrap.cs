using UnityEngine;
using UnityEngine.SceneManagement;

/// Runs before any scene loads. Forces the game to always start at MenuPrincipal,
/// regardless of which scene is open in the editor when Play is pressed.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (SceneManager.GetActiveScene().name != "MenuPrincipal")
        {
            Debug.Log("[GameBootstrap] Redirigiendo a MenuPrincipal.");
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}
