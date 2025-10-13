using UnityEngine;
using UnityEngine.AI;

public class MonsterChase : MonoBehaviour
{
    [Header("怪物移动参数")]
    public float moveSpeed = 3f;

    [Header("玩家")]
    public Transform player;
    public float chaseRadius = 30f; // 追踪玩家的范围

    [Header("攻击参数")]
    public int damagePerHit = 10;
    public float attackInterval = 2f;

    [Header("调试")]
    public bool drawGizmos = true;

    private NavMeshAgent agent;
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

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
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
            // 检查玩家是否在NavMesh上
            NavMeshHit hit;
            if (NavMesh.SamplePosition(player.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            // 玩家离开追击范围，怪物停止移动
            agent.ResetPath();
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
        if (!drawGizmos) return;

        // 绘制追踪范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
