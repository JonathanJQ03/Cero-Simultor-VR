using UnityEngine;
using UnityEngine.InputSystem;

/// Rastrea la posicion y rotacion del controlador XR via InputSystem.
/// Solo aplica la pose cuando el dispositivo esta siendo rastreado activamente.
/// Con XR Device Simulator: permanece en posicion de escena hasta que el usuario
/// selecciona el controlador (tecla 1 o 2) y lo mueve.
public class XRControllerPoseTracker : MonoBehaviour
{
    [SerializeField] public bool isLeftHand = true;

    InputActionReference _positionAction;
    InputActionReference _rotationAction;
    InputActionReference _isTrackedAction;

    public void SetPoseActions(InputActionReference pos, InputActionReference rot, InputActionReference isTracked)
    {
        _positionAction  = pos;
        _rotationAction  = rot;
        _isTrackedAction = isTracked;

        _positionAction?.action.Enable();
        _rotationAction?.action.Enable();
        _isTrackedAction?.action.Enable();
    }

    void Update()
    {
        if (_isTrackedAction?.action == null) return;

        bool tracked = _isTrackedAction.action.ReadValue<float>() > 0.5f;
        if (!tracked) return;

        if (_positionAction?.action != null)
        {
            Vector3 pos = _positionAction.action.ReadValue<Vector3>();
            if (pos != Vector3.zero)
                transform.localPosition = pos;
        }

        if (_rotationAction?.action != null)
        {
            Quaternion rot = _rotationAction.action.ReadValue<Quaternion>();
            if (rot != Quaternion.identity)
                transform.localRotation = rot;
        }
    }
}
