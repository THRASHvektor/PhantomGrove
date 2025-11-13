using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log("��ʼѪ��: " + currentHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log("��ǰѪ��: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Ѫ��Ϊ0����Ϸ�˳���");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
