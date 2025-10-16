using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class SimpleAttach : MonoBehaviour
{
    // Start is called before the first frame update

    private Interactable interactable;

    void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    // Update is called once per frame
    private void OnHandHoverBegin(Hand hand)
    {
        hand.ShowGrabHint();
    }

    private void OnHandHoverEnd(Hand hand)
    {
        hand.HideGrabHint();
    }

    private void HandHoverUpdate(Hand hand)
    {

        GrabTypes grabTypes = hand.GetGrabStarting();
        bool isGrabEnding = hand.IsGrabEnding(gameObject);

        if(interactable.attachedToHand == null && grabTypes != GrabTypes.None)
        {
            hand.AttachObject(gameObject, grabTypes);
            hand.HoverLock(interactable);
            hand.HideGrabHint();
        }
        else if (isGrabEnding)
        {
            hand.DetachObject(gameObject);
            hand.HoverUnlock(interactable);
        }
    }
}
