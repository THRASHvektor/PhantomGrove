using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// M1911 behavior.
/// Including Fireing, Bullet Shell Casting, Muzzle Flash Animation, Fire Rate Limit.
/// todo: Double Shot Testing.
/// </summary>
public class M1911 : MonoBehaviour
{
    /// <summary>
    /// SteamVR_Action_Boolean for trigger firing.
    /// </summary>
    public SteamVR_Action_Boolean fireAction;
    /// <summary>
    /// Muzzle flash GameObject (ParticleSystem).
    /// </summary>
    public GameObject muzzleFlash;
    /// <summary>
    /// Rigidbody Bullet Instance.
    /// 
    /// </summary>
    public GameObject bullet;
    /// <summary>
    /// seconds before spawned bullet is destroyed if no target hit.
    /// </summary>
    public float bulletLifetime = 5f;
    /// <summary>
    /// Transform representing the barrel pivot / muzzle position and rotation.
    /// </summary>
    public Transform barrelPivot;
    /// <summary>
    /// Controll velocity of the bullet (Default 300f in reality).
    /// </summary>
    public float shootingSpeed = 300f;
    
    [Header("Casing")]
    /// <summary>
    /// Casing prefab instantiated when firing.
    /// </summary>
    public GameObject casingPrefab;
    /// <summary>
    /// Transform from which casings are ejected.
    /// </summary>
    public Transform casingExitLocation;

    [Tooltip("Casing Destroy Timer")]
    /// <summary>
    /// Time in seconds before spawned casing is destroyed.
    /// </summary>
    public float casingDestroyTimer = 2f;
    

    [Tooltip("Eject Power")]
    /// <summary>
    /// Force applied to ejected casings (default 150f).
    /// 
    /// </summary>
    public float ejectPower = 150f;

    [Header("Fire Rate")]
    /// <summary>
    /// Fire rate in rounds per second.
    /// </summary>
    public float roundsPerSecond = 5f;
    /// <summary>
    /// // Tracks the next allowed fire time per input source
    /// </summary>
    private Dictionary<SteamVR_Input_Sources, float> nextAllowedFireTime = new Dictionary<SteamVR_Input_Sources, float>();

    [Header("DoubleShot")]
    /// <summary>
    /// Probability (0..1) that a fired shot will trigger a second shot shortly after (default 0.05 = 5%).
    /// </summary>
    [Range(0f, 1f)]
    public float doubleShotChance = 0.05f;
    /// <summary>
    /// Delay in seconds between the first and second bullet when double-shot triggers.
    /// </summary>
    public float doubleShotDelay = 0.1f; 

    private Interactable interactable;
    private Animator animator;
    private Rigidbody rb;

    private RigidbodyConstraints originalConstraints;
    private bool originalUseGravity;
    private bool savedConstraints = false;
    private Dictionary<Collider, bool> originalIsTrigger = new Dictionary<Collider, bool>();


    /// <summary>
    /// Initialize references and internal timers.
    /// </summary>
    void Start()
    {
        // The animator is in the model, must get from its child.
        animator = GetComponentInChildren<Animator>(true);
        interactable = GetComponent<Interactable>();
        nextAllowedFireTime[SteamVR_Input_Sources.Any] = 0f;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

    }

    /// <summary>
    /// Per-frame update: checks for attached hand and fire input, then enforces fire rate.
    /// </summary>
    void Update()
    {
        if (interactable != null && interactable.attachedToHand != null)
        {
            var source = interactable.attachedToHand.handType;
            if (fireAction != null && fireAction[source].stateDown)
            {
                float now = Time.time;
                float nextAllowed = GetNextAllowedForSource(source);
                if (now >= nextAllowed)
                {
                    Fire();

                    // Double shot decision.
                    if (Random.value <= doubleShotChance)
                    {
                        //  Schedule second shot after delay, but don't allow second shot to bypass cooldown:
                        StartCoroutine(DoDoubleShotAfterDelay(source, doubleShotDelay));
                    }

                    SetNextAllowedForSource(source, now + (1f / Mathf.Max(0.0001f, roundsPerSecond)));
                }
                    
            }
        }
    }

