using System.Collections;
using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    public Material original;
    public Material selectedMaterial;
    public ParticleSystem selectedPartical;
    public bool isSelect = false;
    public TextMeshPro cardText;
    public string Text;

    public enum CardEffectType { DoubleShot, FireRate, BulletSpeed, FrostBullet, FrostTime, Damage, PlayerHP }

    private Renderer objectRenderer;
    private Animator animator;
    private CardManager _manager;
    private CardEffectType _effect;
    private float cardDestroyTime = 3f;

    void SetAlbedoColor(Renderer r, Color c)
    {
        var mat = r.material;
        string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        mat.SetColor(colorProp, c);
    }

    public void Initialize(CardManager manager, CardEffectType effect)
    {
        _manager = manager;
        _effect = effect;
        isSelect = false;
        switch (_effect)
        {
            case CardEffectType.DoubleShot:
                cardText.text = "Double Shot +5%";
                break;
            case CardEffectType.FireRate:
                cardText.text = "Fire Rate +5%";
                break;
            case CardEffectType.BulletSpeed:
                cardText.text = "Bullet Speed +5%";
                break;
            case CardEffectType.PlayerHP:
                cardText.text = "Player HP +10";
                break;
            case CardEffectType.FrostBullet:
                cardText.text = "Frost Shot +5%";
                break;
            case CardEffectType.FrostTime:
                cardText.text = "Frost Time +2s";
                break;
            case CardEffectType.Damage:
                cardText.text = "Bullet Damage +5";
                break;
        }
    }

    public void DestroyCard()
    {
        if (!isSelect)
        {
            Destroy(gameObject, cardDestroyTime);
            StartFadeOut();
        }
    }

    void StartFadeOut()
    {
        if (animator != null)
            animator.SetTrigger("FadeOut");
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (selectedPartical != null)
            selectedPartical.Stop();
        objectRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// 用Trigger判定卡牌被子弹射中
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (isSelect) return; // 防止多次触发

        var bullet = other.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            isSelect = true;

            // 卡牌效果应用到武器
            var shooter = bullet.shooter != null ? bullet.shooter.GetComponent<M1911>() : null;
            switch (_effect)
            {
                case CardEffectType.DoubleShot:
                    if (shooter != null) shooter.SetDoubleShotChance(0.05f);
                    else Debug.LogWarning("CardManager: shooter is null for DoubleShot.");
                    break;
                case CardEffectType.FireRate:
                    if (shooter != null) shooter.IncreaseFireRateByPercentage(0.05f);
                    else Debug.LogWarning("CardManager: shooter is null for FireRate.");
                    break;
                case CardEffectType.BulletSpeed:
                    if (shooter != null) shooter.IncreaseBulletSpeedByPercentage(0.05f);
                    else Debug.LogWarning("CardManager: shooter is null for BulletSpeed.");
                    break;
                case CardEffectType.PlayerHP:
                    // TODO: Player effect affect.
                    break;
                case CardEffectType.Damage:
                    if (shooter != null) shooter.IncreaseBulletDamge(5f);
                    break;
                case CardEffectType.FrostBullet:
                    // TODO: FrostBullet effect
                    break;
                case CardEffectType.FrostTime:
                    // TODO: FrostTime effect
                    break;
            }

            // 材质切换
            if (objectRenderer != null && selectedMaterial != null)
                objectRenderer.material = selectedMaterial;

            // 播放选中特效
            if (selectedPartical != null)
            {
                ParticleSystem effect = Instantiate(selectedPartical, transform.position, Quaternion.identity, transform);
                effect.Play();
            }

            // 播放动画
            StartFadeOut();

            // 通知管理器选中
            if (_manager != null)
                _manager.NotifySelected(this);

            // 销毁子弹
            Destroy(bullet.gameObject);

            // 销毁自身
            Destroy(gameObject, 4f);
        }
    }
}
