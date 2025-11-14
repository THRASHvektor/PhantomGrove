using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // Optional: If left empty, we'll try to reuse the SystemMenuPrefab from PlayerController
    public GameObject systemMenuPrefab;
    private GameObject _systemMenuInstance;

    void Awake()
    {
        currentHealth = maxHealth;
        Debug.Log("初始生命: " + currentHealth);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);
        Debug.Log("当前生命: " + currentHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("生命为0，显示死亡菜单并暂停游戏");

            // Try to obtain the menu prefab from PlayerController if not assigned
            if (systemMenuPrefab == null)
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null)
                {
                    systemMenuPrefab = pc.SystemMenuPrefab;
                }
            }

            // If we have a prefab, instantiate it under the player's UI root (same as PlayerController does)
            if (systemMenuPrefab != null && _systemMenuInstance == null)
            {
                // Attempt to find the player's UI parent (SteamVRObjects/VRCamera/UI)
                Transform uiParent = null;
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null)
                {
                    var parent = pc.transform.Find("SteamVRObjects/VRCamera/UI");
                    if (parent != null) uiParent = parent;
                }

                if (uiParent != null)
                {
                    _systemMenuInstance = Instantiate(systemMenuPrefab, uiParent, false);
                }
                else
                {
                    // fallback: instantiate at world origin
                    _systemMenuInstance = Instantiate(systemMenuPrefab, Vector3.zero, Quaternion.identity);
                }

                // Pause game while menu is open. Menu buttons should restore Time.timeScale when appropriate.
                Time.timeScale = 0f;
            }
            else
            {
                // If no menu prefab is available, fall back to exiting the application (behavior before)
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}
