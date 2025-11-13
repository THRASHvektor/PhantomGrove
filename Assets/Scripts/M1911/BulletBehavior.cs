using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ӵ���Ϊ�ű���������Trigger�ж��ʹ�������ƶ�
/// </summary>
public class BulletBehavior : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float moveSpeed = 20f;         // �ӵ��ٶ�
    public float lifetime = 5f;           // ���ʱ��
    public float damage = 10f;            // �˺�
    public float knockbackForce = 0f;     // �������ȣ���ͨ�ӵ�Ϊ0�������ӵ������ã�

    [Header("Frost Bullet Settings")]
    public bool isFrostBullet = false;    // �Ƿ�Ϊ�����ӵ�
    public float frostTime = 2f;          // ��������ʱ��
    public float speedSlowRate = 0.1f;    // ������ٱ���
    [Header("Critical")]
    public bool isCritBullet = false;     // 是否暴击子弹

    public void InitCritBullet()
    {
        this.isCritBullet = true;
    }

    [Header("Shooter & Layer")]
    public GameObject shooter;            // ������
    public LayerMask hittableLayers;      // �����еĲ�
    // Guard to ensure this bullet only applies damage once (multiple child colliders
    // on a target can cause multiple OnTriggerEnter calls for the same physical
    // bullet). Set to true when the bullet processes a hit so further triggers
    // are ignored.
    private bool _hasHit = false;

    [Header("Impact Effect")]
    public GameObject impactPrefab;       // ������Ч

    void Start()
    {
        // �Զ�����
        Destroy(gameObject, lifetime);

        // ȷ��Collider��Trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        // ��������ӵ��ƶ�
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// ��ʼ�������ӵ�����
    /// </summary>
    public void InitFrostBullet(float frostTime, float speedSlowRate)
    {
        this.frostTime = frostTime;
        this.speedSlowRate = speedSlowRate;
        this.isFrostBullet = true;
    }

    /// <summary>
    /// Trigger�ж�����
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;
        // �����Լ�
        if (shooter != null && other.transform.IsChildOf(shooter.transform)) return;

        // ֻ��hittableLayers
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0)
        {
            Destroy(gameObject);
            return;
        }

        // ���й���
        var target = other.GetComponent<TargetBall>();
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        if (target != null)
        {
            _hasHit = true;
            target.ApplyDamageImmediate(this, hitPoint);

            // ���ˣ������Ҫ��
            if (knockbackForce > 0)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                target.ApplyKnockback(direction, knockbackForce);
            }
        }

        // ���Ż�����Ч
        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, hitPoint, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
