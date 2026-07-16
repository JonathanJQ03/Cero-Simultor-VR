using UnityEngine;
using TMPro;

public class GameTimer : MonoBehaviour
{
    public TextMeshPro timerText;
    public TextMeshProUGUI timerUGUIText;

    public float baseTimeSeconds = 120f;
    public bool countDown = true;

    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;

    private PatientFSM fsm;
    private float startTime;
    private float remainingTime;
    private bool isRunning;

    public event System.Action OnTimeUp;

    void Start()
    {
        fsm = PatientFSM.Instance;
    }

    public void StartTimer()
    {
        startTime = Time.time;
        remainingTime = baseTimeSeconds;
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning || fsm == null) return;

        if (countDown)
        {
            remainingTime = Mathf.Max(0, baseTimeSeconds - (Time.time - startTime));
        }
        else
        {
            remainingTime = Time.time - startTime;
        }

        UpdateDisplay();

        if (countDown && remainingTime <= 0)
        {
            isRunning = false;
            OnTimeUp?.Invoke();
        }
    }

    void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        string timeStr = $"{minutes:00}:{seconds:00}";

        if (timerText != null)
        {
            timerText.text = timeStr;
            if (countDown)
            {
                if (remainingTime < 30f)
                    timerText.color = criticalColor;
                else if (remainingTime < 60f)
                    timerText.color = warningColor;
                else
                    timerText.color = normalColor;
            }
        }

        if (timerUGUIText != null)
        {
            timerUGUIText.text = timeStr;
        }
    }

    public float GetElapsedTime()
    {
        return Time.time - startTime;
    }

    public float GetRemainingTime()
    {
        return remainingTime;
    }

    public float GetProgress()
    {
        return 1f - (remainingTime / baseTimeSeconds);
    }
}
