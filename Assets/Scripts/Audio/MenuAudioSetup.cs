using UnityEngine;
using UnityEngine.UI;

// Coloca este script en cualquier GameObject de la escena de menú.
// En Awake busca todos los Buttons y les añade MenuButtonAudio automáticamente.
public class MenuAudioSetup : MonoBehaviour
{
    void Awake()
    {
        // Crear UIAudioManager si no existe
        if (UIAudioManager.Instance == null)
        {
            var mgr = new GameObject("_UIAudioManager");
            mgr.AddComponent<UIAudioManager>();
        }

        // Adjuntar MenuButtonAudio a cada Button de la escena
        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            if (btn.GetComponent<MenuButtonAudio>() == null)
                btn.gameObject.AddComponent<MenuButtonAudio>();
        }

        Debug.Log($"[MenuAudioSetup] Sonido UI añadido a {buttons.Length} botones.");
    }
}
