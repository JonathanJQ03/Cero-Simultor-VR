using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonitorDisplayController : MonoBehaviour
{
    [Header("Status Color (borde + header)")]
    public Image diagnosisBg;
    public Image borderTop;
    public Image borderBottom;
    public Image borderLeft;
    public Image borderRight;

    [Header("Paro Cardíaco Overlay")]
    public Image paroFlashOverlay;

    // ── ECG ─────────────────────────────────────────────────────────────
    [Header("ECG")]
    public RawImage          ecgWaveImage;
    public TextMeshProUGUI   txtHeartRate;   // large number
    public TextMeshProUGUI   txtBpmLabel;    // "bpm"

    // ── NIBP ─────────────────────────────────────────────────────────────
    [Header("NIBP")]
    public TextMeshProUGUI   txtBP;          // "85/55"
    public TextMeshProUGUI   txtMmHg;        // "mmHg"

    // ── SpO2 ─────────────────────────────────────────────────────────────
    [Header("SpO2")]
    public TextMeshProUGUI   txtSpO2;        // "87%"
    public TextMeshProUGUI   txtSpO2Warn;    // "⚠"
    public RawImage          spo2WaveImage;

    // ── Shock Index ───────────────────────────────────────────────────────
    [Header("Shock Index")]
    public TextMeshProUGUI   txtShockIndex;  // integer value (SI×100)
    public Image             shockIndexBg;   // border color reacts to severity

    // ── Phys Stat ─────────────────────────────────────────────────────────
    [Header("Phys Stat")]
    public Image             physStatFill;
    public TextMeshProUGUI   txtPhysStat;    // "PHYS STAT: 42%"

    // ── Diagnosis ────────────────────────────────────────────────────────
    [Header("Diagnosis")]
    public TextMeshProUGUI   txtDiagnosis;   // "HEMORRAGIA ACTIVA" / "VIA AEREA BLOQUEADA"

    // ── Runtime vitals ───────────────────────────────────────────────────
    float _hr, _spo2, _sysBP, _diaBP, _physStat;
    float _tHR, _tSpo2, _tSysBP, _tDiaBP, _tPhysStat;

    // ── Waveform ──────────────────────────────────────────────────────────
    Texture2D _ecgTex, _spo2Tex;
    float     _ecgUV, _spo2UV;

    static readonly Color32 C_ECG    = new Color32(0,   255, 80,  255);
    static readonly Color32 C_CYAN   = new Color32(0,   210, 255, 255);
    static readonly Color32 C_YELLOW = new Color32(255, 200, 0,   255);
    static readonly Color32 C_RED    = new Color32(230, 40,  40,  255);
    static readonly Color32 C_GREEN  = new Color32(0,   220, 80,  255);
    static readonly Color32 C_WARN   = new Color32(255, 200, 0,   255);
    static readonly Color32 C_BG     = new Color32(0,   0,   0,   255);

    PatientFSM _fsm;
    Coroutine  _paroFlash;
    bool       _isParoCardiaco;
    bool       _isFlatline;

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (paroFlashOverlay != null) paroFlashOverlay.color = new Color(1,0,0,0);
        SetStatusColor(S_YELLOW);
        InitVitalsFromCase();
        BuildWaveformTextures();
        SubscribeFSM();
        ApplyImmediate();
    }

    void OnDestroy()
    {
        if (_fsm != null)
        {
            _fsm.OnCorrectTool  -= HandleCorrectTool;
            _fsm.OnWrongTool    -= HandleWrongTool;
            _fsm.OnStateEnter   -= HandleStateEnter;
            _fsm.OnCriticalError -= HandleCriticalError;
            _fsm.OnSimulationEnd -= HandleSimulationEnd;
        }
    }

    void Update()
    {
        // Smooth animated approach to targets
        float s = Time.deltaTime * 1.2f;
        _hr      = Mathf.Lerp(_hr,      _tHR,      s);
        _spo2    = Mathf.Lerp(_spo2,    _tSpo2,    s);
        _sysBP   = Mathf.Lerp(_sysBP,   _tSysBP,   s);
        _diaBP   = Mathf.Lerp(_diaBP,   _tDiaBP,   s);
        _physStat = Mathf.Lerp(_physStat, _tPhysStat, s);

        RefreshUI();
        ScrollWaves();
    }

    // ── Initialisation ────────────────────────────────────────────────────
    void InitVitalsFromCase()
    {
        if (PatientCaseManager.Instance?.CurrentCase != null)
        {
            var d = PatientCaseManager.Instance.CurrentCase;
            _tHR       = _hr       = d.heartRate;
            _tSpo2     = _spo2     = d.spo2;
            _tSysBP    = _sysBP    = d.systolicBP;
            _tDiaBP    = _diaBP    = d.diastolicBP;
            _tPhysStat = _physStat = CalcPhysStat(d.heartRate, d.spo2, d.systolicBP);

            if (txtDiagnosis != null)
            {
                bool isHemo = d.caseType == CaseType.HemorragiaActiva;
                txtDiagnosis.text  = isHemo ? "HEMORRAGIA ACTIVA" : "VIA AEREA BLOQUEADA";
                txtDiagnosis.color = isHemo
                    ? new Color(0.95f, 0.25f, 0.25f, 1f)
                    : new Color(0.25f, 0.60f, 1.00f, 1f);
            }
        }
        else
        {
            _tHR = _hr = 135; _tSpo2 = _spo2 = 86;
            _tSysBP = _sysBP = 82; _tDiaBP = _diaBP = 52; _tPhysStat = _physStat = 34;
        }
    }

    void SubscribeFSM()
    {
        _fsm = FindObjectOfType<PatientFSM>();
        if (_fsm != null)
        {
            _fsm.OnCorrectTool   += HandleCorrectTool;
            _fsm.OnWrongTool     += HandleWrongTool;
            _fsm.OnStateEnter    += HandleStateEnter;
            _fsm.OnCriticalError += HandleCriticalError;
            _fsm.OnSimulationEnd += HandleSimulationEnd;
        }
    }

    // ── Nuevos handlers de estado ─────────────────────────────────────────
    // Colores de estado del monitor
    static readonly Color S_GREEN  = new Color(0.10f, 0.85f, 0.30f, 1f);
    static readonly Color S_YELLOW = new Color(0.95f, 0.75f, 0.05f, 1f);
    static readonly Color S_RED    = new Color(0.90f, 0.10f, 0.10f, 1f);
    static readonly Color S_DARK   = new Color(0.20f, 0.02f, 0.02f, 1f);

    void SetStatusColor(Color color)
    {
        Color dimBg = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 0.85f);
        if (diagnosisBg) diagnosisBg.color = dimBg;
        if (borderTop)    borderTop.color    = color;
        if (borderBottom) borderBottom.color = color;
        if (borderLeft)   borderLeft.color   = color;
        if (borderRight)  borderRight.color  = color;
    }

    void HandleStateEnter(string stateId)
    {
        switch (stateId)
        {
            case "ESPERANDO_BISTURI":
            case "ESPERANDO_GASAS":
            case "ESPERANDO_TORNIQUETE":
            case "ESPERANDO_CANULA":
            case "ESPERANDO_LARINGOSCOPIO":
                SetStatusColor(S_YELLOW);
                break;
            case "PARO_EPINEFRINA":
            case "PARO_DESFIBRILADOR":
                SetStatusColor(S_RED);
                EnterParoCardiaco();
                break;
            case "RECUPERADO_PARO":
                SetStatusColor(S_YELLOW);
                ExitParoCardiaco();
                Shift(hrDelta: +55f, spo2Delta: +28f, sysDelta: +47f, diaDelta: +32f, physDelta: +25f);
                break;
            case "ESTABILIZADO":
                SetStatusColor(S_GREEN);
                ExitParoCardiaco();
                _tHR = 72f; _tSpo2 = 98f; _tSysBP = 115f; _tDiaBP = 75f; _tPhysStat = 92f;
                break;
            case "FALLECIDO":
                SetStatusColor(S_DARK);
                ExitParoCardiaco();
                _isFlatline = true;
                _tHR = 0f; _tSpo2 = 0f; _tSysBP = 0f; _tDiaBP = 0f; _tPhysStat = 0f;
                break;
        }
    }

    void HandleCriticalError(string toolId)
    {
        EnterParoCardiaco();
    }

    void HandleSimulationEnd(bool success)
    {
        ExitParoCardiaco();
    }

    void EnterParoCardiaco()
    {
        if (_isParoCardiaco) return;
        _isParoCardiaco = true;
        _tHR = 32f; _tSpo2 = 62f; _tSysBP = 46f; _tDiaBP = 28f;
        if (txtDiagnosis != null)
        {
            txtDiagnosis.text  = "PARO CARDÍACO";
            txtDiagnosis.color = new Color(1f, 0.15f, 0.15f, 1f);
        }
        if (_paroFlash != null) StopCoroutine(_paroFlash);
        _paroFlash = StartCoroutine(ParoFlashRoutine());
    }

    void ExitParoCardiaco()
    {
        _isParoCardiaco = false;
        _isFlatline     = false;
        if (_paroFlash != null) { StopCoroutine(_paroFlash); _paroFlash = null; }
        if (paroFlashOverlay != null) paroFlashOverlay.color = new Color(1,0,0,0);
    }

    IEnumerator ParoFlashRoutine()
    {
        while (_isParoCardiaco)
        {
            if (paroFlashOverlay != null)
            {
                // Fade rojo in
                for (float t = 0; t < 0.3f; t += Time.deltaTime)
                {
                    paroFlashOverlay.color = new Color(1, 0, 0, Mathf.Lerp(0, 0.35f, t / 0.3f));
                    yield return null;
                }
                // Fade rojo out
                for (float t = 0; t < 0.5f; t += Time.deltaTime)
                {
                    paroFlashOverlay.color = new Color(1, 0, 0, Mathf.Lerp(0.35f, 0, t / 0.5f));
                    yield return null;
                }
            }
            yield return new WaitForSeconds(0.6f);
        }
        if (paroFlashOverlay != null) paroFlashOverlay.color = new Color(1,0,0,0);
    }

    void ApplyImmediate()
    {
        _hr = _tHR; _spo2 = _tSpo2;
        _sysBP = _tSysBP; _diaBP = _tDiaBP; _physStat = _tPhysStat;
        RefreshUI();
    }

    // ── FSM callbacks ─────────────────────────────────────────────────────
    void HandleCorrectTool(string toolId)
    {
        switch (toolId)
        {
            case "Bisturi":
            case "TijerasDeTrauma":
                Shift(hrDelta: -8f, physDelta: +12f);
                break;
            case "VendasHemo":
            case "Gasas":
                Shift(hrDelta: -15f, spo2Delta: +3f, sysDelta: +10f, diaDelta: +6f, physDelta: +20f);
                break;
            case "Torniquete":
                Shift(hrDelta: -25f, spo2Delta: +5f, sysDelta: +25f, diaDelta: +12f, physDelta: +30f);
                break;
            case "CanulaDeGuedel":
                Shift(hrDelta: -8f,  spo2Delta: +12f, physDelta: +22f);
                break;
            case "Laringoscopio":
                Shift(hrDelta: -10f, spo2Delta: +12f, physDelta: +25f);
                break;
            case "Epinefrina":
            case "Desfibrilador":
                Shift(hrDelta: -20f, spo2Delta: +4f, sysDelta: +15f, physDelta: +20f);
                break;
            default:
                Shift(physDelta: +8f);
                break;
        }
    }

    void HandleWrongTool(string toolId, string stateId)
    {
        Shift(hrDelta: +12f, spo2Delta: -4f, sysDelta: -8f, diaDelta: -4f, physDelta: -12f);
    }

    void Shift(float hrDelta = 0, float spo2Delta = 0,
               float sysDelta = 0, float diaDelta = 0, float physDelta = 0)
    {
        _tHR      = Mathf.Clamp(_tHR      + hrDelta,   40f, 200f);
        _tSpo2    = Mathf.Clamp(_tSpo2    + spo2Delta, 55f,  99f);
        _tSysBP   = Mathf.Clamp(_tSysBP   + sysDelta,  50f, 140f);
        _tDiaBP   = Mathf.Clamp(_tDiaBP   + diaDelta,  30f,  90f);
        _tPhysStat = Mathf.Clamp(_tPhysStat + physDelta,  2f,  98f);
    }

    // ── UI refresh ────────────────────────────────────────────────────────
    void RefreshUI()
    {
        int hr   = Mathf.RoundToInt(_hr);
        int spo2 = Mathf.RoundToInt(_spo2);
        int sys  = Mathf.RoundToInt(_sysBP);
        int dia  = Mathf.RoundToInt(_diaBP);
        int phys = Mathf.RoundToInt(_physStat);
        int si   = Mathf.RoundToInt((_sysBP > 1f ? _hr / _sysBP : 9f) * 100f);

        // Heart rate
        if (txtHeartRate)
        {
            txtHeartRate.text  = hr.ToString();
            txtHeartRate.color = HRColor(hr);
        }

        // BP
        if (txtBP) txtBP.text = $"{sys}/{dia}";

        // SpO2
        if (txtSpO2)
        {
            txtSpO2.text  = $"{spo2}%";
            txtSpO2.color = Spo2Color(spo2);
        }
        if (txtSpO2Warn)
            txtSpO2Warn.gameObject.SetActive(spo2 < 95);

        // Shock Index
        if (txtShockIndex) txtShockIndex.text = si.ToString();
        if (shockIndexBg)
        {
            shockIndexBg.color = si > 100 ? new Color(0.5f, 0.05f, 0.05f, 0.85f)
                               : si > 80  ? new Color(0.4f, 0.30f, 0.05f, 0.85f)
                               :            new Color(0.05f, 0.30f, 0.15f, 0.85f);
        }

        // Phys stat bar
        if (physStatFill)
        {
            physStatFill.fillAmount = phys / 100f;
            physStatFill.color = phys > 60 ? Color.green
                               : phys > 30 ? new Color(1f, 0.7f, 0.05f)
                               :             new Color(0.85f, 0.1f, 0.1f);
        }
        if (txtPhysStat) txtPhysStat.text = $"PHYS STAT:  {phys}%";
    }

    Color HRColor(int hr)
        => hr < 100 ? (Color)C_GREEN : hr < 140 ? (Color)C_WARN : (Color)C_RED;

    Color Spo2Color(int spo2)
        => spo2 >= 95 ? (Color)C_GREEN : spo2 >= 90 ? (Color)C_WARN : (Color)C_RED;

    float CalcPhysStat(float hr, float spo2, float sys)
    {
        float h = 1f - Mathf.Clamp01((hr   -  60f) / 120f);
        float o =      Mathf.Clamp01((spo2 -  60f) /  40f);
        float b =      Mathf.Clamp01((sys  -  60f) /  60f);
        return (h * 0.3f + o * 0.4f + b * 0.3f) * 100f;
    }

    // ── Waveform textures ─────────────────────────────────────────────────
    void BuildWaveformTextures()
    {
        _ecgTex  = MakeECGTexture(512, 64, C_ECG);
        _spo2Tex = MakeSineTexture(512, 48, C_CYAN);

        if (ecgWaveImage)  { ecgWaveImage.texture  = _ecgTex;  ecgWaveImage.uvRect  = new Rect(0,0,1,1); }
        if (spo2WaveImage) { spo2WaveImage.texture = _spo2Tex; spo2WaveImage.uvRect = new Rect(0,0,1,1); }
    }

    Texture2D MakeECGTexture(int w, int h, Color32 col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = C_BG;
        float base_y = h * 0.45f, amp = h * 0.42f;
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)w;
            float v = ECGSample(t);
            int y = Mathf.Clamp(Mathf.RoundToInt(base_y + v * amp), 0, h - 1);
            for (int dy = -1; dy <= 1; dy++)
            {
                int py = Mathf.Clamp(y + dy, 0, h - 1);
                px[py * w + x] = col;
            }
        }
        tex.SetPixels32(px); tex.Apply(); return tex;
    }

    float ECGSample(float t)
    {
        float p  =  0.15f * Mathf.Exp(-Mathf.Pow((t - 0.10f) / 0.040f, 2));
        float q  = -0.06f * Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.015f, 2));
        float r  =  0.95f * Mathf.Exp(-Mathf.Pow((t - 0.36f) / 0.012f, 2));
        float s  = -0.28f * Mathf.Exp(-Mathf.Pow((t - 0.43f) / 0.012f, 2));
        float tw =  0.22f * Mathf.Exp(-Mathf.Pow((t - 0.62f) / 0.070f, 2));
        return p + q + r + s + tw;
    }

    Texture2D MakeSineTexture(int w, int h, Color32 col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = C_BG;
        float base_y = h * 0.5f, amp = h * 0.36f;
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)w * Mathf.PI * 2f;
            float v = Mathf.Sin(t) * 0.70f + Mathf.Sin(t * 2) * 0.15f;
            int y = Mathf.Clamp(Mathf.RoundToInt(base_y + v * amp), 0, h - 1);
            for (int dy = -1; dy <= 1; dy++)
            {
                int py = Mathf.Clamp(y + dy, 0, h - 1);
                px[py * w + x] = col;
            }
        }
        tex.SetPixels32(px); tex.Apply(); return tex;
    }

    void ScrollWaves()
    {
        if (_isFlatline)
        {
            // Línea recta — mostrar textura estática sin scroll o scroll muy lento
            float deadSpeed = 0.04f;
            _ecgUV  = (_ecgUV  + deadSpeed * Time.deltaTime) % 1f;
            _spo2UV = (_spo2UV + deadSpeed * Time.deltaTime) % 1f;
            if (ecgWaveImage)  ecgWaveImage.uvRect  = new Rect(_ecgUV, 0.45f, 1, 0.02f); // franja plana
            if (spo2WaveImage) spo2WaveImage.uvRect = new Rect(_spo2UV,0.47f, 1, 0.02f);
            return;
        }

        float hrNorm   = Mathf.Clamp01((_hr - 40f) / 160f);
        float ecgSpeed = _isParoCardiaco
            ? Mathf.Lerp(0.08f, 0.18f, hrNorm)   // muy lento en paro
            : Mathf.Lerp(0.25f, 1.10f, hrNorm);
        _ecgUV  = (_ecgUV  + ecgSpeed        * Time.deltaTime) % 1f;
        _spo2UV = (_spo2UV + ecgSpeed * 0.55f * Time.deltaTime) % 1f;
        if (ecgWaveImage)  ecgWaveImage.uvRect  = new Rect(_ecgUV,  0, 1, 1);
        if (spo2WaveImage) spo2WaveImage.uvRect = new Rect(_spo2UV, 0, 1, 1);
    }
}
