using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ball behaviour for being hit by bullets.
/// On hit: briefly change color for visual feedback, notify spawner, then destroy when health reaches zero.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TargetBall : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health points for the ball.")]
    public float maxHealth = 30f;

    [Tooltip("Current health points.")]
    public float currentHealth = 30f;

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

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isHit = false;
    private bool isFrost = false;
    private BallSpawner spawner;

    // Health bar components
    private Canvas healthBarCanvas;
    private Image healthBarFill;
    private RectTransform healthBarFillRect;

    private float lastHitTime = 0f;

    private float frostExpireTime;
    private Coroutine frostCo;
    private Coroutine hitCo;

    void Awake()
    {
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

        // Create health fill - single red bar that shrinks from right to left
        GameObject fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(canvasGO.transform);
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.color = Color.red;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = 0; // Fill from left to right

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
            float healthPercent = (float)currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercent;

            // Change color based on health percentage
            if (healthPercent > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }
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
        var bb = c.gameObject.GetComponent<BulletBehavior>(); if (bb == null) return;

        // 极短冷却，避免同帧重复判定导致抖动，又不影响快速连发
        if (Time.time - lastHitTime < 0.01f) return;
        lastHitTime = Time.time;
        // 先直接应用伤害（同步），协程只做视觉/延时销毁
        ApplyDamageImmediate(bb);
        Debug.Log("Current HP: "+currentHealth);

        var now = Time.time;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }
        hitCo = StartCoroutine(HitFeedbackRoutine());
        //StartCoroutine(HitFeedbackCoroutine(c.contacts.Length > 0 ? (Vector3?)c.contacts[0].point : null));
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
            Destroy(gameObject,1f);
        }
    }

    public void ApplyOrRefreshFrost(float frostTime, float speedSlowRate)
    { // 如果已冰冻，刷新到新的过期时间
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
        gameObject.GetComponent<MonsterChase>().moveSpeed *= (1f - speedSlowRate);
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
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
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

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }

        gameObject.GetComponent<MonsterChase>().moveSpeed = 1f; // 需要在 Awake/Start 存一下初始速度
        frostCo = null;
    }

    private void HandleHit(GameObject other, Vector3? hitPoint)
    {
        if (isHit) return;

        // Detect bullet either by BulletBehavior component or tag
        BulletBehavior bb = other.GetComponent<BulletBehavior>();
       if (bb != null)
        {
            StartCoroutine(TakeDamage(hitPoint, bb));
        }
    }

    /// <summary>
    /// Coroutine that handles taking damage, visual feedback, and destruction when health reaches zero.
    /// </summary>
    private IEnumerator TakeDamage(Vector3? hitPoint, BulletBehavior bb)
    {
        isHit = true;

        // Show health bar when taking damage
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }

        // Spawn impact effect if any
        if (impactPrefab != null && hitPoint.HasValue)
        {
            Instantiate(impactPrefab, hitPoint.Value, Quaternion.identity);
        }

        // Reduce health
        currentHealth = currentHealth - bb.damage;
        isHit = false;

        //currentHealth--;
        if (bb.isFrostBullet && !isFrost) 
        {
            StartCoroutine(AffectFrostBulletEffect(bb.frostTime, bb.speedSlowRate));
        }

        UpdateHealthBar();

        // Flash hit color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }

        if (hitColorShowTime > 0f)
            yield return new WaitForSeconds(hitColorShowTime);

        // Check if ball should be destroyed
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

            // Add a small delay before destruction for death effects
            yield return new WaitForSeconds(0.1f);

            // Destroy this ball
            Destroy(gameObject);
        }
        else
        {
            // Restore original color if ball survives
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = originalColors[i];
            }

            // Reset hit flag after recovery
            isHit = false;

            // Hide health bar after a moment if not dead
            StartCoroutine(HideHealthBarAfterDelay(2f));
        }
    }

    /// <summary>
    /// Hides the health bar after a delay if the ball is still alive.
    /// </summary>
    private IEnumerator HideHealthBarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (healthBarCanvas != null && currentHealth > 0 && currentHealth == maxHealth)
        {
            healthBarCanvas.gameObject.SetActive(false);
        }
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
            StartCoroutine(HideHealthBarAfterDelay(2f));
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
            StartCoroutine(HideHealthBarAfterDelay(2f));
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

    /// <summary>
    /// Frost Bullet Effect.
    /// </summary>
    /// <param name="frostTime"></param>
    /// <param name="speedSlowRate"></param>
    /// <returns></returns>
    public IEnumerator AffectFrostBulletEffect(float frostTime, float speedSlowRate)
    {
        var originSpeed = gameObject.GetComponent<MonsterChase>().moveSpeed;
        gameObject.GetComponent<MonsterChase>().moveSpeed = originSpeed * (1f - speedSlowRate);
        // Flash hit color
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = frostColor;
        }
        // wait for configured delay
        yield return new WaitForSeconds(frostTime);
        isFrost = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
        gameObject.GetComponent<MonsterChase>().moveSpeed = originSpeed;
        
    }
}