using UnityEngine;
using UnityEngine.InputSystem;

/// Intercepts ALL position changes (XR Device Simulator, locomotion providers,
/// physical tracking drift) and re-routes them through the CharacterController
/// so walls and furniture block movement correctly.
[RequireComponent(typeof(CharacterController))]
public class VRPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float gravity   = -9.81f;

    [Header("Input – VR thumbstick (XRI Left Locomotion > Move)")]
    public InputActionReference moveActionRef;

    CharacterController _cc;
    Transform           _cameraTransform;
    float               _verticalVelocity;
    Vector3             _prevPosition;      // position as of end of last frame

    void Awake()
    {
        _cc              = GetComponent<CharacterController>();
        // Prefer the camera inside the XR rig (Camera Offset/Main Camera)
        _cameraTransform = GetComponentInChildren<Camera>()?.transform;
    }

    void Start()
    {
        _prevPosition = transform.position;
    }

    void OnEnable()  => moveActionRef?.action.Enable();
    void OnDisable() => moveActionRef?.action.Disable();

    void Update()
    {
        // ── 1. Capture and UNDO any external position change ─────────────────
        // The XR Device Simulator (and any other system) may have moved the
        // XR Origin directly via transform.position.  We capture that delta,
        // restore the previous position, and re-apply the delta through the
        // CharacterController so that wall colliders are respected.
        Vector3 externalDelta = transform.position - _prevPosition;
        transform.position    = _prevPosition;           // restore

        Vector3 externalH = new Vector3(externalDelta.x, 0f, externalDelta.z);

        // ── 2. VR thumbstick input ────────────────────────────────────────────
        Vector3 stickMove = Vector3.zero;
        if (moveActionRef?.action != null)
        {
            Vector2 stick = moveActionRef.action.ReadValue<Vector2>();
            if (stick.sqrMagnitude > 0.01f && _cameraTransform != null)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
                Vector3 rgt = Vector3.ProjectOnPlane(_cameraTransform.right,   Vector3.up).normalized;
                stickMove   = (fwd * stick.y + rgt * stick.x) * moveSpeed * Time.deltaTime;
            }
        }

        // ── 3. Keyboard fallback (only when the simulator isn't moving the body) ──
        // Avoids doubling movement when simulator + keyboard both active.
        Vector3 keyMove = Vector3.zero;
        if (externalH.sqrMagnitude < 0.00001f && Keyboard.current != null)
        {
            float h = (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed  ? 1f : 0f);
            float v = (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed   ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed  ? 1f : 0f);
            if (_cameraTransform != null && (h != 0f || v != 0f))
            {
                Vector3 fwd = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up).normalized;
                Vector3 rgt = Vector3.ProjectOnPlane(_cameraTransform.right,   Vector3.up).normalized;
                keyMove     = (fwd * v + rgt * h) * moveSpeed * Time.deltaTime;
            }
        }

        // ── 4. Gravity ────────────────────────────────────────────────────────
        if (_cc.isGrounded) _verticalVelocity = -0.5f;
        else                _verticalVelocity += gravity * Time.deltaTime;

        // ── 5. Apply ALL movement through CharacterController ─────────────────
        // externalH is already a world-space displacement (not velocity*dt)
        Vector3 totalMove = externalH + stickMove + keyMove;
        totalMove.y = _verticalVelocity * Time.deltaTime;

        _cc.Move(totalMove);                             // wall collision handled here

        _prevPosition = transform.position;
    }
}
