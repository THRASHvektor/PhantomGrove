using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Attach to a TeleportPoint (or TeleportMarkerBase) to automatically lock it after the player teleports to it.
/// Optionally unlock after a delay.
/// </summary>
[RequireComponent(typeof(TeleportMarkerBase))]
public class TeleportPointAutoLock : MonoBehaviour
{
    [Tooltip("If true the point will be locked immediately after the player teleports to it.")]
    public bool lockOnUse = true;

    [Tooltip("If > 0 the point will be automatically unlocked after this many seconds.")]
    public float autoUnlockDelay = 0f;

    TeleportMarkerBase marker;

    void Awake()
    {
        marker = GetComponent<TeleportMarkerBase>();
    }

    void OnEnable()
    {
        // Subscribe to Teleport.Player event
        Valve.VR.InteractionSystem.Teleport.Player.Listen(OnPlayerTeleported);
    }

    void OnDisable()
    {
        Valve.VR.InteractionSystem.Teleport.Player.Remove(OnPlayerTeleported);
    }

    void OnPlayerTeleported(Valve.VR.InteractionSystem.TeleportMarkerBase teleportedMarker)
    {
        if (!lockOnUse || marker == null || teleportedMarker == null) return;

        // If this marker is the one teleported to, lock it
        if (teleportedMarker == marker)
        {
            marker.SetLocked(true);

            if (autoUnlockDelay > 0f)
            {
                CancelInvoke(nameof(AutoUnlock));
                Invoke(nameof(AutoUnlock), autoUnlockDelay);
            }
        }
    }

    void AutoUnlock()
    {
        if (marker != null)
            marker.SetLocked(false);
    }
}
