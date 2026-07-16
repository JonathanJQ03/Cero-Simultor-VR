using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class ToolInteractor : MonoBehaviour
{
    private XRSocketInteractor socketInteractor;

    void Start()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.AddListener(OnToolPlaced);
            socketInteractor.selectExited.AddListener(OnToolRemoved);
        }
    }

    void OnDestroy()
    {
        if (socketInteractor != null)
        {
            socketInteractor.selectEntered.RemoveListener(OnToolPlaced);
            socketInteractor.selectExited.RemoveListener(OnToolRemoved);
        }
    }

    void OnToolPlaced(SelectEnterEventArgs args)
    {
        var tool = args.interactableObject.transform.GetComponent<MedicalTool>();
        if (tool != null)
        {
            string toolId = tool.GetToolId();
            Debug.Log($"Herramienta colocada: {toolId}");

            PatientFSM fsm = PatientFSM.Instance;
            if (fsm != null && !fsm.IsFinished)
            {
                fsm.ProcessTool(toolId);
            }
        }
    }

    void OnToolRemoved(SelectExitEventArgs args)
    {
        Debug.Log("Herramienta retirada del socket");
    }

    public void UseToolOnPatient(string toolId)
    {
        PatientFSM fsm = PatientFSM.Instance;
        if (fsm != null && !fsm.IsFinished)
        {
            fsm.ProcessTool(toolId);
        }
    }
}
