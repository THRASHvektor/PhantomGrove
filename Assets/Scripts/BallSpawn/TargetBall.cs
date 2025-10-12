using System.Collections;
using UnityEngine;

/// <summary>
/// Ball behaviour for being hit by bullets.
/// On hit: briefly change color for visual feedback, notify spawner, then destroy.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TargetBall : MonoBehaviour
{
    [Tooltip("Color to flash when hit.")]
    public Color hitColor = Color.red;

    [Tooltip("Time (seconds) to show the hit color before destroying (small value, e.g. 0.1).")]
    public float hitColorShowTime = 0.12f;

    [Tooltip("Optional impact prefab to spawn on hit.")]
    public GameObject impactPrefab;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHit = false;

    private BallSpawner spawner;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }
    }

    /// <summary>
    /// Called by spawner immediately after instantiation to set up callback.
    /// </summary>
    public void SetSpawner(BallSpawner s)
    {
        spawner = s;
    }

    // Accept both collision and trigger depending on bullet setup
    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject, collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector3?)null);
    }

    void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject, null);
    }

    private void HandleHit(GameObject other, Vector3? hitPoint)
    {
        if (isHit) return;

        // detect bullet either by BulletBehavior component or tag
        BulletBehavior bb = other.GetComponent<BulletBehavior>();
        if (bb == null && other.CompareTag("Bullet"))
        {
            // no BulletBehavior but tag found - proceed
            StartCoroutine(HitAndDestroy(hitPoint, null));
            return;
        }
        else if (bb != null)
        { 
            StartCoroutine(HitAndDestroy(hitPoint, bb));
        }
    }

    private IEnumerator HitAndDestroy(Vector3? hitPoint, BulletBehavior bb)
    {
        isHit = true;

        // spawn impact effect if any
        if (impactPrefab != null && hitPoint.HasValue)
        {
            Instantiate(impactPrefab, hitPoint.Value, Quaternion.identity);
        }

        // flash color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }

        if (hitColorShowTime > 0f)
            yield return new WaitForSeconds(hitColorShowTime);

        // notify spawner BEFORE destroying (so spawner can decrement/track)
        if (spawner != null)
            spawner.NotifyBallDestroyed(this);

        // destroy this ball
        Destroy(gameObject);
    }
}