using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SimpleGrabber : MonoBehaviour
{
    public Transform attachmentPoint; // 手上的对齐点（在手心）
    public bool snapOnAttach = true;

    public SteamVR_Input_Sources handSource = SteamVR_Input_Sources.LeftHand;
    public SteamVR_Action_Boolean grabGripAction;
    public SteamVR_Action_Boolean grabPinchAction;
    public bool useGrabGrip = true;
    public bool useGrabPinch = false;

    private readonly List<Interactable> candidates = new List<Interactable>();
    public Interactable attached { get; private set; }
    private Rigidbody attachedRb;
    private Transform prevParent;

    void Awake()
    {
        if (grabGripAction == null) grabGripAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabGrip");
        if (grabPinchAction == null) grabPinchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        if (!attachmentPoint) attachmentPoint = transform;

        var rb = GetComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
        var col = GetComponent<Collider>(); col.isTrigger = true;
    }

    void Update()
    {
        bool press =
            (useGrabGrip && grabGripAction != null && grabGripAction.GetStateDown(handSource)) ||
            (useGrabPinch && grabPinchAction != null && grabPinchAction.GetStateDown(handSource));

        bool release =
            (useGrabGrip && grabGripAction != null && grabGripAction.GetStateUp(handSource)) ||
            (useGrabPinch && grabPinchAction != null && grabPinchAction.GetStateUp(handSource));

        if (press) TryAttachNearest();
        if (release) Detach();
    }

    private void TryAttachNearest()
    {
        if (attached != null) return;

        Interactable best = null; float bestDist = float.MaxValue;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            var it = candidates[i];
            if (!it) { candidates.RemoveAt(i); continue; }
            float d = Vector3.Distance(attachmentPoint.position, it.transform.position);
            if (d < bestDist) { bestDist = d; best = it; }
        }
        if (!best) return;

        attached = best;
        prevParent = best.transform.parent;
        attachedRb = best.GetComponent<Rigidbody>();
        if (attachedRb)
        {
            attachedRb.isKinematic = true;
            attachedRb.useGravity = false;
            attachedRb.velocity = Vector3.zero;
            attachedRb.angularVelocity = Vector3.zero;
        }

        // 如果枪上有 GripPoint，用精确对齐公式
        var anchor = best.GetComponent<GrabbableAnchor>();
        if (snapOnAttach && anchor != null && anchor.gripPoint != null)
        {
            // 公式：让枪的 GripPoint 与手的 attachmentPoint 重合且同向
            // R_new = A_rot * inverse(P_localRot)
            // T_new = A_pos - R_new * P_localPos
            Quaternion Rnew = attachmentPoint.rotation * Quaternion.Inverse(anchor.gripPoint.localRotation);
            Vector3 Tnew = attachmentPoint.position - (Rnew * anchor.gripPoint.localPosition);

            best.transform.SetPositionAndRotation(Tnew, Rnew);
            // 父子关系保持世界姿态不变
            best.transform.SetParent(attachmentPoint, worldPositionStays: true);
        }
        else if (snapOnAttach)
        {
            // 简易对齐：直接零对齐
            best.transform.SetParent(attachmentPoint, worldPositionStays: false);
            best.transform.localPosition = Vector3.zero;
            best.transform.localRotation = Quaternion.identity;
        }
        else
        {
            best.transform.SetParent(attachmentPoint, true);
        }
    }

    private void Detach()
    {
        if (!attached) return;

        attached.transform.SetParent(prevParent, true);

        if (attachedRb)
        {
            attachedRb.isKinematic = false;
            attachedRb.useGravity = true;
        }

        attached = null;
        attachedRb = null;
        prevParent = null;
    }

    void OnTriggerEnter(Collider other)
    {
        var it = other.GetComponentInParent<Interactable>();
        if (it && !candidates.Contains(it)) candidates.Add(it);
    }

    void OnTriggerExit(Collider other)
    {
        var it = other.GetComponentInParent<Interactable>();
        if (it) candidates.Remove(it);
    }
}