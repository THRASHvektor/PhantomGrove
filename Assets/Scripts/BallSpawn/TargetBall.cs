using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class TargetBall : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;

    [Header("Visual Feedback")]
    public Color hitColor = Color.red;
    public Color frostColor = Color.blue;
    public float hitColorShowTime = 0.12f;
    public GameObject impactPrefab;

    [Header("Health Bar Settings")]
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
    public float healthBarWidth = 2f;
    public float healthBarHeight = 0.3f;

    [Header("Health Bar Gradient")]
    public Gradient healthBarGradient;

    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFrost = false;
    private BallSpawner spawner;

    // Health bar components
    private Canvas healthBarCanvas;
    private Image healthBarFill;
    private RectTransform healthBarFillRect;
    private Image healthBarBG;

    private float frostExpireTime;
    private Coroutine frostCo;
    private Coroutine frostFeedbackCo;
    private Coroutine hitCo;

    private float originalMoveSpeed;
    private MonsterChase monsterChase;
    private bool isDead = false;
    [Header("Damage Popup")]
    [Tooltip("Prefab for floating damage text. Should have a TextMeshPro component or DamagePopup script on root.")]
    public GameObject damageTextPrefab;

    void Awake()
    {
        monsterChase = GetComponent<MonsterChase>();
        if (monsterChase != null)
        {
            originalMoveSpeed = monsterChase.moveSpeed;
        }

        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }

        currentHealth = maxHealth;
        CreateHealthBar();
        healthBarCanvas.gameObject.SetActive(true);
    }

    private void CreateHealthBar()
    {
        GameObject canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = healthBarOffset;
        canvasGO.transform.localRotation = Quaternion.identity;

        healthBarCanvas = canvasGO.AddComponent<Canvas>();
        healthBarCanvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(healthBarWidth, healthBarHeight);

        GameObject bgGO = new GameObject("HealthBarBG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        healthBarBG = bgGO.AddComponent<Image>();
        healthBarBG.color = new Color(0, 0, 0, 0.5f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.localPosition = Vector3.zero;

        GameObject fillGO = new GameObject("HealthBarFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.type = Image.Type.Simple;

        healthBarFillRect = fillGO.GetComponent<RectTransform>();
        healthBarFillRect.anchorMin = Vector2.zero;
        healthBarFillRect.anchorMax = Vector2.one;
        healthBarFillRect.sizeDelta = Vector2.zero;
        healthBarFillRect.localPosition = Vector3.zero;

        UpdateHealthBar();
        healthBarCanvas.gameObject.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / maxHealth);
            if (healthBarGradient != null)
                healthBarFill.color = healthBarGradient.Evaluate(healthPercent);
            else
                healthBarFill.color = Color.Lerp(Color.red, Color.green, healthPercent);
        }
        Debug.Log($"[{gameObject.name}] HP: {currentHealth}/{maxHealth}");
    }

    void LateUpdate()
    {
        if (healthBarCanvas != null && Camera.main != null)
        {
            healthBarCanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void SetSpawner(BallSpawner s)
    {
        spawner = s;
    }

    /// <summary>
    /// ���ӵ����ã�����˺��ͷ���
    /// </summary>
    // Updated to accept the hit point so we can spawn damage popups there.
    public void ApplyDamageImmediate(BulletBehavior bb, Vector3 hitPoint)
    {
        if (isDead) return;

        float appliedDamage = bb.isCritBullet ? bb.damage * 2f : bb.damage;
        currentHealth -= appliedDamage;

        // Start hit feedback immediately so frost feedback waits for it.
        if (hitCo != null) StopCoroutine(hitCo);
        hitCo = StartCoroutine(HitFeedbackRoutine());

        // ����Ч�� (frost applied after hit feedback finishes)
        if (bb.isFrostBullet)
        {
            ApplyOrRefreshFrost(bb.frostTime, bb.speedSlowRate);

            // Notify shooter (weapon) that a frost bullet actually hit so it can start frost cooldown.
            if (bb.shooter != null)
            {
                var w1 = bb.shooter.GetComponent<M1911>();
                if (w1 != null) w1.StartFrostCooldown();
                var w2 = bb.shooter.GetComponent<M1A1>();
                if (w2 != null) w2.StartFrostCooldown();
            }
        }

        // Spawn damage popup at hit point if prefab set
        if (damageTextPrefab != null)
        {
            var go = Instantiate(damageTextPrefab, hitPoint + Vector3.up * 0.1f, Quaternion.identity);
            // Ensure popup faces camera and has sane scale so it's visible
            if (Camera.main != null)
            {
                go.transform.rotation = Camera.main.transform.rotation;
            }
            go.transform.localScale = Vector3.one;
            var popup = go.GetComponent<DamagePopup>();
            if (popup != null)
            {
                string dmgText = ((int)appliedDamage).ToString();
                Color c;
                if (bb.isCritBullet)
                    c = new Color(1f, 0.5f, 0f); // orange for crit
                else if (bb.isFrostBullet)
                    c = frostColor;
                else
                    c = Color.white;
                popup.Init(dmgText, c);
                Debug.Log($"[TargetBall] Spawned DamagePopup '{dmgText}' color={c} at {hitPoint}");
            }
            else
            {
                // fallback: if prefab uses TextMeshPro directly
                var tmp = go.GetComponent<TMPro.TextMeshPro>();
                if (tmp != null)
                {
                    tmp.text = ((int)appliedDamage).ToString();
                    if (bb.isCritBullet)
                        tmp.color = new Color(1f, 0.5f, 0f);
                    else if (bb.isFrostBullet)
                        tmp.color = frostColor;
                    else
                        tmp.color = Color.white;
                    Destroy(go, 1f);
                    Debug.Log($"[TargetBall] Spawned TMP damage text '{tmp.text}' color={tmp.color} at {hitPoint}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[TargetBall] damageTextPrefab not assigned on {gameObject.name}; no damage popup will be shown.");
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            isDead = true;
            // disable colliders to avoid receiving further triggers during the destroy delay
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                if (col != null) col.enabled = false;
            }

            if (healthBarCanvas != null)
                healthBarCanvas.gameObject.SetActive(false);

            if (spawner != null)
                spawner.NotifyBallDestroyed(this);

            Destroy(gameObject, 0.2f);
        }
        else
        {
            if (healthBarCanvas != null)
                healthBarCanvas.gameObject.SetActive(true);
        }

        if (hitCo != null) StopCoroutine(hitCo);
        hitCo = StartCoroutine(HitFeedbackRoutine());
    }

    /// <summary>
    /// ���ӵ����ã���ɻ���
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// ��������Ч��
    /// </summary>
    public void ApplyOrRefreshFrost(float frostTime, float speedSlowRate)
    {
        var now = Time.time;
        frostExpireTime = now + Mathf.Max(0f, frostTime);

        isFrost = true;

        if (monsterChase != null)
        {
            float newSpeed = originalMoveSpeed * (1f - speedSlowRate);
            monsterChase.SetMoveSpeed(newSpeed);
            Debug.Log($"[TargetBall] Apply frost to {gameObject.name}: moveSpeed {originalMoveSpeed} -> {newSpeed}");

            // Log NavMeshAgent state for debugging immediate-stop behavior
            var agent = monsterChase.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                Debug.Log($"[TargetBall] Agent state after frost apply: velocity={agent.velocity.magnitude:F3}, hasPath={agent.hasPath}, isStopped={agent.isStopped}, remainingDistance={agent.remainingDistance:F3}");
            }
        }

        if (frostCo != null)
            StopCoroutine(frostCo);

        frostCo = StartCoroutine(FrostRoutine());
        if (frostFeedbackCo != null)
            StopCoroutine(frostFeedbackCo);
        frostFeedbackCo = StartCoroutine(FrostFeedbackRoutine());
    }

    private IEnumerator FrostFeedbackRoutine()
    {
        while (hitCo != null)
            yield return null;

        if (isFrost)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                    renderers[i].material.color = frostColor;
            }
            Debug.Log("Frost Color Apply");
        }

        while (isFrost)
            yield return null;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
        frostFeedbackCo = null;
    }

    private IEnumerator FrostRoutine()
    {
        Debug.Log("Enable Frost");
        while (Time.time < frostExpireTime)
            yield return null;

        isFrost = false;

        if (monsterChase != null)
        {
            monsterChase.SetMoveSpeed(originalMoveSpeed);
            Debug.Log($"[TargetBall] Disable frost on {gameObject.name}: restore moveSpeed -> {originalMoveSpeed}");
            var agent = monsterChase.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                Debug.Log($"[TargetBall] Agent state after frost disable: velocity={agent.velocity.magnitude:F3}, hasPath={agent.hasPath}, isStopped={agent.isStopped}, remainingDistance={agent.remainingDistance:F3}");
            }
        }
        frostCo = null;
        Debug.Log("Disable Frost");
    }

    private IEnumerator HitFeedbackRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = hitColor;
        }
        yield return new WaitForSeconds(hitColorShowTime);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                renderers[i].material.color = originalColors[i];
        }
        hitCo = null;
    }

    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthBar();

        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }
    }

    public void Heal(int healAmount)
    {
        currentHealth = Mathf.Clamp(currentHealth + healAmount, 0, maxHealth);
        UpdateHealthBar();

        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(true);
        }
    }

    public void SetHealthBarVisible(bool visible)
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }
    }
}