    /// <summary>
    /// Perform a single fire action: instantiate bullet, play muzzle flash and play fire animation.
    /// </summary>
    void Fire()
    {

        GameObject bulletInstance = Instantiate(bullet, barrelPivot.position, barrelPivot.rotation);

        bulletInstance.layer = LayerMask.NameToLayer("Bullet");

        // Get Compoment Rigidbody.
        Rigidbody bulletrb = bulletInstance.GetComponent<Rigidbody>();
        if (bulletrb == null)
        {
            Debug.LogWarning("Bullet prefab has no Rigidbody. Destroying instance.");
            Destroy(bulletInstance, 1f);
            return;
        }
        // Init Bullet Rigibody value.
        bulletrb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bulletrb.velocity = barrelPivot.forward * shootingSpeed;
        //Get Compoment BulletBehavior.
        var bb = bulletInstance.GetComponent<BulletBehavior>();
        if (bb != null)
        {
            // Bind current gun to this bullet for further use, e.g. fire rate change.
            bb.shooter = this.gameObject;
            bb.hittableLayers = LayerMask.GetMask("Enemy", "Default","World"); // 示例：击中敌人和地面会触发销毁
        }
        Destroy(bulletInstance, bulletLifetime);

        //new muzzle flash function
        if (muzzleFlash)
        {
            GameObject tempFlash;
            tempFlash = Instantiate(muzzleFlash, barrelPivot.position, barrelPivot.rotation);

            //Destroy the muzzle flash effect
            Destroy(tempFlash, 2f);
        }


        // Play M1911 shooting amination.
        if (animator != null)
        {
            animator.Play("Fire", 0, 0f);
        }
    }

    /// <summary>
    /// Instantiate and eject a casing from the casing exit transform.
    /// </summary>
    public void CasingRelease()
    {
        if (!casingExitLocation || !casingPrefab)
        {
            return;
        }

        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);

        var rb = tempCasing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            
            rb.AddExplosionForce(Random.Range(ejectPower * 0.7f, ejectPower),
                                 (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f),
                                 1f);

