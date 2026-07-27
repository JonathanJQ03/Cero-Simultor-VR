using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Coloca este script en un GameObject vacío frente al monitor físico en la escena.
// Construye su propio Canvas World-Space — sin asignaciones en el Inspector.
//
// Escala recomendada del GO: (0.001, 0.001, 0.001) para que el canvas mida ~0.5×0.38m
public class VitalSignsMonitor : MonoBehaviour
{
    // ── Vitals (current smoothed / target) ───────────────────────────────
    float _hr, _spo2, _sys, _dia, _phys;
    float _tHR, _tSpo2, _tSys, _tDia, _tPhys;

    // ── UI ────────────────────────────────────────────────────────────────
    TextMeshProUGUI _lblHR, _lblSpo2, _lblBP, _lblPhys, _lblDiag, _lblStatus;
    Image           _physFill, _headerBg, _flashOverlay;
    RawImage        _ecgImg, _spo2WaveImg;

    // ── Waveforms ─────────────────────────────────────────────────────────
    Texture2D _ecgTex, _spo2Tex;
    float     _ecgUV, _spo2UV;

    // ── Colors ────────────────────────────────────────────────────────────
    static readonly Color C_GREEN  = new Color(0.00f, 0.90f, 0.35f, 1f);
    static readonly Color C_YELLOW = new Color(1.00f, 0.80f, 0.10f, 1f);
    static readonly Color C_RED    = new Color(0.90f, 0.15f, 0.15f, 1f);
    static readonly Color C_CYAN   = new Color(0.10f, 0.80f, 1.00f, 1f);
    static readonly Color32 C32_GREEN  = new Color32(0,   230,  90, 255);
    static readonly Color32 C32_CYAN   = new Color32(0,   200, 255, 255);
    static readonly Color32 C32_BLACK  = new Color32(5,   12,   8, 255);

    PatientFSM _fsm;
    bool _finished;
    float _flashTimer;

    // ── Canvas pixel dimensions (GO should have scale 0.001 → ~0.5×0.38 m) ──
    const float CW = 500f, CH = 380f;

    // ════════════════════════════════════════════════════════════════════════
    void Awake()  { BuildCanvas(); }

    void Start()
    {
        InitVitals();
        BuildWaveforms();
        SubscribeFSM();
        Refresh();
    }

    void OnDestroy()
    {
        if (_fsm != null)
        {
            _fsm.OnCorrectTool   -= OnCorrect;
            _fsm.OnWrongTool     -= OnWrong;
            _fsm.OnSimulationEnd -= OnSimEnd;
        }
    }

