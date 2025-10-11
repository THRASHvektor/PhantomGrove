using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class M1911 : MonoBehaviour
{ // ��ԭ�е��ֶ�
    public SteamVR_Action_Boolean fireAction; public GameObject muzzleFlash; public GameObject bullet; public Transform barrelPivot; public float shootingSpeed = 1; private ParticleSystem muzzleFlashPS;

    // �������׿���أ����� SimpleShoot��
    [Header("Casing")]
    public GameObject casingPrefab;
    public Transform casingExitLocation;
    [Tooltip("�Զ������׿�ʱ��")]
    public float casingDestroyTimer = 2f;
    [Tooltip("�׿�����")]
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

        // ��������ӵ��߼�
        var bulletrb = Instantiate(bullet, barrelPivot.position, barrelPivot.rotation).GetComponent<Rigidbody>();
        bulletrb.velocity = barrelPivot.forward * shootingSpeed;

        // �������ǹ�沥���߼�
        if (muzzleFlashPS != null && muzzleFlashPS.gameObject.activeInHierarchy)
        {
            muzzleFlashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlashPS.Play(true);
        }

        // ���ſ��𶯻���ȷ�� Animator ������Ϊ "Fire" ��״̬��
        if (animator != null)
        {
            // ������õ��� Trigger��Ҳ���Ը�Ϊ SetTrigger("Fire")
            animator.Play("Fire", 0, 0f);
        }

        // ����A���Ƽ������ö����¼��ں��ʵ�֡���� CasingRelease()
        // �� Fire ���������һ�� Animation Event���������� "CasingRelease"

        // ����B����ѡ���������ʱû�ж����¼�����ֱ�ӽ⿪��һ�У�����˲����׿�
        // CasingRelease();
    }

    // �׿��߼������Թٷ� SimpleShoot����������
    public void CasingRelease()
    {
        if (!casingExitLocation || !casingPrefab)
            return;

        GameObject tempCasing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);

        var rb = tempCasing.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // ������ը���õ��ǵ�������ٷ�һ�£��ɰ����������/ϵ����
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