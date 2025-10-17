using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Locomotion")] public SteamVR_Action_Vector2 moveAction; // 绑定你的移动向量（拇指摇杆/触摸板）
    public SteamVR_Input_Sources moveHand = SteamVR_Input_Sources.LeftHand; public float moveSpeed = 1.5f; [Range(0f, 0.5f)] public float moveDeadzone = 0.15f;

    [Header("Turn")]
    public SteamVR_Action_Vector2 turnAction;                   // 绑定你的转向向量（通常单独的Axis）
    public SteamVR_Input_Sources turnHand = SteamVR_Input_Sources.RightHand;
    public float turnSpeedDegPerSec = 180f;
    [Range(0f, 0.5f)] public float turnDeadzone = 0.2f;

    [Header("Gravity")]
    public float gravity = 9.81f;

    [Header("Debug")]
    public bool requireFocus = true;                            // 需要应用窗口有焦点才接收输入
    public bool logInputEverySecond = false;                    // 打印输入诊断日志

    private CharacterController cc;
    private float verticalVelocity; // 简单重力

    private void Awake()
    {
        cc = GetComponent<CharacterController>();

        // 可选：如果未在 Inspector 里绑定，尝试用自动生成的 Actions 赋值（按你项目的 Action 名称调整）
        try
        {
            if (moveAction == null)
            {
                // 如果项目里生成了 SteamVR_Actions.default_Move 这类，取消注释：
                // moveAction = SteamVR_Actions.default_Move;
            }
            if (turnAction == null)
            {
                // turnAction = SteamVR_Actions.default_Turn;
            }
        }
        catch { /* 忽略 */ }
    }

    private void Update()
    {
        if (requireFocus && !Application.isFocused)
            return;

        // 验证 Player/HMD
        Transform hmd = Player.instance != null ? Player.instance.hmdTransform : null;
        if (hmd == null)
        {
            // 没有 HMD 就用自己的 transform 方向
            hmd = transform;
        }

        // 读取移动输入
        Vector2 mv = Vector2.zero;
        if (moveAction != null)
        {
            mv = moveAction.GetAxis(moveHand);
            if (mv.sqrMagnitude < moveDeadzone * moveDeadzone)
                mv = Vector2.zero;
        }

        // 计算基于 HMD 的地面方向
        Vector3 fwd = Vector3.ProjectOnPlane(hmd.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward; // 兜底
        Vector3 right = Vector3.ProjectOnPlane(hmd.right, Vector3.up).normalized;

        Vector3 moveOnPlane = fwd * mv.y + right * mv.x;

        // 简单重力
        if (cc.isGrounded)
        {
            verticalVelocity = -0.1f; // 轻微贴地
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 velocity = moveOnPlane * moveSpeed;
        Vector3 step = new Vector3(velocity.x, verticalVelocity, velocity.z) * Time.deltaTime;

        // 移动
        if (cc != null && cc.enabled)
        {
            cc.Move(step);
        }
        else
        {
            // 回退：无 CharacterController 时直接改位置（无碰撞/落地检测）
            transform.position += step;
        }

        // 转向（平滑）
        if (turnAction != null)
        {
            Vector2 t = turnAction.GetAxis(turnHand);
            float x = Mathf.Abs(t.x) < turnDeadzone ? 0f : t.x;
            if (!Mathf.Approximately(x, 0f))
            {
                Vector3 pivot = new Vector3(hmd.position.x, transform.position.y, hmd.position.z);
                float yaw = x * turnSpeedDegPerSec * Time.deltaTime;
                transform.RotateAround(pivot, Vector3.up, yaw);
            }
        }

        // 可选诊断日志
        if (logInputEverySecond && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VRMove] focused={Application.isFocused}, grounded={cc.isGrounded}, mv={mv}, speed={moveSpeed}, vertY={verticalVelocity:0.00}");
        }
    }
}