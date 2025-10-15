using UnityEngine;
using Valve.VR;

public class VRInputDebugger : MonoBehaviour
{
    public SteamVR_Action_Vector2 moveAction; // 在 Inspector 指定你的移动 action

    void Update()
    {
        if (moveAction == null)
        {
            Debug.Log("VRInputDebugger: moveAction is null");
            return;
        }

        // 推荐使用 GetAxis(...)
        Vector2 left = moveAction.GetAxis(SteamVR_Input_Sources.LeftHand);
        Vector2 right = moveAction.GetAxis(SteamVR_Input_Sources.RightHand);
        Vector2 any = moveAction.GetAxis(SteamVR_Input_Sources.Any);

        Debug.Log($"MoveAxis L:{left.x:F2},{left.y:F2}  R:{right.x:F2},{right.y:F2}  Any:{any.x:F2},{any.y:F2}");
    }
}