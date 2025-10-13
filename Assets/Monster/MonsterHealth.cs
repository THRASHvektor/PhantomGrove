using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // ËÀÍöÌØÐ§µÈ
        Destroy(gameObject);
    }
}
