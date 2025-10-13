using UnityEngine;
using UnityEngine.AI;

public class MonsterWander : MonoBehaviour
{
    [Header("建筑物点（请拖入三个建筑物的Transform）")]
    public Transform buildingA;
    public Transform buildingB;
    public Transform buildingC;

    [Header("怪物移动参数")]
    public float wanderRadius = 2f; // 距离建筑物边界的最大偏移
    public float moveSpeed = 2f;
    public float waitTimeMin = 1f;
    public float waitTimeMax = 3f;

    [Header("玩家")]
    public Transform player;
    public float chaseRadius = 10f; // 追踪玩家的范围

    [Header("攻击参数")]
    public int damagePerHit = 10;
    public float attackInterval = 2f;

    [Header("调试")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
    private Vector3 areaCenter;
    private float areaRadius;
    private float waitTimer = 0f;
    private float currentWaitTime = 0f;
    private bool isWaiting = false;

    private enum State { Wandering, Chasing }
    private State currentState = State.Wandering;

    // 攻击相关
    private bool isPlayerInRange = false;
    private float attackTimer = 0f;
    private PlayerHealth playerHealth;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent 组件未挂载在怪物对象上！");
            enabled = false;
            return;
        }

        agent.speed = moveSpeed;
        agent.autoBraking = true;

        if (buildingA == null || buildingB == null || buildingC == null)
        {
            Debug.LogError("请在Inspector中拖入三个建筑物的Transform！");
            enabled = false;
            return;
        }

        // 计算三建筑物的中心和半径
        areaCenter = (buildingA.position + buildingB.position + buildingC.position) / 3f;
        areaRadius = Mathf.Max(
            Vector3.Distance(areaCenter, buildingA.position),
            Vector3.Distance(areaCenter, buildingB.position),
            Vector3.Distance(areaCenter, buildingC.position)
        ) + wanderRadius;

        // 获取玩家血量脚本
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        // 初始随机移动
        MoveToRandomPoint();
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("玩家Transform未赋值！");
            return;
        }

        float playerDistance = Vector3.Distance(transform.position, player.position);

        if (playerDistance <= chaseRadius)
        {
            // 进入追踪状态
            if (currentState != State.Chasing)
            {
                currentState = State.Chasing;
                agent.speed = moveSpeed * 1.5f; // 追踪时速度可提升
            }

            // 检查玩家是否在NavMesh上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(player.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                Debug.LogWarning("玩家不在NavMesh上，怪物无法追踪！");
            }
        }
        else
        {
            // 回到游荡状态
            if (currentState != State.Wandering)
            {
                currentState = State.Wandering;
                agent.speed = moveSpeed;
                StartWaiting(); // 重新等待再游荡
            }

            if (isWaiting)
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= currentWaitTime)
                {
                    isWaiting = false;
                    MoveToRandomPoint();
                }
            }
            else if (HasReachedDestination())
            {
                StartWaiting();
            }
        }

        // 攻击逻辑
        if (isPlayerInRange && playerHealth != null)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                attackTimer = 0f;
                playerHealth.TakeDamage(damagePerHit);
            }
        }
        else
        {
            attackTimer = 0f; // 离开时重置计时
        }
    }

    void MoveToRandomPoint()
    {
        Vector3 randomPoint = GetRandomPointInArea();
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(randomPoint);
        }
    }

    Vector3 GetRandomPointInArea()
    {
        // 在建筑物围成的圆形范围内随机取点
        Vector2 randomCircle = Random.insideUnitCircle * areaRadius;
        Vector3 randomPos = areaCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        return randomPos;
    }

    bool HasReachedDestination()
    {
        if (agent == null || !agent.enabled) return false;
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance &&
               agent.velocity.magnitude < 0.1f;
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
        currentWaitTime = Random.Range(waitTimeMin, waitTimeMax);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            isPlayerInRange = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            isPlayerInRange = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || buildingA == null || buildingB == null || buildingC == null) return;

        // 绘制建筑物点
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(buildingA.position, 0.3f);
        Gizmos.DrawWireSphere(buildingB.position, 0.3f);
        Gizmos.DrawWireSphere(buildingC.position, 0.3f);

        // 绘制怪物活动范围
        Vector3 center = (buildingA.position + buildingB.position + buildingC.position) / 3f;
        float radius = Mathf.Max(
            Vector3.Distance(center, buildingA.position),
            Vector3.Distance(center, buildingB.position),
            Vector3.Distance(center, buildingC.position)
        ) + wanderRadius;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, radius);

        // 绘制追踪范围
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, chaseRadius);
        }
    }
}
