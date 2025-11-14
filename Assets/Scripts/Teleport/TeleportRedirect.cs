using System.Collections;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// Attach this to a TeleportPoint (or the same GameObject) to redirect the player
/// to a different Transform after they teleport to this TeleportPoint.
/// It listens to Teleport.Player (sent after the SteamVR teleport completes) and
/// performs an additional move to the `redirectTarget`.
/// </summary>
[RequireComponent(typeof(TeleportMarkerBase))]
public class TeleportRedirect : MonoBehaviour
{
    [Tooltip("The transform the player should be moved to after teleporting to this point")]
    public Transform redirectTarget;

    [Tooltip("If true, apply a short fade when performing the redirect to hide the instantaneous move.")]
    public bool useFade = true;

    [Tooltip("Fade duration (seconds) used for the redirect move")]
    public float fadeTime = 0.08f;

    [Tooltip("Optionally lock this teleport point when used")]
    public bool lockOnUse = false;

    TeleportMarkerBase marker;
    bool redirected = false;
    [Tooltip("Proximity fallback: if Teleport.Player event is not received, when the player's tracking origin is within this distance of the marker we will trigger the redirect.")]
    public float proximityFallbackDistance = 0.6f;

    void Awake()
    {
        marker = GetComponent<TeleportMarkerBase>();
    }

    void OnEnable()
    {
        Teleport.Player.Listen(OnPlayerTeleported);
    }

    void OnDisable()
    {
        Teleport.Player.Remove(OnPlayerTeleported);
    }

    void OnPlayerTeleported(TeleportMarkerBase teleportedMarker)
    {
        Debug.Log($"TeleportRedirect: OnPlayerTeleported called on '{name}', teleportedMarker='{(teleportedMarker?teleportedMarker.name:"null")}'");
        if (teleportedMarker != marker) return;

        if (redirectTarget == null)
        {
            Debug.LogWarning("TeleportRedirect: redirectTarget is not set on " + name);
            return;
        }

        if (lockOnUse)
        {
            marker.SetLocked(true);
        }

        StartCoroutine(RedirectCoroutine());
    }

    void Update()
    {
        if (redirected) return;

        var player = Player.instance ?? FindObjectOfType<Player>();
        if (player == null) return;

        float dist = Vector3.Distance(player.trackingOriginTransform.position, marker.transform.position);
        if (dist <= proximityFallbackDistance)
        {
            Debug.Log($"TeleportRedirect: proximity fallback triggered (dist={dist}) on '{name}'");
            redirected = true;
            if (lockOnUse)
                marker.SetLocked(true);
            StartCoroutine(RedirectCoroutine());
        }
    }

    IEnumerator RedirectCoroutine()
    {
        var player = Player.instance ?? FindObjectOfType<Player>();
        if (player == null) yield break;

        Debug.Log($"TeleportRedirect: starting redirect coroutine to '{redirectTarget.name}' (useFade={useFade})");
        if (useFade)
        {
            SteamVR_Fade.Start(Color.clear, 0f);
            SteamVR_Fade.Start(Color.black, fadeTime);
            yield return new WaitForSeconds(fadeTime);
        }

        // Compute feet offset same way Teleport does
        Vector3 playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
        player.trackingOriginTransform.position = redirectTarget.position + playerFeetOffset;
        Debug.Log($"TeleportRedirect: player moved to {player.trackingOriginTransform.position}");

        // Reset attached object transforms so held items don't jump
        if (player.leftHand != null && player.leftHand.currentAttachedObjectInfo.HasValue)
            player.leftHand.ResetAttachedTransform(player.leftHand.currentAttachedObjectInfo.Value);
        if (player.rightHand != null && player.rightHand.currentAttachedObjectInfo.HasValue)
            player.rightHand.ResetAttachedTransform(player.rightHand.currentAttachedObjectInfo.Value);

        if (useFade)
        {
            SteamVR_Fade.Start(Color.clear, fadeTime);
        }

        yield break;
    }
}
