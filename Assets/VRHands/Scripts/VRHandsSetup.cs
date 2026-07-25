using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// Configura manos VR en runtime:
///   - Posiciona controladores relativo a la camara cuando no hay tracking real
///   - Usa datos reales de tracking (XR hardware) cuando estan disponibles
///   - Asigna botones de agarre (Select/Activate) al NearFarInteractor
///   - Conecta HumanHandAnimator a botones de controlador
[DefaultExecutionOrder(-100)]
public class VRHandsSetup : MonoBehaviour
{
    [Header("Prefabs de manos")]
    [SerializeField] private GameObject leftHandPrefab;
    [SerializeField] private GameObject rightHandPrefab;
    [SerializeField] private InputActionAsset vrHandsInputActions;

    [Header("Offset manos relativo a camara (XR Device Simulator)")]
    [SerializeField] private Vector3 leftOffset  = new Vector3(-0.35f, -0.20f, 0.55f);
    [SerializeField] private Vector3 rightOffset = new Vector3( 0.35f, -0.20f, 0.55f);
    [SerializeField] private Vector3 leftRotEuler  = new Vector3(20f, -15f, 0f);
    [SerializeField] private Vector3 rightRotEuler = new Vector3(20f,  15f, 0f);

    private Transform _leftCtrl, _rightCtrl;
    private Camera _cam;
    private InputAction _leftPosAction, _rightPosAction;
    private InputAction _leftRotAction, _rightRotAction;

    // Nombres correctos en XRI Default Input Actions 3.x
    const string LeftPosActionPath  = "XRI Left/LeftHand Controller - TPD - Position";
    const string LeftRotActionPath  = "XRI Left/LeftHand Controller - TPD - Rotation";
    const string RightPosActionPath = "XRI Right/RightHand Controller - TPD - Position";
    const string RightRotActionPath = "XRI Right/RightHand Controller - TPD - Rotation";

    void Awake()
    {
        // Garantizar que existe un XRInteractionManager en la escena
        if (FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() == null)
        {
            var go = new GameObject("XR Interaction Manager");
            go.AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
            Debug.Log("[VRHandsSetup] XRInteractionManager creado automaticamente.");
        }

        _leftCtrl  = transform.Find("Camera Offset/LeftHand Controller");
        _rightCtrl = transform.Find("Camera Offset/RightHand Controller");

        if (_leftCtrl  == null) Debug.LogWarning("[VRHandsSetup] No se encontro LeftHand Controller");
        if (_rightCtrl == null) Debug.LogWarning("[VRHandsSetup] No se encontro RightHand Controller");

        InputActionAsset poseAsset = null;
        foreach (var a in Resources.FindObjectsOfTypeAll<InputActionAsset>())
            if (a.name == "XRI Default Input Actions") { poseAsset = a; break; }

        if (poseAsset != null)
        {
            // Habilitar mapas de interaccion (necesarios para Select/Activate)
            poseAsset.FindActionMap("XRI Left Interaction")?.Enable();
            poseAsset.FindActionMap("XRI Right Interaction")?.Enable();

            // Acciones de tracking de posicion/rotacion
            _leftPosAction  = poseAsset.FindAction(LeftPosActionPath);
            _leftRotAction  = poseAsset.FindAction(LeftRotActionPath);
            _rightPosAction = poseAsset.FindAction(RightPosActionPath);
            _rightRotAction = poseAsset.FindAction(RightRotActionPath);
            _leftPosAction?.Enable();
            _leftRotAction?.Enable();
            _rightPosAction?.Enable();
            _rightRotAction?.Enable();

            // Configurar acciones de agarre en NearFarInteractor
            ConfigureInteractorInputs(
                _leftCtrl?.gameObject,
                poseAsset.FindAction("XRI Left Interaction/Select"),
                poseAsset.FindAction("XRI Left Interaction/Activate"),
                poseAsset.FindAction("XRI Left Interaction/UI Press"));

            ConfigureInteractorInputs(
                _rightCtrl?.gameObject,
                poseAsset.FindAction("XRI Right Interaction/Select"),
                poseAsset.FindAction("XRI Right Interaction/Activate"),
                poseAsset.FindAction("XRI Right Interaction/UI Press"));
        }
        else
        {
            Debug.LogWarning("[VRHandsSetup] XRI Default Input Actions no encontrado.");
        }

        // Eliminar XRControllerPoseTracker si existe (reemplazado por este Update)
        RemoveOldTracker(_leftCtrl?.gameObject);
        RemoveOldTracker(_rightCtrl?.gameObject);

        SetupHandAnimator(_leftCtrl,  "VRHands Left",  leftHandPrefab,  "LeftHand");
        SetupHandAnimator(_rightCtrl, "VRHands Right", rightHandPrefab, "RightHand");
    }

    void Start()
    {
        _cam = Camera.main;

        // Asegurar registro de todos los interactores e interactuables con el manager
        var manager = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        if (manager != null)
        {
            foreach (var ir in FindObjectsOfType<NearFarInteractor>())
                manager.RegisterInteractor((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor)ir);

            foreach (var ia in FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>())
                manager.RegisterInteractable((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable)ia);
        }

        // Posicion inicial antes del primer Update
        ApplyCameraRelative(_leftCtrl,  leftOffset,  leftRotEuler);
        ApplyCameraRelative(_rightCtrl, rightOffset, rightRotEuler);
    }

