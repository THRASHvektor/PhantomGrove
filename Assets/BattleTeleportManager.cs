using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

/// <summary>
/// Manager that locks all child TeleportPoints for a cooldown when any teleport is used.
/// Attach this to a parent GameObject that contains TeleportPoint children.
/// </summary>
public class BattleTeleportManager : MonoBehaviour
{
    [Tooltip("Cooldown duration in seconds during which all child teleport points are locked.")]
    public float cooldownDuration = 10f;

    // Current cooldown coroutine (if running)
    private Coroutine cooldownCoroutine = null;

    // Stores previous locked state for each marker so we can restore it after cooldown
    private Dictionary<TeleportMarkerBase, bool> previousLocked = new Dictionary<TeleportMarkerBase, bool>();

    void OnEnable()
    {
        // Listen for completed teleports
        Teleport.Player.Listen(OnPlayerTeleported);
    }

    void OnDisable()
    {
        Teleport.Player.Remove(OnPlayerTeleported);
    }

    private void OnPlayerTeleported(TeleportMarkerBase teleportedMarker)
    {
        if (teleportedMarker == null) return;

        // If a cooldown is already running, restart the timer but keep the original previousLocked states.
        if (cooldownCoroutine == null)
        {
            // Record previous locked states for all child markers
            previousLocked.Clear();
            var markers = GetComponentsInChildren<TeleportMarkerBase>(true);
            foreach (var m in markers)
            {
                if (m != null && !previousLocked.ContainsKey(m))
                {
                    previousLocked[m] = m.locked;
                }
            }
        }

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }

        cooldownCoroutine = StartCoroutine(CooldownCoroutine());
    }

    private IEnumerator CooldownCoroutine()
    {
        // Lock all child teleport markers
        var markers = GetComponentsInChildren<TeleportMarkerBase>(true);
        foreach (var m in markers)
        {
            if (m != null)
            {
                m.SetLocked(true);
            }
        }

        float startTime = Time.time;
        float endTime = startTime + Mathf.Max(0f, cooldownDuration);

        while (Time.time < endTime)
        {
            yield return null;
        }

        // Restore previous locked states (or unlock if marker wasn't tracked)
        markers = GetComponentsInChildren<TeleportMarkerBase>(true);
        foreach (var m in markers)
        {
            if (m == null) continue;

            bool prev = false;
            if (previousLocked.TryGetValue(m, out prev))
            {
                m.SetLocked(prev);
            }
            else
            {
                // If we didn't track it, just unlock
                m.SetLocked(false);
            }
        }

        previousLocked.Clear();
        cooldownCoroutine = null;
    }
}
