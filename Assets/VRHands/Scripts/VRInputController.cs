using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Control de entrada 100% teclado para el simulador VR:
///   WASD        → moverse (VRLocomotion)
///   Flechas     → girar camara (izq/der=yaw, arriba/abajo=pitch)
///   1 / Numpad1 → agarrar mano izquierda  (queda agarrado)
///   2 / Numpad2 → soltar  mano izquierda
///   4 / Numpad4 → agarrar mano derecha    (queda agarrado)
///   5 / Numpad5 → soltar  mano derecha
[DefaultExecutionOrder(-110)]
public class VRInputController : MonoBehaviour
{
    [SerializeField] private float turnSpeed = 80f;

    private Transform _cameraOffset;
    private float _yaw;
    private float _pitch;

    private NearFarInteractor _leftInteractor;
    private NearFarInteractor _rightInteractor;

    void Awake()
    {
        KillDeviceSimulator();
        _cameraOffset = transform.Find("Camera Offset");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void Start()
    {
        // Segunda pasada: si el simulador se reactivó solo, lo matamos de nuevo
        KillDeviceSimulator();

        _yaw = transform.eulerAngles.y;
        if (_cameraOffset != null)
        {
            float e = _cameraOffset.localEulerAngles.x;
            _pitch = e > 180f ? e - 360f : e;
        }

        // Buscar interactores — pueden no existir aun en Awake si son creados por VRHandsSetup
        FindInteractors();
    }

    void Update()
    {
        // Reintentar buscar interactores si aun no estan (carga desde otra escena)
        if (_leftInteractor == null || _rightInteractor == null)
            FindInteractors();

        HandleCameraRotation();
        HandleGrab();
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    void KillDeviceSimulator()
    {
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (mb == null) continue;
            var typeName = mb.GetType().Name;
            if (typeName == "XRDeviceSimulator" || typeName == "SimulatedDeviceLifecycleManager")
            {
                // Desactivar el GO completo (para ambos componentes a la vez)
                if (mb.gameObject.activeSelf)
                {
                    mb.gameObject.SetActive(false);
                    Debug.Log($"[VRInputController] Desactivado: {typeName}");
                }
                // Destruir para limpieza total
                Destroy(mb.gameObject);
                break; // ambos estan en el mismo GO
            }
        }
    }

    void FindInteractors()
    {
        var leftCtrl  = transform.Find("Camera Offset/LeftHand Controller");
        var rightCtrl = transform.Find("Camera Offset/RightHand Controller");
        if (leftCtrl  != null) _leftInteractor  = leftCtrl.GetComponent<NearFarInteractor>();
        if (rightCtrl != null) _rightInteractor = rightCtrl.GetComponent<NearFarInteractor>();
    }

    void HandleCameraRotation()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.isPressed)  _yaw   -= turnSpeed * Time.deltaTime;
        if (kb.rightArrowKey.isPressed) _yaw   += turnSpeed * Time.deltaTime;
        if (kb.upArrowKey.isPressed)    _pitch -= turnSpeed * Time.deltaTime;
        if (kb.downArrowKey.isPressed)  _pitch += turnSpeed * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        if (_cameraOffset != null)
            _cameraOffset.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void HandleGrab()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Mano izquierda: 1 o Numpad1 = agarrar, 2 o Numpad2 = soltar
        bool grabLeft    = kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame;
        bool releaseLeft = kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame;

        // Mano derecha: 4 o Numpad4 = agarrar, 5 o Numpad5 = soltar
        bool grabRight    = kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame;
        bool releaseRight = kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame;

        if (grabLeft)    TryGrab(_leftInteractor);
        if (releaseLeft) TryRelease(_leftInteractor);
        if (grabRight)   TryGrab(_rightInteractor);
        if (releaseRight) TryRelease(_rightInteractor);
    }

    void TryGrab(NearFarInteractor interactor)
    {
        if (interactor == null) return;
        if (interactor.hasSelection) return;

        IXRSelectInteractable target = null;
        foreach (var h in interactor.interactablesHovered)
        {
            if (h is IXRSelectInteractable ia) { target = ia; break; }
        }

        if (target != null)
            interactor.StartManualInteraction(target);
        else
            Debug.Log($"[VRInputController] {interactor.name}: no hay objeto en rango.");
    }

    void TryRelease(NearFarInteractor interactor)
    {
        if (interactor == null) return;
        if (interactor.hasSelection)
            interactor.EndManualInteraction();
    }
}
