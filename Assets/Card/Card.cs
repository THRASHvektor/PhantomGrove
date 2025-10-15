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
    public enum CardEffectType { DoubleShot, FireRate, BulletSpeed, PlayerHP }

    

    

    private Renderer objectRenderer;
    private Animator animator;
    private CardManager manager;
    // Start is called before the first frame update

    private CardManager _manager;
    private CardEffectType _effect; 
    private bool _selected = false;
    private bool isHit = false;

    public void Initialize(CardManager manager, CardEffectType effect)
    {
        _manager = manager;
        _effect = effect;
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

    public void DestroyCard() //未选中的卡牌 淡出销毁函数
    {
        Destroy(gameObject, 3f);
        StartFadeOut();
    }

    void StartFadeOut()  //淡出动画的trigger
    {
        animator.SetTrigger("FadeOut");
    }
    void Start()
    {
        //RandomCardText();
        //manager = GetComponentInParent<CardManager>();
        animator = GetComponent<Animator>();
        selectedPartical.Stop();
        objectRenderer = GetComponent<Renderer>();
        objectRenderer.material = original;
    }

    // Update is called once per frame
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
    //                //manager.OnCardClicked(this);
    //                objectRenderer.material = selectedMaterial;
    //                ParticleSystem effect = Instantiate(selectedPartical, transform);
    //                Destroy(gameObject, 6f);
    //            }
    //        }
    //    }
    //}

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            _selected = true;
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
            Debug.Log($"Card selected => {_effect}");
            
        }
        DestroyCard();
    }

}
