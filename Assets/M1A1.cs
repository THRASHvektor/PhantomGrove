using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// M1A1 behavior (automatic weapon).
/// Based on M1911 but fires continuously while trigger is held.
/// Includes muzzle flash, casing ejection, double-shot chance and frost shots.
/// </summary>
public class M1A1 : MonoBehaviour
{
    public SteamVR_Action_Boolean fireAction;
    public GameObject muzzleFlash;
    public GameObject bullet;
    public float bulletLifetime = 5f;
    public Transform barrelPivot;
    public float shootingSpeed = 300f;
    public float bulletDamage = 8f;
    [Header("Casing")]
    public GameObject casingPrefab;
    public Transform casingExitLocation;
    [Tooltip("Casing Destroy Timer")]
    public float casingDestroyTimer = 2f;
    private bool originalIsKinematic;
    private RigidbodyInterpolation originalInterpolation;
    private bool originalDetectCollisions;
    [Tooltip("Eject Power")]
    public float ejectPower = 150f;

    [Header("Fire Rate")]
    // rounds per second
    public float roundsPerSecond = 15f; // default higher for SMG
    private Dictionary<SteamVR_Input_Sources, float> nextAllowedFireTime = new Dictionary<SteamVR_Input_Sources, float>();

    [Header("DoubleShot")]
    [Range(0f, 1f)]
    public float doubleShotChance = 0.02f;
    public float doubleShotDelay = 0.05f;

    [Header("FrostShot")]
    [Range(0f, 1f)]
    public float frostShotChance = 0.02f;
    public float frostTime = 2f;
    public float speedSlowRate = 0.1f;
    [Header("FireShot")]
    [Tooltip("Probability (0..1) that a fired shot will be a fire bullet.")]
    public float fireShotChance = 0f;
    public float fireDuration = 3f;
    public float fireDamagePerSecond = 1f;

    [Tooltip("Cooldown (seconds) after a fire bullet successfully applies burn on a target")]
    public float fireCooldownOnHit = 10f;
    


    public void IncreaseFireChanceByAbsolute(float amount)
    {
        fireShotChance = Mathf.Clamp01(fireShotChance + amount);
        Debug.Log($"[M1A1] Fire chance increased to {fireShotChance}");
    }
    [Header("Critical")]
    [Tooltip("Chance (0..1) for a bullet to be a critical hit. Cards can increase this.")]
    public float critChance = 0f;

    public void IncreaseCritChanceByAbsolute(float amount)
    {
        critChance = Mathf.Clamp01(critChance + amount);
        Debug.Log($"[M1A1] Crit chance increased to {critChance}");
    }
    /// <summary>
    /// Frost hit cooldown (seconds) to apply after a frost bullet actually hits a target.
    /// </summary>
    public float frostCooldownOnHit = 1f;

    private float _nextAllowedFrostTime = 0f;
    // Time until this weapon is allowed to create another fire bullet (set on fire HIT)
    private float _nextAllowedFireTime = 0f;

    private Interactable interactable;
    private Animator animator;
    private Rigidbody rb;

    private RigidbodyConstraints originalConstraints;
    private bool originalUseGravity;
    private Dictionary<Collider, bool> originalIsTrigger = new Dictionary<Collider, bool>();

    private bool savedConstraints;
    

