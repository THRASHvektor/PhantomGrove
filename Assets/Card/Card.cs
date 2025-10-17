using System.Collections;
using System.Collections.Generic;
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
    /// <summary>
    /// Card effect type affect on weapon, player.
    /// </summary>
    public enum CardEffectType { DoubleShot, FireRate, BulletSpeed, FrostBullet,FrostTime,Damage,PlayerHP }

    private Renderer objectRenderer;
    private Animator animator;
    private CardManager manager;
    // Start is called before the first frame update
    /// <summary>
    /// Parent CardManager.
    /// </summary>
    private CardManager _manager;
    /// <summary>
    /// Card Effect.
    /// </summary>
    private CardEffectType _effect;
    /// <summary>
    /// Card destory time (default in 3s).
    /// </summary>
    private float cardDestroyTime = 3f;

    /// <summary>
    /// Initialize Card instance
    /// </summary>
    /// <param name="manager">Card's parent CardManager </param>
    /// <param name="effect">Card's effect</param>
    ///

    void SetAlbedoColor(Renderer r, Color c)
    { // 清除可能的 MPB 覆盖 r.SetPropertyBlock(null);

        var mat = r.material; // 实例化材质
        string colorProp = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        mat.SetColor(colorProp, c);
    }
    public void Initialize(CardManager manager, CardEffectType effect)
    {
        _manager = manager;
        _effect = effect;
        // Text display on Card.
        switch (_effect)
        {
            case CardEffectType.DoubleShot:
                cardText.text = new string("Double Shot +5%");
                break;
            case CardEffectType.FireRate:
                cardText.text = new string("Fire Rate +5%");
                break;
            case CardEffectType.BulletSpeed:
                cardText.text = new string("Bullet Speed +5%");
                break;
            case CardEffectType.PlayerHP:
                cardText.text = new string("Player HP +10");
                break;
            case CardEffectType.FrostBullet:
                cardText.text = new string("Frost Shot + 5%");
                break;
            case CardEffectType.FrostTime:
                break;
            case CardEffectType.Damage:
                break;
        }

    }

    //public void RandomCardText() //卡片文本显示函数
    //{
    //    int randomIndex = Random.Range(0, attributes.Length);
    //    string randomAttribute = attributes[randomIndex];

    //    randomIndex = Random.Range(0, numerical.Length);
    //    string randomNumerical = numerical[randomIndex];
    //    Text = randomAttribute + " + " + randomNumerical; //使用属性+数值拼接而成
    //    cardText.text = Text;
    //}

    /// <summary>
    /// Card destory and animator play.
    /// </summary>
    public void DestroyCard() 
    {
        if(isSelect == false)
        {
            Debug.Log("q");
            Destroy(gameObject, cardDestroyTime);
            StartFadeOut();
        }
        
    }
    /// <summary>
    /// Fade animator play.
    /// </summary>
    void StartFadeOut()  //淡出动画的trigger
    {
        animator.SetTrigger("FadeOut");
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        selectedPartical.Stop();
        objectRenderer = GetComponent<Renderer>();
    }

    //void Update()
    //{
    //    if (Input.GetMouseButtonDown(0))
    //    {
    //        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //        RaycastHit hit;
    //        if (Physics.Raycast(ray, out hit))
    //        {
    //            // 检查是否击中了当前物体
    //            if (hit.collider.gameObject == gameObject)//这个if下面的语句才是选中卡牌后执行的函数 上面的语句需要修改选中逻辑
    //            {
    //                objectRenderer.material = selectedMaterial;
    //                ParticleSystem effect = Instantiate(selectedPartical, transform);
    //                Destroy(gameObject, 6f);
    //                _manager.NotifySelected(this);
    //            }
    //        }
    //    }
    //}

    /// <summary>
    /// Detect Bullet collsion event.
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<BulletBehavior>())
        {
            
            var shooter = collision.gameObject.GetComponent<BulletBehavior>().shooter.GetComponent<M1911>();
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
                    //Todo: Player effect affect.
                    break;
            }
            objectRenderer.material = selectedMaterial;
            ParticleSystem effect = Instantiate(selectedPartical, transform);
            Destroy(gameObject, 4f);
            _manager.NotifySelected(this);
        }
        
        DestroyCard();
    }

}
