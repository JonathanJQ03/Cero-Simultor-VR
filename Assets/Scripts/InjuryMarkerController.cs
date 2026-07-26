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

    [Header("Pulse")]
    public float pulseSpeed  = 2.0f;
    [Range(0.7f, 1f)]  public float pulseMin = 0.85f;
    [Range(1f, 1.5f)]  public float pulseMax = 1.15f;

    GameObject _activeMarker;
    GameObject _activeMarker2;
    Vector3    _baseScale1;
    Vector3    _baseScale2;

    // Llamado por GameManager.StartSimulation() — no en Start()
    // para garantizar que el caso ya está asignado al momento de activar.
    public void Initialize()
    {
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
