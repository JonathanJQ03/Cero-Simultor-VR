using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MonitorAudioController : MonoBehaviour
{
    [Header("Volumen")]
    [Range(0f, 1f)] public float beepVolume  = 0.6f;
    [Range(0f, 1f)] public float alarmVolume = 0.85f;

    AudioSource _src;
    AudioClip   _beepNormal;      // 440 Hz — ritmo normal
    AudioClip   _beepTachy;       // 660 Hz — taquicardia
    AudioClip   _alarmLoop;       // alarma continua paro

    PatientFSM  _fsm;
    float       _currentHR   = 120f;
    float       _nextBeepIn  = 0f;
    bool        _isAlarm     = false;
    bool        _isParo      = false;
    Coroutine   _alarmRoutine;

    const int SR = 44100;

    // ── Init ──────────────────────────────────────────────────────────────
    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.spatialBlend = 1f;   // 3D espacial
        _src.rolloffMode  = AudioRolloffMode.Logarithmic;
        _src.maxDistance  = 8f;
        _src.loop         = false;
        _src.playOnAwake  = false;

        _beepNormal = GenerateSine("Beep_Normal", 440f, 0.07f, 1f);
        _beepTachy  = GenerateSine("Beep_Tachy",  660f, 0.05f, 1f);
        _alarmLoop  = GenerateAlarm("Alarm", 0.6f);
    }

    void Start()
    {
        _fsm = PatientFSM.Instance;
        if (_fsm != null)
        {
            _fsm.OnStateEnter    += HandleStateEnter;
            _fsm.OnCriticalError += _ => EnterAlarm();
            _fsm.OnSimulationEnd += _ => { StopAlarm(); enabled = false; };
        }

        // Leer vitales iniciales del caso
        if (PatientCaseManager.Instance?.CurrentCase != null)
            _currentHR = PatientCaseManager.Instance.CurrentCase.heartRate;
    }

    void OnDestroy()
    {
        if (_fsm != null)
        {
            _fsm.OnStateEnter    -= HandleStateEnter;
            _fsm.OnSimulationEnd -= _ => { };
        }
    }

    // ── Update: ritmo de pitido ───────────────────────────────────────────
    void Update()
    {
        if (_isParo) return;

        _nextBeepIn -= Time.deltaTime;
        if (_nextBeepIn <= 0f)
        {
            float interval  = 60f / Mathf.Max(_currentHR, 20f);
            _nextBeepIn     = interval;
            AudioClip clip  = _currentHR >= 120f ? _beepTachy : _beepNormal;
            float vol       = _currentHR >= 120f ? beepVolume * 1.1f : beepVolume;
            _src.PlayOneShot(clip, Mathf.Min(vol, 1f));
        }
    }

    // ── FSM handlers ──────────────────────────────────────────────────────
    void HandleStateEnter(string stateId)
    {
        switch (stateId)
        {
            case "PARO_EPINEFRINA":
            case "PARO_DESFIBRILADOR":
                EnterAlarm();
                break;
            case "RECUPERADO_PARO":
                StopAlarm();
                _currentHR = 88f;
                break;
            case "ESTABILIZADO":
                StopAlarm();
                _currentHR = 72f;
                break;
        }
    }

    public void SetHeartRate(float hr) => _currentHR = hr;

    void EnterAlarm()
    {
        if (_isParo) return;
        _isParo = true;
        _src.Stop();
        if (_alarmRoutine != null) StopCoroutine(_alarmRoutine);
        _alarmRoutine = StartCoroutine(PlayAlarmLoop());
    }

    void StopAlarm()
    {
        _isParo = false;
        if (_alarmRoutine != null) { StopCoroutine(_alarmRoutine); _alarmRoutine = null; }
        _src.Stop();
    }

    IEnumerator PlayAlarmLoop()
    {
        while (_isParo)
        {
            _src.PlayOneShot(_alarmLoop, alarmVolume);
            yield return new WaitForSeconds(_alarmLoop.length);
        }
    }

    // ── Generadores procedurales ──────────────────────────────────────────
    static AudioClip GenerateSine(string clipName, float freq, float dur, float vol)
    {
        int n = Mathf.RoundToInt(SR * dur);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t   = i / (float)SR;
            float env = Mathf.Clamp01(t / 0.004f) * Mathf.Clamp01((dur - t) / 0.012f);
            d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * vol * env;
        }
        var c = AudioClip.Create(clipName, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // Alarma: tono 880 Hz, 0.3s ON / 0.3s silencio (1 ciclo = 0.6s)
    static AudioClip GenerateAlarm(string clipName, float cycleDur)
    {
        int n  = Mathf.RoundToInt(SR * cycleDur);
        int on = Mathf.RoundToInt(SR * cycleDur * 0.5f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            if (i < on)
            {
                float t   = i / (float)SR;
                float env = Mathf.Clamp01(t / 0.008f) * Mathf.Clamp01((on / (float)SR - t) / 0.015f);
                // Dos tonos mezclados para sonido de alarma médica
                d[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.6f
                       + Mathf.Sin(2f * Mathf.PI * 1108f * t) * 0.4f) * env;
            }
            // silencio en la segunda mitad
        }
        var c = AudioClip.Create(clipName, n, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }
}
