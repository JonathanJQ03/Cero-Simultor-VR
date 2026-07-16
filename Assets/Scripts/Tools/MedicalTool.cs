using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class MedicalTool : MonoBehaviour
{
    public MedicalToolTag toolTag;
    public bool requireTwoHands;

    private XRGrabInteractable grabInteractable;
    private MeshRenderer[] meshRenderers;
    private Color originalColor;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();

        if (toolTag != null)
        {
            name = toolTag.toolName;
        }

        if (requireTwoHands && grabInteractable != null)
        {
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        }

        if (meshRenderers.Length > 0)
        {
            originalColor = meshRenderers[0].material.color;
        }
    }

    void Start()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        Highlight(true);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        Highlight(false);
    }

    void Highlight(bool active)
    {
        foreach (var renderer in meshRenderers)
        {
            if (active)
            {
                var color = Color.Lerp(originalColor, Color.cyan, 0.3f);
                foreach (var mat in renderer.materials)
                    mat.color = color;
            }
            else
            {
                foreach (var mat in renderer.materials)
                    mat.color = originalColor;
            }
        }
    }

    public string GetToolId()
    {
        return toolTag != null ? toolTag.toolId : name;
    }
}
