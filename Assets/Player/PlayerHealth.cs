using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log("初始血量: " + currentHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log("当前血量: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("血量为0，游戏退出！");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