    void Update()
    {
        if (_finished) return;

        // Smooth lerp toward targets
        float s = Time.deltaTime * 1.2f;
        _hr   = Mathf.Lerp(_hr,   _tHR,   s);
        _spo2 = Mathf.Lerp(_spo2, _tSpo2, s);
        _sys  = Mathf.Lerp(_sys,  _tSys,  s);
        _dia  = Mathf.Lerp(_dia,  _tDia,  s);
        _phys = Mathf.Lerp(_phys, _tPhys, s);

        Refresh();
        ScrollWaves();

        // Red flash overlay fade
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashOverlay != null)
                _flashOverlay.color = new Color(0.8f, 0.05f, 0.05f,
                    Mathf.Clamp01(_flashTimer) * 0.35f);
        }
    }

    // ── Initialization ───────────────────────────────────────────────────
    void InitVitals()
    {
        var d = PatientCaseManager.Instance?.CurrentCase;
        if (d != null)
        {
            _tHR = _hr = d.heartRate; _tSpo2 = _spo2 = d.spo2;
            _tSys = _sys = d.systolicBP; _tDia = _dia = d.diastolicBP;
            _tPhys = _phys = PhysStat(_hr, _spo2, _sys);

            bool hemo = d.caseType == CaseType.HemorragiaActiva;
            SetDiag(hemo ? "HEMORRAGIA ACTIVA" : "VÍA AÉREA BLOQUEADA",
                    hemo ? C_RED : C_CYAN);
        }
        else
        {
            _tHR = _hr = 130; _tSpo2 = _spo2 = 88;
            _tSys = _sys = 80; _tDia = _dia = 50;
            _tPhys = _phys = 35;
            SetDiag("HEMORRAGIA ACTIVA", C_RED);
        }
    }

    void SubscribeFSM()
    {
        _fsm = PatientFSM.Instance ?? FindObjectOfType<PatientFSM>();
        if (_fsm == null) return;
        _fsm.OnCorrectTool   += OnCorrect;
        _fsm.OnWrongTool     += OnWrong;
        _fsm.OnSimulationEnd += OnSimEnd;
    }

    // ── FSM callbacks ────────────────────────────────────────────────────
    void OnCorrect(string id)
    {
        switch (id)
        {
            case "Bisturi":
            case "TijerasDeTrauma":
                Shift(hrD: -8f, physD: +12f); break;
            case "VendasHemo":
            case "Gasas":
                Shift(hrD: -15f, spo2D: +3f, sysD: +10f, diaD: +6f, physD: +20f); break;
            case "Torniquete":
                Shift(hrD: -25f, spo2D: +5f, sysD: +25f, diaD: +12f, physD: +30f); break;
            case "CanulaDeGuedel":
                Shift(hrD: -8f,  spo2D: +12f, physD: +22f); break;
            case "Laringoscopio":
                Shift(hrD: -10f, spo2D: +12f, physD: +25f); break;
            case "Epinefrina":
            case "Desfibrilador":
                Shift(hrD: -20f, spo2D: +4f, sysD: +15f, physD: +20f); break;
            default:
                Shift(physD: +8f); break;
        }
    }

    void OnWrong(string id, string state)
    {
        Shift(hrD: +14f, spo2D: -5f, sysD: -10f, diaD: -5f, physD: -14f);
        Flash();
    }

    void OnSimEnd(bool success)
    {
        _finished = true;
        if (success)
        { _tHR = 75; _tSpo2 = 97; _tSys = 120; _tDia = 80; _tPhys = 95; }
        else
        { _tHR = 0; _tSpo2 = 0; _tSys = 0; _tDia = 0; _tPhys = 0; }

        SetDiag(success ? "PACIENTE ESTABLE" : "PACIENTE FALLECIDO",
                success ? C_GREEN : C_RED);
    }

    void Shift(float hrD=0, float spo2D=0, float sysD=0, float diaD=0, float physD=0)
    {
        _tHR   = Mathf.Clamp(_tHR   + hrD,   40f, 200f);
        _tSpo2 = Mathf.Clamp(_tSpo2 + spo2D, 55f,  99f);
        _tSys  = Mathf.Clamp(_tSys  + sysD,  40f, 160f);
        _tDia  = Mathf.Clamp(_tDia  + diaD,  25f,  95f);
        _tPhys = Mathf.Clamp(_tPhys + physD,   1f,  98f);
    }

    void Flash() { _flashTimer = 0.6f; }

    // ── UI Refresh ────────────────────────────────────────────────────────
    void Refresh()
    {
        int hr   = Mathf.RoundToInt(_hr);
        int spo2 = Mathf.RoundToInt(_spo2);
        int sys  = Mathf.RoundToInt(_sys);
        int dia  = Mathf.RoundToInt(_dia);
        int phys = Mathf.RoundToInt(_phys);

        Color cHR   = _finished && !(_tHR > 0) ? C_RED : HRColor(hr);
        Color cSpo2 = Spo2Color(spo2);

        if (_lblHR   != null) { _lblHR.text   = _finished && _tHR  <= 0 ? "---" : hr.ToString(); _lblHR.color = cHR; }
        if (_lblSpo2 != null) { _lblSpo2.text  = _finished && _tSpo2 <= 0 ? "0%" : $"{spo2}%"; _lblSpo2.color = cSpo2; }
        if (_lblBP   != null) { _lblBP.text    = _finished && _tSys <= 0 ? "0/0" : $"{sys}/{dia}"; }
        if (_lblPhys != null) { _lblPhys.text  = $"PHYS STAT  {phys}%"; }
        if (_physFill != null)
        {
            _physFill.fillAmount = phys / 100f;
            _physFill.color = phys > 60 ? C_GREEN : phys > 30 ? C_YELLOW : C_RED;
        }
    }

    void SetDiag(string text, Color col)
    {
        if (_lblDiag != null) { _lblDiag.text = text; _lblDiag.color = col; }
        if (_headerBg != null) _headerBg.color = new Color(col.r * 0.15f, col.g * 0.15f, col.b * 0.15f, 1f);
    }

    Color HRColor(int hr)   => hr < 100 ? C_GREEN : hr < 140 ? C_YELLOW : C_RED;
    Color Spo2Color(int sp) => sp >= 95  ? C_GREEN : sp >= 88  ? C_YELLOW : C_RED;

    float PhysStat(float hr, float spo2, float sys)
    {
        float h = 1f - Mathf.Clamp01((hr   -  60f) / 120f);
        float o =      Mathf.Clamp01((spo2 -  60f) /  40f);
        float b =      Mathf.Clamp01((sys  -  60f) /  60f);
        return (h * 0.3f + o * 0.4f + b * 0.3f) * 100f;
    }

    // ── Waveform scrolling ────────────────────────────────────────────────
    void ScrollWaves()
    {
        float hrNorm = Mathf.Clamp01((_hr - 40f) / 160f);
        float speed  = Mathf.Lerp(0.20f, 1.10f, hrNorm);

        if (_finished && _tHR <= 0f) speed = 0.05f; // flatline slow

        _ecgUV  = (_ecgUV  + speed         * Time.deltaTime) % 1f;
        _spo2UV = (_spo2UV + speed * 0.55f * Time.deltaTime) % 1f;

        if (_ecgImg     != null) _ecgImg.uvRect     = new Rect(_ecgUV,  0, 1, 1);
        if (_spo2WaveImg != null) _spo2WaveImg.uvRect = new Rect(_spo2UV, 0, 1, 1);
    }

    // ── Build waveform textures ───────────────────────────────────────────
    void BuildWaveforms()
    {
        _ecgTex  = MakeECG(512, 56, C32_GREEN);
        _spo2Tex = MakeSine(512, 36, C32_CYAN);
        if (_ecgImg     != null) { _ecgImg.texture     = _ecgTex;  _ecgImg.uvRect     = new Rect(0,0,1,1); }
        if (_spo2WaveImg != null) { _spo2WaveImg.texture = _spo2Tex; _spo2WaveImg.uvRect = new Rect(0,0,1,1); }
    }

    Texture2D MakeECG(int w, int h, Color32 col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = C32_BLACK;
        float by = h * 0.42f, amp = h * 0.44f;
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)w;
            float v = 0.15f * Mathf.Exp(-Mathf.Pow((t - 0.10f) / 0.040f, 2))
                    - 0.06f * Mathf.Exp(-Mathf.Pow((t - 0.30f) / 0.015f, 2))
                    + 0.95f * Mathf.Exp(-Mathf.Pow((t - 0.36f) / 0.012f, 2))
                    - 0.28f * Mathf.Exp(-Mathf.Pow((t - 0.43f) / 0.012f, 2))
                    + 0.22f * Mathf.Exp(-Mathf.Pow((t - 0.62f) / 0.070f, 2));
            int y = Mathf.Clamp(Mathf.RoundToInt(by + v * amp), 0, h - 1);
            for (int dy = -1; dy <= 1; dy++)
                px[Mathf.Clamp(y + dy, 0, h - 1) * w + x] = col;
        }
        tex.SetPixels32(px); tex.Apply(); return tex;
    }

    Texture2D MakeSine(int w, int h, Color32 col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = C32_BLACK;
        float by = h * 0.5f, amp = h * 0.35f;
        for (int x = 0; x < w; x++)
        {
            float t = x / (float)w * Mathf.PI * 2f;
            float v = Mathf.Sin(t) * 0.7f + Mathf.Sin(t * 2) * 0.15f;
            int y = Mathf.Clamp(Mathf.RoundToInt(by + v * amp), 0, h - 1);
            for (int dy = -1; dy <= 1; dy++)
                px[Mathf.Clamp(y + dy, 0, h - 1) * w + x] = col;
        }
        tex.SetPixels32(px); tex.Apply(); return tex;
    }

    // ── Canvas & UI construction ──────────────────────────────────────────
    void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1;
        gameObject.AddComponent<GraphicRaycaster>();

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(CW, CH);

        // ── Background ───────────────────────────────────────────────────
        var bg = MakeRect("BG", transform);
        Stretch(bg, 0);
        bg.AddComponent<Image>().color = new Color(0.018f, 0.040f, 0.026f, 1f);

        // ── Header (diagnosis) ───────────────────────────────────────────
        var hdrGO = MakeRect("Header", transform);
        Anchor(hdrGO, 0, CH - 46, CW, 46);
        _headerBg = hdrGO.AddComponent<Image>();
        _headerBg.color = new Color(0.08f, 0.02f, 0.02f, 1f);

        _lblDiag = MakeTMP("Diag", hdrGO.transform, "HEMORRAGIA ACTIVA", 18f,
            C_RED, TextAlignmentOptions.Center, true);
        StretchTMP(_lblDiag, 6, 4);

        // ── ECG section ──────────────────────────────────────────────────
        float ecgY = CH - 46 - 80;
        var ecgGO = MakeRect("ECG_BG", transform);
        Anchor(ecgGO, 0, ecgY, CW, 80);
        ecgGO.AddComponent<Image>().color = C32_BLACK;

        _ecgImg = MakeRect("ECG_Wave", ecgGO.transform).AddComponent<RawImage>();
        _ecgImg.color = Color.white;
        Stretch(_ecgImg.gameObject, 4);

        // ── SpO2 waveform strip ───────────────────────────────────────────
        float spo2Y = ecgY - 44;
        var spo2BG = MakeRect("SPO2_BG", transform);
        Anchor(spo2BG, 0, spo2Y, CW, 44);
        spo2BG.AddComponent<Image>().color = new Color(0.01f, 0.03f, 0.06f, 1f);

        _spo2WaveImg = MakeRect("SPO2_Wave", spo2BG.transform).AddComponent<RawImage>();
        _spo2WaveImg.color = Color.white;
        Stretch(_spo2WaveImg.gameObject, 3);

        // ── Vitals row (3 cells) ─────────────────────────────────────────
        float vitY  = spo2Y - 10;
        float vitH  = 100f;
        float cellW = CW / 3f;

        TextMeshProUGUI _d1, _d2;

        // HR cell
        BuildVitalCell("HR", transform, 0, vitY - vitH, cellW, vitH,
            "FC", "128", "bpm", C_GREEN,
            out _lblHR, out _d1, out _d2);

        // SpO2 cell
        BuildVitalCell("SPO2", transform, cellW, vitY - vitH, cellW, vitH,
            "SpO2", "87%", "", C_CYAN,
            out _lblSpo2, out _d1, out _d2);

        // PA cell
        TextMeshProUGUI dummyBP;
        BuildVitalCell("PA", transform, cellW * 2, vitY - vitH, cellW, vitH,
            "PA", "82/52", "mmHg", C_GREEN,
            out dummyBP, out _d1, out _d2);
        _lblBP = dummyBP;

        // ── PHYS STAT bar ─────────────────────────────────────────────────
        float barY = vitY - vitH - 8;
        float barH = CH - (CH - barY);          // remaining space to bottom
        barH = Mathf.Min(barH, 56f);

        var barBG = MakeRect("PhysBG", transform);
        Anchor(barBG, 10, 8, CW - 20, barH);
        barBG.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.05f, 1f);

        var barTrack = MakeRect("BarTrack", barBG.transform);
        Anchor(barTrack, 8, 8, (CW - 36) * 0.72f, barH - 22);
        barTrack.AddComponent<Image>().color = new Color(0.06f, 0.12f, 0.08f, 1f);

        var barFillGO = MakeRect("BarFill", barTrack.transform);
        _physFill = barFillGO.AddComponent<Image>();
        _physFill.type = Image.Type.Filled;
        _physFill.fillMethod = Image.FillMethod.Horizontal;
        _physFill.fillAmount = 0.35f;
        _physFill.color = C_YELLOW;
        Stretch(barFillGO, 1);

        _lblPhys = MakeTMP("PhysTxt", barBG.transform, "PHYS STAT  35%",
            10f, new Color(0.6f, 0.9f, 0.7f, 1f), TextAlignmentOptions.Left, false);
        var ptRT = _lblPhys.GetComponent<RectTransform>();
        ptRT.anchorMin = Vector2.zero; ptRT.anchorMax = Vector2.one;
        ptRT.offsetMin = new Vector2(8, 2); ptRT.offsetMax = new Vector2(-4, -4);

        // ── Red flash overlay ────────────────────────────────────────────
        var flashGO = MakeRect("Flash", transform);
        Stretch(flashGO, 0);
        _flashOverlay = flashGO.AddComponent<Image>();
        _flashOverlay.color = new Color(0.8f, 0.05f, 0.05f, 0f);
        _flashOverlay.raycastTarget = false;
    }

    void BuildVitalCell(string name, Transform parent,
        float x, float y, float w, float h,
        string label, string value, string unit, Color col,
        out TextMeshProUGUI valueLbl,
        out TextMeshProUGUI labelLbl, out TextMeshProUGUI unitLbl)
    {
        var cell = MakeRect(name, parent);
        Anchor(cell, x + 2, y, w - 4, h);
        var img = cell.AddComponent<Image>();
        img.color = new Color(col.r * 0.06f, col.g * 0.06f, col.b * 0.06f, 1f);

        // Border line on top
        var border = MakeRect("Border", cell.transform);
        var bRT = border.AddComponent<Image>();
        bRT.color = new Color(col.r * 0.4f, col.g * 0.4f, col.b * 0.4f, 1f);
        border.GetComponent<RectTransform>().anchorMin = new Vector2(0,1);
        border.GetComponent<RectTransform>().anchorMax = new Vector2(1,1);
        border.GetComponent<RectTransform>().pivot     = new Vector2(0.5f,1f);
        border.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2f);
        border.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Label (small, top)
        var lbl = MakeTMP("Lbl", cell.transform, label, 9f,
            new Color(col.r * 0.65f, col.g * 0.65f, col.b * 0.65f, 1f),
            TextAlignmentOptions.Center, false);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0,0.65f); lRT.anchorMax = new Vector2(1,1);
        lRT.offsetMin = new Vector2(4,4); lRT.offsetMax = new Vector2(-4,-4);
        labelLbl = lbl;

        // Value (large, center-bottom)
        var val = MakeTMP("Val", cell.transform, value, 28f, col,
            TextAlignmentOptions.Center, true);
        var vRT = val.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0,0.1f); vRT.anchorMax = new Vector2(1,0.68f);
        vRT.offsetMin = new Vector2(4,2); vRT.offsetMax = new Vector2(-4,-2);
        valueLbl = val;

        // Unit (tiny, bottom)
        var unt = MakeTMP("Unit", cell.transform, unit, 7.5f,
            new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 1f),
            TextAlignmentOptions.Center, false);
        var uRT = unt.GetComponent<RectTransform>();
        uRT.anchorMin = new Vector2(0,0); uRT.anchorMax = new Vector2(1,0.18f);
        uRT.offsetMin = new Vector2(4,2); uRT.offsetMax = new Vector2(-4,-2);
        unitLbl = unt;
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    static GameObject MakeRect(string n, Transform parent)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static TextMeshProUGUI MakeTMP(string n, Transform parent, string text,
        float size, Color col, TextAlignmentOptions align, bool bold)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col;
        t.alignment = align;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.enableWordWrapping = false;
        return t;
    }

    static void Stretch(GameObject go, float inset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    static void StretchTMP(TextMeshProUGUI t, float hPad, float vPad)
    {
        var rt = t.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(hPad, vPad); rt.offsetMax = new Vector2(-hPad, -vPad);
    }

    // Anchors by pixel coords (bottom-left origin, y grows up)
    static void Anchor(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot     = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }
}
