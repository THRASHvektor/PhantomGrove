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
    
    private string[] attributes = //卡片加成的属性
    {
        "Damage",
        "Movement",
        "HP",
        "Bullet",
        "Cooldown"
    };

    private string[] numerical = //卡片加成的数值 注意此处是string类型
    {
        "1",
        "2",
        "3",
        "4",
        "5"
    };

    private Renderer objectRenderer;
    private Animator animator;
    private CardManager manager;
    // Start is called before the first frame update
    
    public void RandomCardText() //卡片文本显示函数
    {
        int randomIndex = Random.Range(0, attributes.Length);
        string randomAttribute = attributes[randomIndex];

        randomIndex = Random.Range(0, numerical.Length);
        string randomNumerical = numerical[randomIndex];
        Text = randomAttribute + " + " + randomNumerical; //使用属性+数值拼接而成
        cardText.text = Text;
    }

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
        RandomCardText();
        manager = GetComponentInParent<CardManager>();
        animator = GetComponent<Animator>();
        selectedPartical.Stop();
        objectRenderer = GetComponent<Renderer>();
        objectRenderer.material = original;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // 检查是否击中了当前物体
                if (hit.collider.gameObject == gameObject)//这个if下面的语句才是选中卡牌后执行的函数 上面的语句需要修改选中逻辑
                {
                    manager.OnCardClicked(this);
                    objectRenderer.material = selectedMaterial;
                    ParticleSystem effect = Instantiate(selectedPartical, transform);
                    Destroy(gameObject, 6f);
                }
            }
        }
    }
}
