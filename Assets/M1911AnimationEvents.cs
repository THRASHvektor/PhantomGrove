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
    public void Shoot()               // ��Ӧ�����¼� ��Shoot��
    {
        
    }

    public void CasingRelease()       // ��Ӧ�����¼� ��CasingRelease��
    {
        if (gun != null)
        {
            gun.SendMessage("CasingRelease", SendMessageOptions.DontRequireReceiver);
        }
    }
}
