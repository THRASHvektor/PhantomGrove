using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contain bullet info.
/// Including its shooter, damage life time...
/// </summary>
public class BulletBehavior : MonoBehaviour
{
    /// <summary>
    /// Hit effects.
    /// </summary>
    public GameObject impactPrefab;
    /// <summary>
    /// Control bullet layer.
    /// </summary>
    public LayerMask hittableLayers;

    public float lifetime = 5f;
    public float damage = 10f; 
    /// <summary>
    /// Bullet belongins.
    /// </summary>
    public GameObject shooter;


    void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider, collision.contacts.Length > 0 ? collision.contacts[0].point : (Vector3?)null);
    }

    private void HandleHit(Collider other, Vector3? hitPoint)
    {
        
        if (shooter != null && other.transform.IsChildOf(shooter.transform)) return;

        
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0)
        {
            Destroy(gameObject);
            return;
        }

    }
}
