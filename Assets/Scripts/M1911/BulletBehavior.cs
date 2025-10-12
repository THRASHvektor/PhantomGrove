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

    public float lifetime = 5f;
    public float damage = 10f; 
    /// <summary>
    /// Bullet belongins.
    /// </summary>
    public M1911 shooter;


    void OnCollisionEnter(Collision collision)
    {
        Destroy(gameObject);
    }
}
