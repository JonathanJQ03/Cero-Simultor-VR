using UnityEngine;

public class ClockCountdown : MonoBehaviour
{
    [SerializeField] private float totalSeconds = 300f;

    private float remainingTime;
    private TextMesh textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>();

        if (textMesh == null)
        {
            Debug.LogError("[ClockCountdown] No se encontró TextMesh en " + gameObject.name);
            enabled = false;
            return;
        }
        remainingTime = totalSeconds;
        UpdateDisplay();
    }

    void Update()
    {
        if (remainingTime <= 0f) return;
        remainingTime -= Time.deltaTime;
        if (remainingTime < 0f) remainingTime = 0f;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        textMesh.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
