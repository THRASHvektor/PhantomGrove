using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 子弹行为脚本，适用于Trigger判定和代码控制移动
/// </summary>
public class BulletBehavior : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float moveSpeed = 20f;         // 子弹速度
    public float lifetime = 5f;           // 存活时间
    public float damage = 10f;            // 伤害
    public float knockbackForce = 0f;     // 击退力度（普通子弹为0，特殊子弹可设置）

    [Header("Frost Bullet Settings")]
    public bool isFrostBullet = false;    // 是否为寒冰子弹
    public float frostTime = 2f;          // 寒冰持续时间
    public float speedSlowRate = 0.1f;    // 怪物减速比例

    [Header("Shooter & Layer")]
    public GameObject shooter;            // 发射者
    public LayerMask hittableLayers;      // 可命中的层

    [Header("Impact Effect")]
    public GameObject impactPrefab;       // 命中特效

    void Start()
    {
        // 自动销毁
        Destroy(gameObject, lifetime);

        // 确保Collider是Trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        // 代码控制子弹移动
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// 初始化寒冰子弹参数
    /// </summary>
    public void InitFrostBullet(float frostTime, float speedSlowRate)
    {
        this.frostTime = frostTime;
        this.speedSlowRate = speedSlowRate;
        this.isFrostBullet = true;
    }

    /// <summary>
    /// Trigger判定命中
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // 不打自己
        if (shooter != null && other.transform.IsChildOf(shooter.transform)) return;

        // 只打hittableLayers
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0)
        {
            Destroy(gameObject);
            return;
        }

        // 命中怪物
        var target = other.GetComponent<TargetBall>();
        if (target != null)
        {
            target.ApplyDamageImmediate(this);

            // 击退（如果需要）
            if (knockbackForce > 0)
            {
                Vector3 direction = (other.transform.position - transform.position).normalized;
                target.ApplyKnockback(direction, knockbackForce);
            }
        }

        // 播放击中特效
        if (impactPrefab != null)
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            Instantiate(impactPrefab, hitPoint, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
