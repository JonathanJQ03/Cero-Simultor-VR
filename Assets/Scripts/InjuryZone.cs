using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class InjuryZone : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Radius of the trigger zone (world units)")]
    public float triggerRadius = 0.15f;
    public float interactionCooldown = 2f;

    private float _lastTriggerTime = -99f;
    private SphereCollider _collider;

    void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        _collider.isTrigger = true;
        _collider.radius = triggerRadius;
    }

    void OnTriggerEnter(Collider other)
    {
        if (Time.time - _lastTriggerTime < interactionCooldown) return;

        // Walk up the hierarchy to find a MedicalTool (handles child colliders)
        MedicalTool tool = other.GetComponentInParent<MedicalTool>();
        if (tool == null || !tool.IsHeld) return;

        PatientFSM fsm = PatientFSM.Instance;
        if (fsm == null || fsm.IsFinished) return;

        _lastTriggerTime = Time.time;

        string toolId = tool.GetToolId();
        Debug.Log("[InjuryZone] " + gameObject.name + " triggered by: " + toolId);
        fsm.ProcessTool(toolId);
    }
}
