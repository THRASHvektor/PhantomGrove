using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class M1911 : MonoBehaviour
{ // 你原有的字段
    public SteamVR_Action_Boolean fireAction; public GameObject muzzleFlash; public GameObject bullet; public Transform barrelPivot; public float shootingSpeed = 1; private ParticleSystem muzzleFlashPS;

    // 新增：抛壳相关（来自 SimpleShoot）
    [Header("Casing")]
    public GameObject casingPrefab;
    public Transform casingExitLocation;
    [Tooltip("自动销毁抛壳时间")]
    public float casingDestroyTimer = 2f;
    [Tooltip("抛壳力度")]
    public float ejectPower = 150f;

    private Interactable interactable;
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        interactable = GetComponent<Interactable>();
        if (muzzleFlash != null)
            muzzleFlashPS = muzzleFlash.GetComponent<ParticleSystem>();

    }

    void Update()
    {
        if (interactable != null && interactable.attachedToHand != null)
        {
            var source = interactable.attachedToHand.handType;
            if (fireAction != null && fireAction[source].stateDown)
            {
                Fire();
            }
        }
    }

    void Fire()
    {
        Debug.Log("Fire!");

        // 保留你的子弹逻辑
        var bulletrb = Instantiate(bullet, barrelPivot.position, barrelPivot.rotation).GetComponent<Rigidbody>();
        bulletrb.velocity = barrelPivot.forward * shootingSpeed;

        // 保留你的枪焰播放逻辑
        if (muzzleFlashPS != null && muzzleFlashPS.gameObject.activeInHierarchy)
        {
            muzzleFlashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlashPS.Play(true);
        }

        // 播放开火动画（确保 Animator 里有名为 "Fire" 的状态）
        if (animator != null)
        {
            // 如果你用的是 Trigger，也可以改为 SetTrigger("Fire")
            animator.Play("Fire", 0, 0f);
        }

        // 方案A（推荐）：用动画事件在合适的帧调用 CasingRelease()
        // 在 Fire 动画中添加一个 Animation Event，函数名填 "CasingRelease"

        // 方案B（可选）：如果暂时没有动画事件，可直接解开下一行，开火瞬间就抛壳
        // CasingRelease();
    }

    // 抛壳逻辑（来自官方 SimpleShoot，略有清理）
    public void CasingRelease()
    {
        if (!casingExitLocation || !casingPrefab)
            return;

        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);

        var rb = tempCasing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 给出爆炸力让弹壳弹出（与官方一致，可按需求调方向/系数）
            rb.AddExplosionForce(Random.Range(ejectPower * 0.7f, ejectPower),
                                 (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f),
                                 1f);

            rb.AddTorque(new Vector3(0,
                                     Random.Range(100f, 500f),
                                     Random.Range(100f, 1000f)),
                         ForceMode.Impulse);
        }

        Destroy(tempCasing, casingDestroyTimer);
    }
}