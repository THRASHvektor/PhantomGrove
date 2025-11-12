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
    /// 由子弹调用，造成伤害和反馈
    /// </summary>
    public void ApplyDamageImmediate(BulletBehavior bb)
    {
        currentHealth -= bb.damage;

        // 寒冰效果
        if (bb.isFrostBullet)
        {
            ApplyOrRefreshFrost(bb.frostTime, bb.speedSlowRate);
        }

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
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
    /// 由子弹调用，造成击退
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
    /// 寒冰减速效果
    /// </summary>
    public void ApplyOrRefreshFrost(float frostTime, float speedSlowRate)
    {
        var now = Time.time;
        frostExpireTime = now + Mathf.Max(0f, frostTime);

        isFrost = true;

        if (monsterChase != null)
        {
            monsterChase.moveSpeed = originalMoveSpeed * (1f - speedSlowRate);
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
            monsterChase.moveSpeed = originalMoveSpeed;
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
