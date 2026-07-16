using UnityEngine;
using TMPro;
using System.Collections;

public class MessageController : MonoBehaviour
{
    public TextMeshPro messageText;
    public TextMeshProUGUI messageUGUIText;

    public float displayDuration = 3f;
    public float fadeDuration = 0.5f;
    public Color successColor = Color.green;
    public Color errorColor = Color.red;
    public Color infoColor = Color.white;

    private Coroutine activeCoroutine;

    public void ShowMessage(string message, Color color)
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(DisplayMessage(message, color));
    }

    public void ShowSuccess(string message)
    {
        ShowMessage(message, successColor);
    }

    public void ShowError(string message)
    {
        ShowMessage(message, errorColor);
    }

    public void ShowInfo(string message)
    {
        ShowMessage(message, infoColor);
    }

    public void ShowPersistent(string message, Color color)
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
        if (messageUGUIText != null)
        {
            messageUGUIText.text = message;
            messageUGUIText.color = color;
        }
    }

    public void ClearMessage()
    {
        if (messageText != null)
        {
            messageText.text = "";
            messageText.color = infoColor;
        }
        if (messageUGUIText != null)
        {
            messageUGUIText.text = "";
            messageUGUIText.color = infoColor;
        }
    }

    IEnumerator DisplayMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
        if (messageUGUIText != null)
        {
            messageUGUIText.text = message;
            messageUGUIText.color = color;
        }

        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1, 0, elapsed / fadeDuration);
            if (messageText != null)
                messageText.color = new Color(color.r, color.g, color.b, alpha);
            if (messageUGUIText != null)
                messageUGUIText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        ClearMessage();
    }
}