    public GameObject initText;
    [Header("Audio")]
    public AudioSource audioSource;   // 枪上的 AudioSource
    public AudioClip fireClip;        // 开火音效
    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        interactable = GetComponent<Interactable>();
        nextAllowedFireTime[SteamVR_Input_Sources.Any] = 0f;
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (interactable != null && interactable.attachedToHand != null)
        {
            var source = interactable.attachedToHand.handType;
            //initText.SetActive(false);

            if (fireAction != null && fireAction[source].state)
            {
                float now = Time.time;
                float nextAllowed = GetNextAllowedForSource(source);
                if (now >= nextAllowed)
                {
                    Fire();

                    // Double shot decision (kept as in M1911)
                    if (Random.value <= doubleShotChance)
                    {
                        StartCoroutine(DoDoubleShotAfterDelay(source, doubleShotDelay));
                        //Debug.Log("M1A1 Double Shotted!");
                    }

                    SetNextAllowedForSource(source, now + (1f / Mathf.Max(0.0001f, roundsPerSecond)));
                }
            }
        }
    }

    void Fire()
    {
        GameObject bulletInstance = Instantiate(bullet, barrelPivot.position, barrelPivot.rotation);
        bulletInstance.layer = LayerMask.NameToLayer("Bullet");

        Rigidbody bulletrb = bulletInstance.GetComponent<Rigidbody>();
        if (bulletrb == null)
        {
            Debug.LogWarning("Bullet prefab has no Rigidbody. Destroying instance.");
            Destroy(bulletInstance, 1f);
            return;
        }
        bulletrb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        bulletrb.velocity = barrelPivot.forward * shootingSpeed;

        var bb = bulletInstance.GetComponent<BulletBehavior>();
        if (bb != null)
        {
            bb.shooter = this.gameObject;
            bb.hittableLayers = LayerMask.GetMask("Enemy", "Default", "World");
            bb.damage = bulletDamage;
            if (Random.value <= frostShotChance && Time.time >= _nextAllowedFrostTime)
            {
                bb.isFrostBullet = true;
                bb.InitFrostBullet(frostTime, speedSlowRate);
                Debug.Log("M1A1 Frost Bullet Shotted!");
            }
            else if (Random.value <= frostShotChance && Time.time < _nextAllowedFrostTime)
            {
                Debug.Log("M1A1 frost suppressed due to weapon frost cooldown.");
            }
                // Fire roll
                if (Random.value <= fireShotChance && Time.time >= _nextAllowedFireTime)
                {
                    bb.isFireBullet = true;
                    bb.InitFireBullet(fireDuration, fireDamagePerSecond);
                    Debug.Log("M1A1 Fire Bullet Shotted!");
                }
                else if (Random.value <= fireShotChance && Time.time < _nextAllowedFireTime)
                {
                    Debug.Log("M1A1 fire suppressed due to weapon fire cooldown.");
                }

                // Critical roll
                if (Random.value <= critChance)
                {
                    bb.isCritBullet = true;
                    bb.InitCritBullet();
                    Debug.Log("M1A1 Crit Bullet Shotted!");
                }
        }
        if (audioSource != null && fireClip != null)
        {
            audioSource.PlayOneShot(fireClip);
        }
        Destroy(bulletInstance, bulletLifetime);

        if (muzzleFlash)
        {
            GameObject tempFlash = Instantiate(muzzleFlash, barrelPivot.position, barrelPivot.rotation);
            Destroy(tempFlash, 2f);
        }

        if (animator != null)
        {
            animator.Play("Fire", 0, 0f);
        }
    }

    public void CasingRelease()
    {
        if (!casingExitLocation || !casingPrefab) return;

        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);
        var rb = tempCasing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddExplosionForce(Random.Range(ejectPower * 0.7f, ejectPower),
                                 (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f),
                                 1f);
            rb.AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(100f, 1000f)), ForceMode.Impulse);
        }
        Destroy(tempCasing, casingDestroyTimer);
    }

    private float GetNextAllowedForSource(SteamVR_Input_Sources source)
    {
        if (nextAllowedFireTime.TryGetValue(source, out var t)) return t;
        return 0f;
    }

    private void SetNextAllowedForSource(SteamVR_Input_Sources source, float time)
    {
        nextAllowedFireTime[source] = time;
    }

    private IEnumerator DoDoubleShotAfterDelay(SteamVR_Input_Sources source, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (interactable == null || interactable.attachedToHand == null) yield break;
        if (interactable.attachedToHand.handType != source) yield break;

        Fire();
        yield break;
    }

    public void SetDoubleShotChance(float chance)
    {
        doubleShotChance = Mathf.Clamp01(chance);
    }

    /// <summary>
    /// Called when a fired frost bullet actually hits a target. Starts the frost cooldown
    /// so subsequent fired bullets won't be frost for the cooldown duration.
    /// </summary>
    /// <param name="seconds">Cooldown length in seconds (if zero, uses weapon's default).</param>
    public void StartFrostCooldown(float seconds = 0f)
    {
        float dur = seconds > 0f ? seconds : frostCooldownOnHit;
        _nextAllowedFrostTime = Time.time + dur;
        Debug.Log($"[M1A1] Frost cooldown started for {dur}s (until {_nextAllowedFrostTime:F2})");
    }

    /// <summary>
    /// Called when a fired fire bullet actually hits a target. Starts the fire cooldown
    /// so subsequent fired bullets won't be fire for the cooldown duration.
    /// </summary>
    public void StartFireCooldown(float seconds = 0f)
    {
        float dur = seconds > 0f ? seconds : fireCooldownOnHit;
        _nextAllowedFireTime = Time.time + dur;
        Debug.Log($"[M1A1] Fire cooldown started for {dur}s (until {_nextAllowedFireTime:F2})");
    }

    public float GetDoubleShotChance() => doubleShotChance;

    public void SetBulletSpeed(float speed = 300f) { shootingSpeed = speed; }
    public float GetBulletSpeed() { return shootingSpeed; }
    public void IncreaseBulletSpeedByPercentage(float percentage) { SetBulletSpeed(GetBulletSpeed() * (1f + percentage)); }

    public void SetFireRate(float firerate) { roundsPerSecond = firerate; }
    public float GetFireRate() { return roundsPerSecond; }
    public void IncreaseFireRateByPercentage(float percentage) { SetFireRate(GetFireRate() * (1f + percentage)); }

    public void IncreaseBulletDamge(float damage) { bulletDamage += damage; }

    public void OnAttachedToHand(Valve.VR.InteractionSystem.Hand hand)
    {
        if (rb == null) rb = GetComponent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            originalIsKinematic = rb.isKinematic;
            originalInterpolation = rb.interpolation;
            originalDetectCollisions = rb.detectCollisions;

            originalConstraints = rb.constraints;
            originalUseGravity = rb.useGravity;

            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.useGravity = false;

            savedConstraints = true;

            originalIsTrigger.Clear();
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                if (!c) continue;
                originalIsTrigger[c] = c.isTrigger;
                c.isTrigger = true;
            }
            var attach = hand.objectAttachmentPoint ? hand.objectAttachmentPoint : hand.transform;
            if (transform.parent != attach)
                transform.SetParent(attach, true);
        }
    }

    public void OnDetachedFromHand(Valve.VR.InteractionSystem.Hand hand)
    {
        if (rb != null)
        {
            rb.useGravity = originalUseGravity;
            rb.constraints = originalConstraints;
            rb.isKinematic = originalIsKinematic;
            rb.interpolation = originalInterpolation;
            rb.detectCollisions = originalDetectCollisions;
        }

        foreach (var kv in originalIsTrigger)
        {
            if (kv.Key) kv.Key.isTrigger = kv.Value;
        }
        originalIsTrigger.Clear();
        if (transform.parent == (hand.objectAttachmentPoint ? hand.objectAttachmentPoint : hand.transform))
            transform.SetParent(null, true);
    }
}
