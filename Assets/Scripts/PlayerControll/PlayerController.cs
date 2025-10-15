using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerController : MonoBehaviour
{
    public SteamVR_Action_Vector2 turnAction;                 
    public SteamVR_Input_Sources turnHand = SteamVR_Input_Sources.RightHand;
    public float turnSpeedDegPerSec = 180f;
    public float turnDeadzone = 0.2f;

    public SteamVR_Action_Vector2 input;

    public float speed = 1;
    private CharacterController characterController;

    // Start is called before the first frame update
    private  void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 direction = Player.instance.hmdTransform.TransformDirection(new Vector3(input.axis.x, 0, input.axis.y));
        transform.position += speed * Time.deltaTime *  Vector3.ProjectOnPlane(direction, Vector3.up);

        //characterController.Move(speed * Time.deltaTime * Vector3.ProjectOnPlane(direction, Vector3.up)- new Vector3(0,9.81f,0)*Time.deltaTime);
        if (turnAction != null)
        {
            Vector2 t = turnAction.GetAxis(turnHand);
            float x = Mathf.Abs(t.x) < turnDeadzone ? 0f : t.x;
            if (!Mathf.Approximately(x, 0f))
            {
                Transform hmd = Player.instance.hmdTransform;
                Vector3 pivot = new Vector3(hmd.position.x, transform.position.y, hmd.position.z);
                float yaw = x * turnSpeedDegPerSec * Time.deltaTime;
                transform.RotateAround(pivot, Vector3.up, yaw);
            }
        }
    }
    //void Awake()
    //{
       
    //    if (turnAction == null)
    //        turnAction = SteamVR_Actions.default_Turn1;

        
    //    if (turnAction == null)
    //        turnAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("Turn1"); // 

        
    //    if (turnAction != null) turnAction.actionSet.Activate(SteamVR_Input_Sources.Any, 0, false);
    //    Vector2 t = turnAction?.GetAxis(turnHand) ?? Vector2.zero;
    //    Debug.Log($"Turn axis={t}");
    //}
}
