using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
/***
 * This MonoBehaviour is an example of what should be placed on objects that you would like to use force feedback with.
 * When activating force feedback, you are able to specify the amount the fingers should curl (0-1000) when activated.
 * Typically, it's best to have the force feedback activate when hovering over objects, to give the physical system time to move into position before a user tries to grab the object.
 * Once the hand hovering ends, you can relax the force feedback system by calling .Relax()
 */
public class ExampleAttach : MonoBehaviour
{
    private Interactable interactable;

    private FFBManager _ffbManager;

    private void Awake()
    {
        _ffbManager = FindObjectOfType<FFBManager>();
    }

    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    private void OnHandHoverBegin(Hand hand)
    {
        //Get the hand which is hovering
        ETrackedControllerRole handedness = hand.handType == SteamVR_Input_Sources.RightHand
            ? ETrackedControllerRole.RightHand
            : ETrackedControllerRole.LeftHand;
        
        hand.ShowGrabHint();
        
        //Trigger the force feedback
        _ffbManager.TriggerForceFeedback(new VRFFBInput(500, 500, 500, 500, 500, handedness));
    }

    private void OnHandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
        
        //Relax the force feedback when the hand moves away
        _ffbManager.Relax();
    }

    private void HandHoverUpdate(Hand hand)
    {
        GrabTypes grabType = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(gameObject);

        if (interactable.attachedToHand == null && grabType != GrabTypes.None)
        {
            hand.AttachObject(gameObject, grabType);
            hand.HoverLock(interactable);
        }
        else if(isGrabEnding)
        {
            hand.DetachObject(gameObject);
            hand.HoverUnlock(interactable);
        }
        
    }
}
