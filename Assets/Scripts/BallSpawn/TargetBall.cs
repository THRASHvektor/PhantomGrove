using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class TargetBall : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health points for the ball.")]
    public float maxHealth = 50f;

    [Tooltip("Current health points.")]
    public float currentHealth = 50f;

    [Header("Visual Feedback")]
    [Tooltip("Color to flash when hit.")]
    public Color hitColor = Color.red;
    public Color frostColor = Color.blue;

    [Tooltip("Time (seconds) to show the hit color before restoring original color (small value, e.g. 0.1).")]
    public float hitColorShowTime = 0.12f;

    [Tooltip("Optional impact prefab to spawn on hit.")]
    public GameObject impactPrefab;

    [Header("Health Bar Settings")]
    [Tooltip("Offset position for the health bar above the ball.")]
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);

    [Tooltip("Width of the health bar.")]
    public float healthBarWidth = 2f;

    [Tooltip("Height of the health bar.")]
    public float healthBarHeight = 0.3f;

    [Header("Health Bar Gradient")]
    public Gradient healthBarGradient; // 在Inspector里设置渐变色

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHit = false;
    private bool isFrost = false;
    private BallSpawner spawner;

    // Health bar components
    private Canvas healthBarCanvas;
    private Image healthBarFill;
    private RectTransform healthBarFillRect;
    private Image healthBarBG; // 新增：血条背景

    private float lastHitTime = 0f;

    private float frostExpireTime;
    private Coroutine frostCo;
    private Coroutine hitCo;

    // 存储初始移动速度
    private float originalMoveSpeed;

    void Awake()
    {
        // 存储初始移动速度
        MonsterChase monsterChase = GetComponent<MonsterChase>();
        if (monsterChase != null)
        {
            originalMoveSpeed = monsterChase.moveSpeed;
        }

        // Initialize renderers and original colors
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        // Initialize health
        currentHealth = maxHealth;

        // Create health bar UI
        CreateHealthBar();
        healthBarCanvas.gameObject.SetActive(true);
    }

    /// <summary>
    /// Creates and sets up a clean health bar UI above the ball.
    /// </summary>
    private void CreateHealthBar()
    {
        // Create canvas for the health bar
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = healthBarOffset;
        canvasGO.transform.localRotation = Quaternion.identity;

        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Set canvas size - only as large as needed for the health bar
        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);

        // Create background
        GameObject bgGO = new GameObject("HealthBarBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        healthBarBG = bgGO.AddComponent<Image>();
        healthBarBG.color = new Color(0, 0, 0, 0.5f); // 半透明黑色
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.localPosition = Vector3.zero;

        // Create health fill - 删除长度变化，只保留颜色渐变
        GameObject fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        healthBarFill = fillGO.AddComponent<Image>();
        // 删除填充类型设置，使用普通图片
        healthBarFill.type = Image.Type.Simple;

        healthBarFillRect = fillGO.GetComponent<RectTransform>();
        healthBarFillRect.anchorMin = Vector2.zero;
        healthBarFillRect.anchorMax = Vector2.one;
        healthBarFillRect.sizeDelta = Vector2.zero;
        healthBarFillRect.localPosition = Vector3.zero;

        // Set initial health bar state
        UpdateHealthBar();

        // Hide health bar initially, show when damaged
        healthBarCanvas.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates the health bar display based on current health.
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);

            // 删除血条长度变化，只设置颜色渐变
            if (healthBarGradient != null)
                healthBarFill.color = healthBarGradient.Evaluate(healthPercent);
            else
                healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }

        // 实时输出血量
        Debug.Log($"[{gameObject.name}] HP: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Makes the health bar always face the main camera.
    /// </summary>
    void LateUpdate()
    {
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    /// <summary>
    /// Called by spawner immediately after instantiation to set up callback.
    /// </summary>
    public void SetSpawner(BallSpawner s)
    {
        spawner = s;
    }

    // Accept both collision and trigger depending on bullet setup
    void OnCollisionEnter(Collision c)
    {
        var bb = c.gameObject.GetComponent<BulletBehavior>();
        if (bb == null) return;

        // 极短冷却，避免同帧重复判定导致抖动，又不影响快速连发
        if (Time.time - lastHitTime < 0.01f) return;
        lastHitTime = Time.time;

        // 先直接应用伤害（同步），协程只做视觉/延时销毁
        ApplyDamageImmediate(bb);
        Debug.Log("Current HP: " + currentHealth);

        // 视觉反馈
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }

        if (hitCo != null) StopCoroutine(hitCo);
        hitCo = StartCoroutine(HitFeedbackRoutine());
    }

    void ApplyDamageImmediate(BulletBehavior bb)
    {
        currentHealth -= bb.damage;

        // Frost Shot decision
        if (bb.isFrostBullet) ApplyOrRefreshFrost(bb.frostTime, bb.speedSlowRate);

        UpdateHealthBar();

        // Monster Death Decision
        if (currentHealth <= 0)
        {
            // Hide health bar on death
            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(false);
            }
            // Notify spawner BEFORE destroying (so spawner can decrement/track)
            if (spawner != null)
                spawner.NotifyBallDestroyed(this);
            // Destroy this ball
            Destroy(gameObject, 0.2f);
        }
        else
        {
            // 显示血条
            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(true);
            }
        }
    }

    public void ApplyOrRefreshFrost(float frostTime, float speedSlowRate)
    {
        var now = Time.time;
        if (isFrost)
        {
            if (frostTime > 0f)
            {
                frostExpireTime = now + frostTime;
            }
            return;
        }

        isFrost = true;
        MonsterChase monsterChase = GetComponent<MonsterChase>();
        if (monsterChase != null)
        {
            monsterChase.moveSpeed = originalMoveSpeed * (1f - speedSlowRate);
        }

        // 颜色改为冰冻色
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = frostColor;
        }

        frostExpireTime = now + frostTime;
        if (frostCo != null)
        {
            StopCoroutine(frostCo);
        }
        frostCo = StartCoroutine(FrostRoutine());
    }

    private IEnumerator HitFeedbackRoutine()
    {
        yield return new WaitForSeconds(hitColorShowTime);

        // 如果不是冰冻状态，恢复原色
        if (!isFrost)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
            }
        }
        hitCo = null;
    }

    private IEnumerator FrostRoutine()
    {
        while (Time.time < frostExpireTime)
        {
            yield return null;
        }

        isFrost = false;

        // 恢复原色
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }

        // 恢复移动速度
        MonsterChase monsterChase = GetComponent<MonsterChase>();
        if (monsterChase != null)
        {
            monsterChase.moveSpeed = originalMoveSpeed;
        }

        frostCo = null;
    }

    /// <summary>
    /// Method to directly set health for testing or special effects.
    /// </summary>
    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthBar();

        // Show health bar when health changes
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Method to heal the ball by specified amount.
    /// </summary>
    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);
        UpdateHealthBar();

        // Show health bar when healed
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Shows or hides the health bar.
    /// </summary>
    public void SetHealthBarVisible(bool visible)
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }
    }


}