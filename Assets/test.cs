using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Material selectedMaterial;
    private Renderer objectRenderer;
    // Start is called before the first frame update
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
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
                    objectRenderer.material = selectedMaterial;
                    Destroy(gameObject, 6f);
                }
            }
        }
    }
}
