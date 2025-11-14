using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("子弹基础设置")]
    [SerializeField] private int bulletDamage = 10;
    [SerializeField] private float bulletSpeed = 15f;
    public float lifeTime = 5f;
    public LayerMask hittableLayers = -1; // 可击中的层级[1](@ref)

    [Header("特殊效果")]
    public GameObject hitEffect;
    public AudioClip shootSound;
    public AudioClip hitSound;

    [Header("制导设置")]
    public bool hasHoming = false;
    public float homingStrength = 0.1f;

    private Vector3 moveDirection;
    private Transform target;
    private bool _hasHit = false; // 防重复触发标志[1](@ref)
    private float maxRange = 1000f;
    private float distanceTraveled = 0f;
    private Vector3 previousPosition;

    void Start()
    {
        // 自动销毁
        Destroy(gameObject, lifeTime);

        // 确保碰撞器是Trigger[1](@ref)
        var collider = GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;

        previousPosition = transform.position;

        // 播放射击音效
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }
    }

    // 初始化方法
    public void InitializeBullet(int damage, float speed, Transform targetTransform = null)
    {
        bulletDamage = damage;
        bulletSpeed = speed;
        target = targetTransform;

        // 设置移动方向
        if (target != null)
        {
            // 预测目标位置
            Vector3 targetPosition = target.position;
            Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
            if (targetRigidbody != null && !targetRigidbody.isKinematic)
            {
                float timeToTarget = Vector3.Distance(transform.position, targetPosition) / bulletSpeed;
                targetPosition += targetRigidbody.velocity * timeToTarget * 0.3f;
            }
            moveDirection = (targetPosition - transform.position).normalized;
        }
        else
        {
            moveDirection = transform.forward;
        }

        // 面向移动方向
        if (moveDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }
    }

    // 支持射程设置的初始化
    public void InitializeBullet(int damage, float speed, float range, Transform targetTransform = null)
    {
        maxRange = range;
        InitializeBullet(damage, speed, targetTransform);
    }

    void Update()
    {
        if (_hasHit) return;

        Vector3 startPosition = transform.position;

        // 移动子弹
        transform.position += moveDirection * bulletSpeed * Time.deltaTime;

        // 更新移动距离
        distanceTraveled += Vector3.Distance(startPosition, transform.position);

        // 射程检查
        if (distanceTraveled >= maxRange)
        {
            Destroy(gameObject);
            return;
        }

        // 制导逻辑
        if (hasHoming && target != null)
        {
            Vector3 desiredDirection = (target.position - transform.position).normalized;
            moveDirection = Vector3.Slerp(moveDirection, desiredDirection, homingStrength * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 手动碰撞检测（防穿透）
        ManualCollisionDetection(previousPosition, transform.position);
        previousPosition = transform.position;
    }

    /// <summary>
    /// 改进的碰撞检测 - 结合射线检测和Trigger的优点[2,5](@ref)
    /// </summary>
    void ManualCollisionDetection(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 direction = toPosition - fromPosition;
        float distance = direction.magnitude;

        if (distance > 0)
        {
            // 使用球形射线检测，提高准确性[2](@ref)
            RaycastHit[] hits = Physics.SphereCastAll(fromPosition, 0.1f, direction.normalized,
                distance, hittableLayers);

            // 按距离排序，处理最近的碰撞
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (ShouldIgnoreCollision(hit.collider))
                    continue;

                if (ProcessCollision(hit.collider, hit.point))
                {
                    _hasHit = true;
                    transform.position = hit.point;
                    PlayHitEffect();
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 碰撞忽略判断[1](@ref)
    /// </summary>
    bool ShouldIgnoreCollision(Collider collider)
    {
        if (collider.gameObject == gameObject) return true;
        if (collider.isTrigger) return true;
        if (collider.CompareTag("Enemy")) return true;
        if (collider.CompareTag("Bullet")) return true;

        return false;
    }

    /// <summary>
    /// 处理碰撞逻辑
    /// </summary>
    bool ProcessCollision(Collider collider, Vector3 hitPoint)
    {
        // 击中玩家
        if (collider.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(bulletDamage);
                Debug.Log($"远程攻击造成{bulletDamage}血");
                return true;
            }
        }

        // 击中环境物体（非忽略的物体）
        return true;
    }

    /// <summary>
    /// 保留Trigger检测作为备用[1](@ref)
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        // 层级过滤[1](@ref)
        if (((1 << other.gameObject.layer) & hittableLayers.value) == 0)
            return;

        if (ShouldIgnoreCollision(other))
            return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        if (ProcessCollision(other, hitPoint))
        {
            _hasHit = true;
            PlayHitEffect();
            Destroy(gameObject);
        }
    }

    void PlayHitEffect()
    {
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
    }

    // 属性访问器
    public int Damage => bulletDamage;
    public float Speed => bulletSpeed;
    public float DistanceTraveled => distanceTraveled;
}