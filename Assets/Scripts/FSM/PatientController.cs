using UnityEngine;
using System.Collections;

public class PatientController : MonoBehaviour
{
    [Header("Skin Color Materials")]
    public Material skinNormalMaterial;
    public Material skinCyanosisMaterial;
    public Material skinPaleMaterial;
    public Material skinDeadMaterial;

    [Header("Body Parts")]
    public Renderer skinRenderer;
    public Renderer woundRenderer;
    public ParticleSystem bleedingEffect;

    [Header("Vitals")]
    public float heartRate = 120f;
    public float spo2 = 88f;
    public float bloodPressureSystolic = 90f;
    public float bloodPressureDiastolic = 60f;

    [Header("Audio")]
    public AudioSource heartAudioSource;
    public AudioClip heartbeatNormal;
    public AudioClip heartbeatFast;
    public AudioClip heartbeatAgonal;
    public AudioClip flatlineSound;

    private PatientFSM fsm;
    private Coroutine vitalsCoroutine;

    void Start()
    {
        fsm = PatientFSM.Instance;
        if (fsm == null)
        {
            Debug.LogError("PatientController: PatientFSM not found!");
            enabled = false;
            return;
        }

        fsm.OnStateEnter += OnPatientStateChanged;
        fsm.OnCorrectTool += OnCorrectToolUsed;
        fsm.OnWrongTool += OnWrongToolUsed;
        fsm.OnCriticalError += OnCriticalErrorOccurred;
        fsm.OnSimulationEnd += OnSimulationEnded;

        vitalsCoroutine = StartCoroutine(UpdateVitalsLoop());
    }

    void OnDestroy()
    {
        if (fsm != null)
        {
            fsm.OnStateEnter -= OnPatientStateChanged;
            fsm.OnCorrectTool -= OnCorrectToolUsed;
            fsm.OnWrongTool -= OnWrongToolUsed;
            fsm.OnCriticalError -= OnCriticalErrorOccurred;
            fsm.OnSimulationEnd -= OnSimulationEnded;
        }
    }

    public void InitializePatient()
    {
        SetSkinColor(skinPaleMaterial);
        if (bleedingEffect != null) bleedingEffect.Play();
        heartRate = 120f;
        spo2 = 88f;
        bloodPressureSystolic = 90f;
        bloodPressureDiastolic = 60f;
    }

    void OnPatientStateChanged(string stateId)
    {
        var state = fsm.GetState(stateId);
        if (state == null) return;

        switch (state.condition)
        {
            case PatientCondition.HemorragiaActiva:
                if (state.id == "ESPERANDO_BISTURI")
                {
                    SetSkinColor(skinPaleMaterial);
                    if (bleedingEffect != null) bleedingEffect.Play();
                    heartRate = 120f;
                    spo2 = 88f;
                    bloodPressureSystolic = 90f;
                    bloodPressureDiastolic = 60f;
                    PlayHeartSound(heartbeatFast);
                }
                else if (state.id == "ESPERANDO_GASAS")
                {
                    if (woundRenderer != null)
                        woundRenderer.material.color = new Color(0.8f, 0.2f, 0.2f);
                    heartRate = 115f;
                    bloodPressureSystolic = 85f;
                }
                else if (state.id == "ESPERANDO_TORNIQUETE")
                {
                    if (woundRenderer != null)
                        woundRenderer.material.color = new Color(0.6f, 0.1f, 0.1f);
                    heartRate = 130f;
                    spo2 = 82f;
                    PlayHeartSound(heartbeatFast);
                }
                else if (state.id == "RECUPERADO_PARO")
                {
                    heartRate = 110f;
                    spo2 = 90f;
                    bloodPressureSystolic = 95f;
                    bloodPressureDiastolic = 65f;
                    SetSkinColor(skinPaleMaterial);
                    PlayHeartSound(heartbeatFast);
                }
                break;

            case PatientCondition.ParoCardiaco:
                SetSkinColor(skinCyanosisMaterial);
                if (bleedingEffect != null) bleedingEffect.Stop();
                heartRate = 0f;
                spo2 = 40f;
                bloodPressureSystolic = 0f;
                bloodPressureDiastolic = 0f;
                PlayHeartSound(flatlineSound);
                break;

            case PatientCondition.Estabilizado:
                SetSkinColor(skinNormalMaterial);
                if (bleedingEffect != null) bleedingEffect.Stop();
                if (woundRenderer != null)
                    woundRenderer.material.color = new Color(0.4f, 0.4f, 0.4f);
                heartRate = 75f;
                spo2 = 97f;
                bloodPressureSystolic = 120f;
                bloodPressureDiastolic = 80f;
                PlayHeartSound(heartbeatNormal);
                break;

            case PatientCondition.Fallecido:
                SetSkinColor(skinDeadMaterial);
                if (bleedingEffect != null) bleedingEffect.Stop();
                heartRate = 0f;
                spo2 = 0f;
                bloodPressureSystolic = 0f;
                bloodPressureDiastolic = 0f;
                PlayHeartSound(flatlineSound);
                break;
        }
    }

    void OnCorrectToolUsed(string toolId)
    {
        Debug.Log($"Correcto: {toolId}");
    }

    void OnWrongToolUsed(string toolId, string stateId)
    {
        Debug.Log($"Incorrecto: {toolId} en estado {stateId}");
    }

    void OnCriticalErrorOccurred(string toolId)
    {
        Debug.LogError($"Error crítico con: {toolId}. Paciente entra en paro.");
    }

    void OnSimulationEnded(bool success)
    {
        if (success)
        {
            Debug.Log("Paciente estabilizado!");
        }
        else
        {
            Debug.Log("Paciente fallecido.");
        }
    }

    void SetSkinColor(Material mat)
    {
        if (skinRenderer != null && mat != null)
            skinRenderer.material = mat;
    }

    void PlayHeartSound(AudioClip clip)
    {
        if (heartAudioSource != null && clip != null)
        {
            heartAudioSource.clip = clip;
            heartAudioSource.loop = true;
            heartAudioSource.Play();
        }
    }

    IEnumerator UpdateVitalsLoop()
    {
        while (true)
        {
            if (fsm != null && !fsm.IsFinished)
            {
                var state = fsm.CurrentState;
                if (state != null)
                {
                    float progress = fsm.TimeInState;
                    if (state.condition == PatientCondition.HemorragiaActiva)
                    {
                        spo2 = Mathf.Max(40f, spo2 - Time.deltaTime * 2f);
                        heartRate = Mathf.Lerp(heartRate, 140f, Time.deltaTime * 0.5f);
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void SetBleedingActive(bool active)
    {
        if (bleedingEffect != null)
        {
            if (active) bleedingEffect.Play();
            else bleedingEffect.Stop();
        }
    }
}
