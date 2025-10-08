using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 2.0f;
    public Transform CameraRig;
    public Transform head;

    // Start is called before the first frame update
    void Start()
    {
        //后续统一使用我们自定义的操作集
        SteamVR_Actions.Phantom.Activate();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = SteamVR_Actions.Phantom.Move.GetAxis(SteamVR_Input_Sources.LeftHand);
        Vector3 direction = new Vector3(input.x, 0, input.y);
        Vector3 headYaw = new Vector3(head.forward.x, 0, head.forward.z).normalized;
        Vector3 moveDirection = Quaternion.LookRotation(headYaw) * direction;
        CameraRig.position += moveDirection * speed * Time.deltaTime;
    }
}
