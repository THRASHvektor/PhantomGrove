using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SelectLeft : MonoBehaviour
{
    // Start is called before the first frame update
    public Material original;
    public Material selectedMaterial;
    public ParticleSystem selectedPartical;
    public bool isSelect = false;
    public float destroyDelay = 3f;

    private Renderer objectRenderer;
    private Animator animator;
    private bool isFading = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        selectedPartical.Stop();
        objectRenderer = GetComponent<Renderer>();
        Debug.Log("original");
        objectRenderer.material = original;
    }

    public void useSelectedMaterial()
    {
        if (isSelect == true)
        {
            Debug.Log("selected");
            objectRenderer.material = selectedMaterial;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isSelect = true;
            useSelectedMaterial();
            selectedPartical.transform.position = transform.position;
            selectedPartical.gameObject.SetActive(true);
            selectedPartical.Play();
            Debug.Log(transform.position);
            Destroy(gameObject, destroyDelay);
        }
        if (Input.anyKeyDown)//修改逻辑为：如果有卡牌被集中
        {
            if (isSelect == false) //修改逻辑为：如果被集中的卡牌不是这张卡牌
                StartFadeOut();
        }
    }
    void StartFadeOut()
    {
        isFading = true;
        // 触发过渡到淡出动画
        animator.SetTrigger("FadeOut");
    }
    public void OnFadeComplete()
    {
        Destroy(gameObject);
    }
}