using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * 管理玩家实时状态的数据模型
 * 例如：生命值、体力值、金钱等
 */
public class PlayerState : MonoBehaviour
{
    public float healthMax = 100f;
    public float healthCurrent { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        // 初始满血
        updateHealth(healthMax);
    }

    public void updateHealth(float currentHP)
    {
        healthCurrent = Mathf.Clamp(currentHP, 0, healthMax);
        EventDispatcher<string>.triggerEvent(GameEvents.Gameplay.Events.UpdateHPDisplay, null);
    }

    void Update()
    {
        updateHealth(healthCurrent - Time.deltaTime * 2); // 测试用，每秒掉2点血
    }

}
