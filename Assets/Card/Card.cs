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

    public enum CardEffectType { DoubleShot, FireRate, BulletSpeed, FrostBullet, FrostTime, FireBullet, Damage, Critical, PlayerHP
    ,FrostBulletTest, FireBulletTest, CriticalTest
    }
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
            case CardEffectType.FireBullet:
                cardText.text = "Fire Shot +5%";
                break;
            case CardEffectType.FrostTime:
                cardText.text = "Frost Time +2s";
                break;
            case CardEffectType.Critical:
                cardText.text = "Crit Chance +5%";
                break;
            case CardEffectType.Damage:
                cardText.text = "Bullet Damage +5";
                break;
            case CardEffectType.FrostBulletTest:
                cardText.text = "Frost Shot applied";
                break;
            case CardEffectType.FireBulletTest:
                cardText.text = "Fire Shot applied";
                break;
            case CardEffectType.CriticalTest:
                cardText.text = "Critical Hit applied";
                break;
        }
    }

    public void DestroyCard()
    {
        // Always start the fade-out and destroy sequence for cleanup.
        // CardManager marks non-selected cards and then calls DestroyCard();
        // previously this method skipped destruction when `isSelect` was true,
        // which prevented non-selected cards from being removed because
        // CardManager set `isSelect = true` before calling DestroyCard().
        Destroy(gameObject, cardDestroyTime);
        StartFadeOut();
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
    /// ��Trigger�ж����Ʊ��ӵ�����
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (isSelect) return; // ��ֹ��δ���

        var bullet = other.GetComponent<BulletBehavior>();
        if (bullet != null)
        {
            isSelect = true;

            // Determine shooter type: support both M1911 (pistol) and M1A1 (SMG)
            var shooterObj = bullet.shooter;
            var shooter1911 = shooterObj != null ? shooterObj.GetComponent<M1911>() : null;
            var shooterM1A1 = shooterObj != null ? shooterObj.GetComponent<M1A1>() : null;

            switch (_effect)
            {
                case CardEffectType.DoubleShot:
                    if (shooter1911 != null)
                        shooter1911.SetDoubleShotChance(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.SetDoubleShotChance(0.01f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for DoubleShot.");
                    break;
                case CardEffectType.FireRate:
                    if (shooter1911 != null)
                        shooter1911.IncreaseFireRateByPercentage(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseFireRateByPercentage(0.01f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for FireRate.");
                    break;
                case CardEffectType.BulletSpeed:
                    if (shooter1911 != null)
                        shooter1911.IncreaseBulletSpeedByPercentage(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseBulletSpeedByPercentage(0.01f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for BulletSpeed.");
                    break;
                case CardEffectType.PlayerHP:
                    // TODO: Player effect affect.
                    break;
                case CardEffectType.Damage:
                    if (shooter1911 != null)
                        shooter1911.IncreaseBulletDamge(5f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseBulletDamge(1f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for Damage.");
                    break;
                case CardEffectType.FrostBullet:
                    if(shooter1911 != null)
                        shooter1911.IncreaseFrostChanceByAbsolute(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseFrostChanceByAbsolute(0.05f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for FrostBullet.");
                    break;
                case CardEffectType.FrostTime:
                    // TODO: FrostTime effect
                    break;
                case CardEffectType.FireBullet:
                    if (shooter1911 != null)
                        shooter1911.IncreaseFireChanceByAbsolute(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseFireChanceByAbsolute(0.05f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for FireBullet.");
                    break;
                case CardEffectType.Critical:
                    if (shooter1911 != null)
                        shooter1911.IncreaseCritChanceByAbsolute(0.05f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseCritChanceByAbsolute(0.05f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for Critical.");
                    break;
                case CardEffectType.FrostBulletTest:
                    if(shooter1911 != null)
                        shooter1911.IncreaseFrostChanceByAbsolute(1.0f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseFrostChanceByAbsolute(1.0f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for FrostBullet test effect.");
                    break;
                case CardEffectType.FireBulletTest:
                    if (shooter1911 != null)
                        shooter1911.IncreaseFireChanceByAbsolute(1.0f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseFireChanceByAbsolute(1.0f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for FireBullet test effect.");
                    break;
                case CardEffectType.CriticalTest:
                    if (shooter1911 != null)
                        shooter1911.IncreaseCritChanceByAbsolute(1.0f);
                    else if (shooterM1A1 != null)
                        shooterM1A1.IncreaseCritChanceByAbsolute(1.0f);
                    else
                        Debug.LogWarning("CardManager: shooter is null for Critical test effect.");
                    break;
            }

            // �����л�
            if (objectRenderer != null && selectedMaterial != null)
                objectRenderer.material = selectedMaterial;

            // ����ѡ����Ч
            if (selectedPartical != null)
            {
                ParticleSystem effect = Instantiate(selectedPartical, transform.position, Quaternion.identity, transform);
                effect.Play();
            }

            // ���Ŷ���
            StartFadeOut();

            // ֪ͨ������ѡ��
            if (_manager != null)
                _manager.NotifySelected(this);

            // �����ӵ�
            Destroy(bullet.gameObject);

            // ��������
            Destroy(gameObject, 4f);
        }
    }
}
