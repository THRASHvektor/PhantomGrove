using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class M1911AnimationEvents : MonoBehaviour
{
    private M1911 gun;
    // Start is called before the first frame update
    void Awake()
    {
        gun = GetComponentInParent<M1911>();
    }
    public void Shoot()               // 对应动画事件 “Shoot”
    {
        
    }

    public void CasingRelease()       // 对应动画事件 “CasingRelease”
    {
        if (gun != null)
        {
            gun.SendMessage("CasingRelease", SendMessageOptions.DontRequireReceiver);
        }
    }
}