    void Update()
    {
        if (_cam == null) return;

        // Si la accion devuelve posicion real (hardware XR activo), usar tracking real.
        // En XR Device Simulator sin controlador activado devuelve zero → modo camara.
        bool leftReal  = _leftPosAction  != null && _leftPosAction.ReadValue<Vector3>()  != Vector3.zero;
        bool rightReal = _rightPosAction != null && _rightPosAction.ReadValue<Vector3>() != Vector3.zero;

        if (leftReal)
            ApplyTrackedPose(_leftCtrl,  _leftPosAction,  _leftRotAction);
        else
            ApplyCameraRelative(_leftCtrl,  leftOffset,  leftRotEuler);

        if (rightReal)
            ApplyTrackedPose(_rightCtrl, _rightPosAction, _rightRotAction);
        else
            ApplyCameraRelative(_rightCtrl, rightOffset, rightRotEuler);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    void ApplyCameraRelative(Transform ctrl, Vector3 offset, Vector3 rotEuler)
    {
        if (ctrl == null || _cam == null) return;
        var t = _cam.transform;
        ctrl.position = t.position + t.right * offset.x + t.up * offset.y + t.forward * offset.z;
        ctrl.rotation = t.rotation * Quaternion.Euler(rotEuler);
    }

    void ApplyTrackedPose(Transform ctrl, InputAction posAction, InputAction rotAction)
    {
        if (ctrl == null) return;
        ctrl.localPosition = posAction.ReadValue<Vector3>();
        var rot = rotAction?.ReadValue<Quaternion>() ?? Quaternion.identity;
        if (rot != Quaternion.identity) ctrl.localRotation = rot;
    }

    // Asigna Select / Activate / UIPress al NearFarInteractor via reflexion
    static void ConfigureInteractorInputs(GameObject go,
        InputAction selectAction, InputAction activateAction, InputAction uiPressAction)
    {
        if (go == null) return;
        var interactor = go.GetComponent<NearFarInteractor>();
        if (interactor == null) return;

        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Buscar XRBaseInputInteractor en la jerarquia de herencia
        var baseType = interactor.GetType().BaseType;
        while (baseType != null && baseType.Name != "XRBaseInputInteractor")
            baseType = baseType.BaseType;

        if (baseType == null)
        {
            Debug.LogWarning("[VRHandsSetup] XRBaseInputInteractor no encontrado en jerarquia");
            return;
        }

        SetButtonReaderAction(baseType.GetField("m_SelectInput",   flags)?.GetValue(interactor), selectAction);
        SetButtonReaderAction(baseType.GetField("m_ActivateInput", flags)?.GetValue(interactor), activateAction);

        // UIPress esta en NearFarInteractor directamente
        var uiField = interactor.GetType().GetField("m_UIPressInput", flags);
        SetButtonReaderAction(uiField?.GetValue(interactor), uiPressAction);
    }

    static void SetButtonReaderAction(object buttonReader, InputAction action)
    {
        if (buttonReader == null || action == null) return;
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var t = buttonReader.GetType();
        t.GetField("m_InputSourceMode", flags)?.SetValue(buttonReader, 2); // 2 = InputActionReference
        var actionRef = InputActionReference.Create(action);
        t.GetField("m_InputActionReferencePerformed", flags)?.SetValue(buttonReader, actionRef);
    }

    void SetupHandAnimator(Transform ctrlTransform, string handsMapName, GameObject prefab, string handName)
    {
        if (ctrlTransform == null || prefab == null) return;

        Transform existingHand = ctrlTransform.Find(handName);
        HumanHandAnimator animator;
        if (existingHand != null)
        {
            animator = existingHand.GetComponent<HumanHandAnimator>();
        }
        else
        {
            var handGO = Instantiate(prefab, ctrlTransform);
            handGO.name = handName;
            handGO.transform.localPosition = Vector3.zero;
            handGO.transform.localRotation = Quaternion.identity;
            handGO.transform.localScale    = Vector3.one;
            animator = handGO.GetComponent<HumanHandAnimator>();
        }

        if (animator == null || vrHandsInputActions == null) return;
        vrHandsInputActions.Enable();
        animator.SetInputActions(
            ToRef(vrHandsInputActions, handsMapName + "/Grip"),
            ToRef(vrHandsInputActions, handsMapName + "/Trigger"),
            ToRef(vrHandsInputActions, handsMapName + "/Primary")
        );
    }

    static void RemoveOldTracker(GameObject go)
    {
        if (go == null) return;
        var old = go.GetComponent<XRControllerPoseTracker>();
        if (old != null) Destroy(old);
    }

    static InputActionReference ToRef(InputActionAsset asset, string path)
    {
        var action = asset?.FindAction(path);
        return action != null ? InputActionReference.Create(action) : null;
    }
}
