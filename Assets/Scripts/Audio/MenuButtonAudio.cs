using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    void Start()
    {
        // Asegurar que el UIAudioManager exista en la escena
        if (UIAudioManager.Instance == null)
        {
            var go = new GameObject("_UIAudioManager");
            go.AddComponent<UIAudioManager>();
        }
    }

    public void OnPointerEnter(PointerEventData _)
    {
        UIAudioManager.Instance?.PlayHover();
    }

    public void OnPointerClick(PointerEventData _)
    {
        UIAudioManager.Instance?.PlayClick();
    }
}
