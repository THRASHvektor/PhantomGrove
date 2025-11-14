using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyRangedAI : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 2f;
    public float chaseRadius = 20f;
    // 移除 attackRange，因为现在远程范围无限大
    public float meleeRange = 2f;

    [Header("远程攻击设置")]
    public int damagePerShot = 8;
    public float rangedAttackCooldown = 3f;
    public float attackWindUpTime = 0.5f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("近战攻击设置")]
    public int meleeDamage = 15;
    public float meleeAttackCooldown = 1.5f;
    public float meleeWindUpTime = 0.3f;

    [Header("目标")]
    public Transform player;

    [Header("调试")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private float rangedAttackTimer;
    private float meleeAttackTimer;
    private bool isAttacking = false;
    private Animator animator;
    private PlayerHealth playerHealth;

    // 状态管理
    private enum EnemyState { Idle, Chasing, RangedAttacking, MeleeAttacking }
    private EnemyState currentState = EnemyState.Idle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerHealth = player.GetComponent<PlayerHealth>();
                Debug.Log("自动找到玩家并赋值！");
            }
            else
            {
                Debug.LogError("场景中没有找到带有Player标签的对象！");
            }
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = meleeRange;

        // 移除对 attackRange 的检查，因为现在远程范围无限大
        if (meleeRange >= chaseRadius)
        {
            Debug.LogWarning("近战范围不能大于等于追逐范围，已自动修正！");
            meleeRange = chaseRadius * 0.5f;
        }

        rangedAttackTimer = rangedAttackCooldown;
        meleeAttackTimer = meleeAttackCooldown;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("Player 未赋值！");
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        UpdateStateMachine(distanceToPlayer);
        UpdateAttackCooldowns();
        UpdateAnimationState();
    }

    void UpdateStateMachine(float distanceToPlayer)
    {
        // 主要修改：现在只要玩家在追逐范围内，就考虑攻击
        if (distanceToPlayer > chaseRadius)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (distanceToPlayer <= meleeRange)
        {
            ChangeState(EnemyState.MeleeAttacking);
        }
        else
        {
            // 玩家在追逐范围内但不在近战范围内，使用远程攻击
            // 由于远程范围无限大，只要在追逐范围内就可以远程攻击
            ChangeState(EnemyState.RangedAttacking);
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Chasing:
                HandleChaseState();
                break;
            case EnemyState.RangedAttacking:
                HandleRangedAttackState();
                break;
            case EnemyState.MeleeAttacking:
                HandleMeleeAttackState();
                break;
        }
    }

    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"怪物状态切换: {currentState} -> {newState}");

        if (currentState == EnemyState.RangedAttacking || currentState == EnemyState.MeleeAttacking)
        {
            isAttacking = false;
            StopAllCoroutines();
            if (agent != null)
                agent.isStopped = false;
        }

        currentState = newState;
    }

    void HandleIdleState()
    {
        agent.ResetPath();
    }

    void HandleChaseState()
    {
        // 注意：在无限远程范围的情况下，可能很少进入纯追逐状态
        Debug.Log("怪物正在追逐玩家");
        if (!agent.hasPath || Vector3.Distance(agent.destination, player.position) > 1f)
        {
            agent.SetDestination(player.position);
        }
    }

    void HandleRangedAttackState()
    {
        // 主要修改：在远程攻击状态下也保持移动，寻找最佳攻击位置
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 寻找最佳攻击距离（比近战范围稍远）
        float optimalRange = meleeRange + 3f;

        if (distanceToPlayer > optimalRange)
        {
            // 距离过远，向玩家移动
            if (!agent.hasPath || Vector3.Distance(agent.destination, player.position) > 1f)
            {
                agent.SetDestination(player.position);
            }
        }
        else if (distanceToPlayer < meleeRange + 1f)
        {
            // 距离过近，后退保持距离
            Vector3 retreatDirection = (transform.position - player.position).normalized;
            Vector3 retreatPosition = transform.position + retreatDirection * 2f;
            agent.SetDestination(retreatPosition);
        }
        else
        {
            // 在理想距离，停止移动准备攻击
            if (agent.hasPath)
                agent.ResetPath();
        }

        FacePlayerSmoothly();

        // 只要在追逐范围内且不在攻击中，就可以进行远程攻击
        if (!isAttacking && rangedAttackTimer >= rangedAttackCooldown)
        {
            StartCoroutine(RangedAttackRoutine());
        }
    }

    void HandleMeleeAttackState()
    {
        if (agent.hasPath)
            agent.ResetPath();

        FacePlayerSmoothly();

        if (!isAttacking && meleeAttackTimer >= meleeAttackCooldown)
        {
            StartCoroutine(MeleeAttackRoutine());
        }
    }

    void FacePlayerSmoothly()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    IEnumerator RangedAttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger("PrepareRangedAttack");

        yield return new WaitForSeconds(attackWindUpTime);

        FireBullet();
        rangedAttackTimer = 0f;
        isAttacking = false;
        agent.isStopped = false;

        if (animator != null)
            animator.SetTrigger("RangedAttack");
    }

    IEnumerator MeleeAttackRoutine()
    {
        isAttacking = true;
        agent.isStopped = true;

        if (animator != null)
            animator.SetTrigger("PrepareMeleeAttack");

        yield return new WaitForSeconds(meleeWindUpTime);

        PerformMeleeAttack();
        meleeAttackTimer = 0f;
        isAttacking = false;
        agent.isStopped = false;

        if (animator != null)
            animator.SetTrigger("MeleeAttack");
    }

    void UpdateAttackCooldowns()
    {
        if (rangedAttackTimer < rangedAttackCooldown)
        {
            rangedAttackTimer += Time.deltaTime;
        }

        if (meleeAttackTimer < meleeAttackCooldown)
        {
            meleeAttackTimer += Time.deltaTime;
        }
    }

    void UpdateAnimationState()
    {
        if (animator == null) return;

        bool isMoving = agent.velocity.magnitude > 0.1f &&
                        currentState != EnemyState.RangedAttacking &&
                        currentState != EnemyState.MeleeAttacking;

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsRangedAttacking", currentState == EnemyState.RangedAttacking);
        animator.SetBool("IsMeleeAttacking", currentState == EnemyState.MeleeAttacking);
    }

    void FireBullet()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogError("子弹预制体或发射点未设置！");
            return;
        }

        // 计算子弹方向（指向玩家）
        Vector3 bulletDirection = (player.position - firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(bulletDirection));

        // 获取子弹组件并初始化
        EnemyBullet enemyBullet = bullet.GetComponent<EnemyBullet>();
        if (enemyBullet != null)
        {
            // 设置极大的射程或使用射线检测实现真正无限范围
            enemyBullet.InitializeBullet(damagePerShot, 1000f, player); // 设置极大射程
        }
        else
        {
            // 备用方案：使用物理系统
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = bulletDirection * 15f;

                // 添加自动销毁组件，避免子弹无限飞行
                AutoDestroy autoDestroy = bullet.GetComponent<AutoDestroy>();
                if (autoDestroy == null)
                {
                    autoDestroy = bullet.AddComponent<AutoDestroy>();
                }
                autoDestroy.destroyTime = 10f; // 10秒后自动销毁
            }
        }
    }

    void PerformMeleeAttack()
    {
        if (playerHealth != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= meleeRange * 1.2f)
            {
                playerHealth.TakeDamage(meleeDamage);
                Debug.Log($"近战攻击造成 {meleeDamage} 点伤害");
            }
            else
            {
                Debug.Log("玩家不在近战范围内");
            }
        }
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0, newSpeed);
        if (agent != null)
        {
            agent.speed = moveSpeed;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);

        // 不再绘制攻击范围，因为现在是无限大
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        if (firePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(firePoint.position, 0.1f);
            Gizmos.DrawLine(firePoint.position, firePoint.position + firePoint.forward);
        }
    }
}

// 自动销毁组件，防止子弹无限存在
public class AutoDestroy : MonoBehaviour
{
    public float destroyTime = 10f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}