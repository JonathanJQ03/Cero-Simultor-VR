using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonitorDisplayController : MonoBehaviour
{
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

    // ────────────────────────────────────────────────────────────────────
    void Start()
    {
        InitVitalsFromCase();
        BuildWaveformTextures();
        SubscribeFSM();
        ApplyImmediate();
    }

    void OnDestroy()
    {
        if (_fsm != null)
        {
            _fsm.OnCorrectTool -= HandleCorrectTool;
            _fsm.OnWrongTool   -= HandleWrongTool;
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
            _tHR     = _hr     = d.heartRate;
            _tSpo2   = _spo2   = d.spo2;
            _tSysBP  = _sysBP  = d.systolicBP;
            _tDiaBP  = _diaBP  = d.diastolicBP;
            _tPhysStat = _physStat = CalcPhysStat(d.heartRate, d.spo2, d.systolicBP);
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
            _fsm.OnCorrectTool += HandleCorrectTool;
            _fsm.OnWrongTool   += HandleWrongTool;
        }
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
        float hrNorm   = Mathf.Clamp01((_hr - 40f) / 160f);
        float ecgSpeed = Mathf.Lerp(0.25f, 1.1f, hrNorm);
        _ecgUV  = (_ecgUV  + ecgSpeed        * Time.deltaTime) % 1f;
        _spo2UV = (_spo2UV + ecgSpeed * 0.55f * Time.deltaTime) % 1f;
        if (ecgWaveImage)  ecgWaveImage.uvRect  = new Rect(_ecgUV,  0, 1, 1);
        if (spo2WaveImage) spo2WaveImage.uvRect = new Rect(_spo2UV, 0, 1, 1);
    }
}
