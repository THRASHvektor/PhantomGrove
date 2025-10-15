using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 佩戴在手腕上的手环，实时显示玩家HP，通过修改手环材质的_BlendPos属性实现
 * 此脚本挂载手环模型上
 * 推荐由事件系统通知HP更新
 */
public class HPBracelet : MonoBehaviour
{
    private PlayerState playerState;
    private Material braceletMaterial;
    // Start is called before the first frame update
    void Start()
    {
        // 获取玩家状态组件
        playerState = GetComponentInParent<PlayerState>();
        braceletMaterial = GetComponent<Renderer>().material;
        braceletMaterial.SetFloat("_BlendPos", playerState.healthCurrent / playerState.healthMax);
        // 订阅玩家状态变化事件
        EventDispatcher<string>.addListener(GameEvents.Gameplay.Events.UpdateHPDisplay, UpdateHP);
    }

    void OnDestroy()
    {
        // 取消订阅玩家状态变化事件
        EventDispatcher<string>.removeListener(GameEvents.Gameplay.Events.UpdateHPDisplay, UpdateHP);
    }

    // Update is called once per frame
    void UpdateHP(string empty)
    {
        braceletMaterial.SetFloat("_BlendPos", playerState.healthCurrent / playerState.healthMax);
    }
}
