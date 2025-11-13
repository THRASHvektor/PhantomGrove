using UnityEngine;
using UnityEngine.AI;

public class MonsterChase : MonoBehaviour
{
    [Header("�����ƶ�����")]
    public float moveSpeed = 3f;

    [Header("���")]
    public Transform player;
    public float chaseRadius = 30f; // ׷����ҵķ�Χ

    [Header("��������")]
    public int damagePerHit = 10;
    public float attackCooldown = 5f; // ������ȴʱ�䣨�룩

    [Header("����")]
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
            Debug.LogError("NavMeshAgent ���δ�����ڹ�������ϣ�");
            enabled = false;
            return;
        }

        agent.speed = moveSpeed;
        agent.autoBraking = true;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    /// <summary>
    /// Safely set the movement speed for this monster and immediately update the NavMeshAgent.
    /// Use this when external code wants to change speed at runtime (e.g. frost slow).
    /// </summary>
    /// <param name="s">New move speed.</param>
    public void SetMoveSpeed(float s)
    {
        moveSpeed = s;
        if (agent != null)
        {
            agent.speed = moveSpeed;
            // If speed is effectively zero, clear path and stop the agent so it halts immediately.
            bool stop = moveSpeed <= 0.0001f;
            if (stop)
            {
                agent.ResetPath();
                agent.isStopped = true;
                Debug.Log($"[MonsterChase] SetMoveSpeed STOP on {gameObject.name}: speed={moveSpeed}, path cleared, isStopped={agent.isStopped}");
            }
            else
            {
                // Resume agent movement immediately and set destination to player if available.
                // Also clamp current agent velocity so reducing speed takes immediate effect
                // instead of allowing a longer residual sliding due to existing velocity.
                if (agent.velocity.sqrMagnitude > 0f && agent.velocity.magnitude > moveSpeed)
                {
                    agent.velocity = agent.velocity.normalized * moveSpeed;
                }
                agent.isStopped = false;
                if (player != null)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(player.position, out hit, 1.0f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                }
                Debug.Log($"[MonsterChase] SetMoveSpeed RESUME on {gameObject.name}: speed={moveSpeed}, isStopped={agent.isStopped}");
            }
        }
    }

    void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("���Transformδ��ֵ��");
            return;
        }

        float playerDistance = Vector3.Distance(transform.position, player.position);

        if (playerDistance <= chaseRadius)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(player.position, out hit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            agent.ResetPath();
        }

        // Note: moveSpeed should be changed via SetMoveSpeed(...) to update agent.speed immediately.

        // ������ȴ�߼�
        if (isPlayerInRange && playerHealth != null)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                playerHealth.TakeDamage(damagePerHit);
            }
        }
        else
        {
            attackTimer = 0f; // �뿪ʱ���ü�ʱ
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            isPlayerInRange = true;
            attackTimer = 0f; // ����ʱ���ü�ʱ
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damagePerHit); // ������Ѫ
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            isPlayerInRange = false;
            attackTimer = 0f; // �뿪ʱ���ü�ʱ
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}
