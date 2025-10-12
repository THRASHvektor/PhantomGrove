using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ball target logic.
/// </summary>
public class TargetBall : MonoBehaviour
{
    /// <summary>
    /// Display color when ball hitted.
    /// </summary>
    public Color hitColor = Color.red;
    /// <summary>
    /// Time for respawn.
    /// </summary>
    public float respawnDelay = 2f;
    /// <summary>
    /// Time for ball hitted color display.
    /// </summary>
    public float hitColorShowTime = 0.15f;

    private Renderer[] renderers;
    private Collider[] colliders;
    /// <summary>
    /// Store the origin color;
    /// </summary>
    private Color[] originalColors;
    /// <summary>
    /// Status for ball hitted.
    /// </summary>
    private bool isHit = false;
    /// <summary>
    /// Position and Rotation at the start;
    /// </summary>
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        // cache initial transform
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        // capture original material colors (material instances will be created at runtime)
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            // Accessing material creates an instance for this renderer at runtime.
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }
    }

    /// <summary>
    ///  Initializer if spawner wants to set initial transform.
    /// </summary>
    public void Initialize(Vector3 pos, Quaternion rot)
    {
        initialPosition = pos;
        initialRotation = rot;
        transform.position = pos;
        transform.rotation = rot;
    }

    // Accept both collision and trigger hits (handles various bullet setups)
    void OnCollisionEnter(Collision collision)
    {
        HandleHitBy(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHitBy(other.gameObject);
    }

    private void HandleHitBy(GameObject other)
    {
        if (isHit) return;

        // Detect bullet by tag or by having a BulletBehavior component
        if (other.CompareTag("Bullet") || other.GetComponent<BulletBehavior>() != null)
        {
            StartCoroutine(HandleHitCoroutine());
        }
    }

    private IEnumerator HandleHitCoroutine()
    {
        isHit = true;

        // 1) change color immediately
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }

        // 2) optionally show hit color shortly for visual feedback
        if (hitColorShowTime > 0f)
            yield return new WaitForSeconds(hitColorShowTime);

        // 3) hide visual and disable collision (but keep this script active to run coroutine)
        foreach (var r in renderers) if (r != null) r.enabled = false;
        foreach (var c in colliders) if (c != null) c.enabled = false;

        // 4) wait respawn delay
        yield return new WaitForSeconds(respawnDelay);

        // 5) reset state: restore colors, re-enable visuals and collisions, reset transform if needed
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            if (renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
            renderers[i].enabled = true;
        }
        foreach (var c in colliders) if (c != null) c.enabled = true;

        // optional: reset position/rotation
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        isHit = false;
    }


}
