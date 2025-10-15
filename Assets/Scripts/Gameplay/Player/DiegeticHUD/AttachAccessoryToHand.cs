using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/**
 * 将配件（如手环）附加到手部模型的指定位置，做特定的HUD显示
 * 此脚本挂载在手部模型prefab上
 * 通过在Inspector中设置modelPrefab和attachPointName来实现
 */
public class AttachAccessoryToHand : MonoBehaviour
{
    public GameObject modelPrefab;
    public string attachPointName = "Root";

    public Transform targetBone;

    void Start()
    {
        Invoke("AttachAccessory", 0.5f); // 延迟0.5秒执行，确保模型已加载
    }

    private void AttachAccessory()
    {
        Transform targetAttachPoint = null;
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child.name == attachPointName)
            {
                targetAttachPoint = child;
                break;
            }
        }

        GameObject accessory = Instantiate(modelPrefab);
        accessory.transform.SetParent(targetBone, false);
        accessory.transform.localPosition = Vector3.zero;
        accessory.transform.localRotation = Quaternion.identity;
        accessory.transform.localScale = Vector3.one;
        // if (targetAttachPoint != null && modelPrefab != null)
        // {
        //     GameObject accessory = Instantiate(modelPrefab);
        //     accessory.transform.parent = targetAttachPoint;
        //     accessory.transform.localPosition = Vector3.zero;
        //     accessory.transform.localRotation = Quaternion.identity;
        //     accessory.transform.localScale = Vector3.one;
        // }
    }


}
