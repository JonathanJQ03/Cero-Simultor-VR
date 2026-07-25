using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class HumanHandAnimator : MonoBehaviour
{
    [SerializeField] private InputActionReference controllerActionGrip;
    [SerializeField] private InputActionReference controllerActionTrigger;
    [SerializeField] private InputActionReference controllerActionPrimary;


    private Animator handAnimator = null;

    private readonly List<Finger> grippingFingers = new List<Finger>()
    {
        new Finger(FingerType.Middle),
        new Finger(FingerType.Ring),
        new Finger(FingerType.Pinky)
    };

    private readonly List<Finger> pointingFingers = new List<Finger>()
    {
        new Finger(FingerType.Index)
    };

    private readonly List<Finger> uiFingers = new List<Finger>()
    {
        new Finger(FingerType.Thumb),
        new Finger(FingerType.Middle),
        new Finger(FingerType.Ring),
        new Finger(FingerType.Pinky)
    };

    private readonly List<Finger> primaryFingers = new List<Finger>()
    {
        new Finger(FingerType.Thumb)
    };

    public void SetInputActions(InputActionReference grip, InputActionReference trigger, InputActionReference primary)
    {
        bool wasEnabled = isActiveAndEnabled;
        if (wasEnabled) UnsubscribeActions();

        controllerActionGrip    = grip;
        controllerActionTrigger = trigger;
        controllerActionPrimary = primary;

        if (wasEnabled) SubscribeActions();
    }

    private void OnEnable()  => SubscribeActions();
    private void OnDisable() => UnsubscribeActions();

    private void SubscribeActions()
    {
        if (controllerActionGrip != null)
        {
            controllerActionGrip.action.performed += GripActionPerformed;
            controllerActionGrip.action.canceled  += GripActionCanceled;
        }
        if (controllerActionTrigger != null)
        {
            controllerActionTrigger.action.performed += TriggerActionPerformed;
            controllerActionTrigger.action.canceled  += TriggerActionCanceled;
        }
        if (controllerActionPrimary != null)
        {
            controllerActionPrimary.action.performed += PrimaryActionPerformed;
            controllerActionPrimary.action.canceled  += PrimaryActionCanceled;
        }
    }

    private void UnsubscribeActions()
    {
        if (controllerActionGrip != null)
        {
            controllerActionGrip.action.performed -= GripActionPerformed;
            controllerActionGrip.action.canceled  -= GripActionCanceled;
        }
        if (controllerActionTrigger != null)
        {
            controllerActionTrigger.action.performed -= TriggerActionPerformed;
            controllerActionTrigger.action.canceled  -= TriggerActionCanceled;
        }
        if (controllerActionPrimary != null)
        {
            controllerActionPrimary.action.performed -= PrimaryActionPerformed;
            controllerActionPrimary.action.canceled  -= PrimaryActionCanceled;
        }
    }

    void Start()
    {
        this.handAnimator = GetComponent<Animator>();
    }

    private void GripActionPerformed(InputAction.CallbackContext obj)
    {
        SetFingerAnimationValues(grippingFingers, 1.0f);
        AnimateActionInput(grippingFingers);
    }
    private void TriggerActionPerformed(InputAction.CallbackContext obj)
    {
        SetFingerAnimationValues(pointingFingers, 1.0f);
        AnimateActionInput(pointingFingers);
    }
    private void PrimaryActionPerformed(InputAction.CallbackContext obj)
    {
        SetFingerAnimationValues(primaryFingers, 1.0f);
        AnimateActionInput(primaryFingers);
    }
    private void GripActionCanceled(InputAction.CallbackContext obj)
    {
        SetFingerAnimationValues(grippingFingers, 0.0f);
        AnimateActionInput(grippingFingers);
    }
    private void TriggerActionCanceled(InputAction.CallbackContext obj)
    {

        SetFingerAnimationValues(pointingFingers, 0.0f);
        AnimateActionInput(pointingFingers);
    }
    private void PrimaryActionCanceled(InputAction.CallbackContext obj)
    {
        SetFingerAnimationValues(primaryFingers, 0.0f);
        AnimateActionInput(primaryFingers);
    }

    private void AnimateActionInput(List<Finger> fingersToAnimate)
    {
        foreach (Finger finger in fingersToAnimate)
        {
            AnimateFinger(finger.type.ToString(), finger.target);
        }
    }
    private void AnimateFinger(string fingerName, float animationBlendValue)
    {
        handAnimator.SetFloat(fingerName, animationBlendValue);
    }

    private void SetFingerAnimationValues(List<Finger> fingersToAnimate, float targetValue)
    {
        foreach (Finger finger in fingersToAnimate)
        {
            finger.target = targetValue;
        }
    }
}
