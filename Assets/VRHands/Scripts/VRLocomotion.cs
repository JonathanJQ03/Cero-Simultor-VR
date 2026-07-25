using UnityEngine;
using UnityEngine.InputSystem;

/// Locomotion VR: WASD para moverse relativo a la camara. Sin dependencia de
/// XRI action maps ni XR Device Simulator — lee teclado directamente.
[DefaultExecutionOrder(-90)]
public class VRLocomotion : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.0f;

    private Camera _cam;

    void Start() => _cam = Camera.main;

    void Update()
    {
        if (_cam == null) { _cam = Camera.main; return; }

        var kb = Keyboard.current;
        if (kb == null) return;

        float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        if (h == 0f && v == 0f) return;

        var fwd   = _cam.transform.forward; fwd.y   = 0f; if (fwd != Vector3.zero) fwd.Normalize();
        var right = _cam.transform.right;   right.y = 0f; if (right != Vector3.zero) right.Normalize();

        transform.position += (fwd * v + right * h) * (moveSpeed * Time.deltaTime);
    }
}
