using UnityEngine;

public class ClockCountdown : MonoBehaviour
{
    public static ClockCountdown Instance { get; private set; }

    [SerializeField] private float totalSeconds = 300f;

    public event System.Action OnTimeUp;

    public float RemainingTime => remainingTime;
    public bool IsRunning      => _running;
    public bool IsFinished     => remainingTime <= 0f;

    private float remainingTime;
    private bool  _running;
    private TextMesh textMesh;

    void Awake()
    {
        Instance = this;
    }

    // ViaAerea: paciente entra en paro → baja a 1 min si estaba por encima
    public void AjustarParaParo()
    {
        if (remainingTime > 60f)
        {
            remainingTime = 60f;
            UpdateDisplay();
        }
    }

    // ViaAerea: paciente sale del paro → sube a 3 min si estaba por debajo
    public void AjustarParaRecuperacion()
    {
        if (remainingTime < 180f)
        {
            remainingTime = 180f;
            if (!_running) _running = true;
            UpdateDisplay();
        }
    }

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

    public void StartCountdown()
    {
        var st = SessionTimer.Instance;
        if (st != null && !st.Expired)
        {
            remainingTime = st.Remaining;
            st.StopTimer();
        }
        else
        {
            remainingTime = totalSeconds;
        }
        _running = true;
        UpdateDisplay();
    }

    public void StopCountdown()
    {
        _running = false;
    }

    void Update()
    {
        if (!_running || remainingTime <= 0f) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            _running = false;
            UpdateDisplay();
            OnTimeUp?.Invoke();
            return;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        textMesh.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Color feedback: rojo < 60s, amarillo < 120s
        if (textMesh != null)
        {
            if (remainingTime < 60f)
                textMesh.color = new Color(0.95f, 0.20f, 0.20f);
            else if (remainingTime < 120f)
                textMesh.color = new Color(1.00f, 0.75f, 0.10f);
            else
                textMesh.color = Color.white;
        }
    }
}