            rb.AddTorque(new Vector3(0,Random.Range(100f, 500f),Random.Range(100f, 1000f)),ForceMode.Impulse);
        }

        Destroy(tempCasing, casingDestroyTimer);
    }

    /// <summary>
    /// Get the next allowed fire time for a given SteamVR input source.
    /// </summary>
    /// <param name="source">Input source to query (LeftHand, RightHand, Any).</param>
    /// <returns>(Time.time) Timestamp when next shot is allowed for that source.</returns>
    private float GetNextAllowedForSource(SteamVR_Input_Sources source)
    {
        if (nextAllowedFireTime.TryGetValue(source, out var t))
        {
            return t;
        }
        return 0f;
    }

    /// <summary>
    /// Set the next time this input source is allowed to fire.
    /// </summary>
    /// <param name="source">Input source to set the timer for.</param>
    /// <param name="time">Time (Time.time) when next shot is allowed.</param>
    private void SetNextAllowedForSource(SteamVR_Input_Sources source, float time)
    {
        nextAllowedFireTime[source] = time;
    }

    /// <summary>
    /// Coroutine used to fire the second shot for the double-shot mechanic after a small delay.
    /// </summary>
    /// <param name="source">Input source that triggered the original shot.</param>
    /// <param name="delay">Delay in seconds before firing the second shot.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator DoDoubleShotAfterDelay(SteamVR_Input_Sources source, float delay)
    {
        if (delay > 0f) {
            yield return new WaitForSeconds(delay);
        }

        // Before firing second shot, ensure we still want it (optionally check if the gun is still held)
        if (interactable == null || interactable.attachedToHand == null) {
            yield break;
        }
        if (interactable.attachedToHand.handType != source) {
            yield break;
        }

        // Optional: ensure the cooldown still allows it; here we treat the double shot as part of same firing event,
        // so we do NOT check nextAllowedFireTime again. If you prefer to enforce cooldown, uncomment the check below.
        // float now = Time.time;
        // if (now < GetNextAllowedForSource(source)) yield break;

        Fire();
        yield break;
    }

    /// <summary>
    /// Set the double-shot probability (0..1).
    /// </summary>
    /// <param name="chance">Probability between 0 and 1 (inclusive).</param>
    public void SetDoubleShotChance(float chance)
    {
        doubleShotChance = Mathf.Clamp01(chance);
    }

    /// <summary>
    /// Get the current double-shot probability (0..1).
    /// </summary>
    /// <returns>Current doubleShotChance.</returns>
    public float GetDoubleShotChance()
    {
        return doubleShotChance;
    }

    /// <summary>
    /// Set M1911 Bulllet speed.
    /// </summary>
    /// <param name="speed">default speed is 300f</param>
    public void SetBulletSpeed(float speed = 300f)
    {
        shootingSpeed = speed;
    }
    /// <summary>
    /// Get the current Bullet speed.
    /// </summary>
    /// <returns>Current bullet speed</returns>
    public float GetBulletSpeed()
    {
        return shootingSpeed;
    }

    public void IncreaseBulletSpeedByPercentage(float percentage)
    {
        SetBulletSpeed(GetBulletSpeed() * (1f + percentage));
    }

    /// <summary>
    /// Set Fire Rate.
    /// </summary>
    /// <param name="firerate"></param>
    public void SetFireRate(float firerate)
    {
        roundsPerSecond = firerate;
    }
    /// <summary>
    /// Get current Fire Rate.
    /// </summary>
    /// <returns></returns>
    public float GetFireRate()
    {
        return roundsPerSecond;
    }
    /// <summary>
    /// Increase fire rate.
    /// </summary>
    /// <param name="percentage"> Range in 0f - 1f</param>
    public void IncreaseFireRateByPercentage(float percentage)
    {
        SetFireRate(GetFireRate() * (1f + percentage));
    }

    // Called by SteamVR
    //public void OnAttachedToHand(Valve.VR.InteractionSystem.Hand hand)
    //{
    //    // ensure rb cached
    //    if (rb == null) rb = GetComponent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();
    //    // parent to hand so transform follows exactly
    //    transform.SetParent(hand.transform, true);

    //    if (rb != null)
    //    {
    //        // keep non-kinematic so SteamVR can write velocity
    //        rb.isKinematic = false;

    //        // save and freeze to prevent physics pushing while held
    //        originalConstraints = rb.constraints;
    //        originalUseGravity = rb.useGravity;
    //        rb.useGravity = false;
    //        rb.constraints = RigidbodyConstraints.FreezeAll;
    //        savedConstraints = true;

    //        // optional: make colliders triggers (avoid contact) and save original states
    //        originalIsTrigger.Clear();
    //        foreach (var c in GetComponentsInChildren<Collider>(true))
    //        {
    //            if (c == null) continue;
    //            originalIsTrigger[c] = c.isTrigger;
    //            c.isTrigger = true;
    //        }
    //    }

    //    //// optional: small positional offset if your model needs alignment
    //    //transform.localPosition = Vector3.zero;
    //    //transform.localRotation = Quaternion.identity;
    //}

    //public void OnDetachedFromHand(Valve.VR.InteractionSystem.Hand hand)
    //{
    //    // unparent (keep world position)
    //    //transform.SetParent(null, true);

    //    if (rb != null && savedConstraints)
    //    {
    //        // restore constraints/gravity so physics can act again
    //        rb.useGravity = originalUseGravity;
    //        rb.constraints = originalConstraints;
    //        // keep rb.isKinematic = false so SteamVR's final velocity application works
    //    }

    //    // restore collider trigger flags
    //    foreach (var kv in originalIsTrigger)
    //    {
    //        if (kv.Key == null) continue;
    //        kv.Key.isTrigger = kv.Value;
    //    }
    //    originalIsTrigger.Clear();
    //    savedConstraints = false;
    //}
}
