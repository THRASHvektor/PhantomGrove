using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event controller for M1911 firing animation.
/// </summary>
public class M1911AnimationEvents : MonoBehaviour
{
    private M1911 gun;
    
    void Awake()
    {
        gun = GetComponentInParent<M1911>();
    }
    /// <summary>
    /// Empty.
    /// </summary>
    public void Shoot()               
    {
        
    }

    /// <summary>
    /// For CasingRelease animation
    /// </summary>
    public void CasingRelease()       
    {
        if (gun != null)
        {
            gun.SendMessage("CasingRelease", SendMessageOptions.DontRequireReceiver);
        }
    }
}
