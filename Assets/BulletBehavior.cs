using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehavior : MonoBehaviour
{
    public GameObject impactPrefab; // optional hit effect
    public float lifetime = 5f;
    public float damage = 10f; // if you have HP system

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // apply damage if target has health component (pseudo)
        // var hp = collision.gameObject.GetComponent<Health>();
        // if (hp != null) hp.ApplyDamage(damage);

        if (impactPrefab)
        {
            Instantiate(impactPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
        }

        // optionally stick bullet for decals or destroy immediately
        Destroy(gameObject);
    }
}
