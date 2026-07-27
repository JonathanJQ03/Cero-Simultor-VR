using UnityEngine;

public class InjuryMarkerController : MonoBehaviour
{
    [Header("Vía Aérea")]
    public GameObject markerBoca;

    [Header("Hemorragia")]
    public GameObject markerBrazoIzq;
    public GameObject markerBrazoDer;
    public GameObject markerPiernaIzq;
    public GameObject markerPiernaDer;

    [Header("Paro Cardíaco")]
    public GameObject markerTorax;
    public GameObject markerMuslo;

    [Header("Zona de Interacción")]
    [Tooltip("Radio del trigger en espacio local (marcadores normales)")]
    public float zoneRadius = 0.5f;
    [Tooltip("Radio del trigger para tórax (paro cardíaco)")]
    public float toraxZoneRadius = 0.8f;
    [Tooltip("Radio del trigger para muslo (paro cardíaco)")]
    public float musloZoneRadius = 0.5f;

    [Tooltip("Socket fijo antiguo (blue rectangle) — se desactiva al iniciar")]
    public GameObject legacySocket;

    [Header("Pulse")]
    public float pulseSpeed = 2.0f;
    [Range(0.7f, 1f)]  public float pulseMin = 0.85f;
    [Range(1f, 1.5f)]  public float pulseMax = 1.15f;

    GameObject _activeMarker;
    GameObject _activeMarker2;
    Vector3    _baseScale1;
    Vector3    _baseScale2;

    void Awake()
    {
        GameObject[] normal = { markerBoca, markerBrazoIzq, markerBrazoDer,
                                 markerPiernaIzq, markerPiernaDer };

        foreach (var m in normal) EnsureInjuryZone(m, zoneRadius);
        EnsureInjuryZone(markerTorax, toraxZoneRadius);
        EnsureInjuryZone(markerMuslo, musloZoneRadius);
    }

    void EnsureInjuryZone(GameObject marker, float radius)
    {
        if (marker == null) return;

        var zone = marker.GetComponent<InjuryZone>();
        if (zone == null) zone = marker.AddComponent<InjuryZone>();
        zone.triggerRadius = radius;

        var rb = marker.GetComponent<Rigidbody>();
        if (rb == null) rb = marker.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        var col = marker.GetComponent<SphereCollider>();
        if (col == null) col = marker.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = radius;
    }

    PatientFSM _fsm;

    void Start()
    {
        _fsm = PatientFSM.Instance;
        if (_fsm != null)
            _fsm.OnStateEnter += HandleStateEnter;
    }

    void OnDestroy()
    {
        if (_fsm != null)
            _fsm.OnStateEnter -= HandleStateEnter;
    }

    void HandleStateEnter(string stateId)
    {
        switch (stateId)
        {
            case "PARO_EPINEFRINA":
            case "PARO_DESFIBRILADOR":
                ShowCardiacMarkers();
                break;

            case "RECUPERADO_PARO":
                // Volver al marcador del caso original después del paro
                HideAll();
                bool esViaAerea = PatientCaseManager.Instance?.CurrentCase?.caseType
                                  == CaseType.ViaAereaBlockeada;
                if (esViaAerea)
                    Activate(markerBoca);          // zona roja en cabeza → Cánula
                else
                    Activate(markerMuslo);          // zona roja en muslo → Torniquete
                break;

            case "ESPERANDO_LARINGOSCOPIO":
                // Después de Cánula: activar zona boca para Laringoscopio también
                // (el marker ya estaba activo, solo lo refrescamos)
                break;

            case "ESTABILIZADO":
            case "FALLECIDO":
                HideAll();
                break;
        }
    }

    // Llamado por GameManager.StartSimulation()
    public void Initialize()
    {
        // Desactivar el socket fijo heredado si está asignado
        if (legacySocket != null) legacySocket.SetActive(false);

        HideAll();
        _activeMarker  = null;
        _activeMarker2 = null;

        if (PatientCaseManager.Instance?.CurrentCase == null) return;
        var d = PatientCaseManager.Instance.CurrentCase;

        switch (d.caseType)
        {
            case CaseType.ViaAereaBlockeada:
                Activate(markerBoca);
                break;

            case CaseType.HemorragiaActiva:
                switch (d.injuryLocation)
                {
                    case InjuryLocation.BrazoIzq:  Activate(markerBrazoIzq);  break;
                    case InjuryLocation.BrazoDer:  Activate(markerBrazoDer);  break;
                    case InjuryLocation.PiernaIzq: Activate(markerPiernaIzq); break;
                    case InjuryLocation.PiernaDer: Activate(markerPiernaDer); break;
                }
                break;
        }
    }

    void Activate(GameObject marker)
    {
        if (marker == null) return;
        marker.SetActive(true);
        _activeMarker = marker;
        _baseScale1   = marker.transform.localScale;
    }

    void ActivateTwo(GameObject a, GameObject b)
    {
        Activate(a);
        if (b == null) return;
        b.SetActive(true);
        _activeMarker2 = b;
        _baseScale2    = b.transform.localScale;
    }

    void HideAll()
    {
        GameObject[] all = { markerBoca, markerBrazoIzq, markerBrazoDer,
                              markerPiernaIzq, markerPiernaDer, markerTorax, markerMuslo };
        foreach (var m in all)
            if (m != null) m.SetActive(false);
    }

    void Update()
    {
        float factor = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        if (_activeMarker  != null) _activeMarker.transform.localScale  = _baseScale1 * factor;
        if (_activeMarker2 != null) _activeMarker2.transform.localScale = _baseScale2 * factor;
    }

    public void ShowCardiacMarkers()
    {
        HideAll();
        ActivateTwo(markerMuslo, markerTorax);
    }
}
