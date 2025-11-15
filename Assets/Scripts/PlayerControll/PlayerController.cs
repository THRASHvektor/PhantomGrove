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
    // Snap turn (瞬时转向) 设置
    [Tooltip("Snap turn left boolean action (optional)")]
    public SteamVR_Action_Boolean snapTurnLeftAction;
    [Tooltip("Snap turn right boolean action (optional)")]
    public SteamVR_Action_Boolean snapTurnRightAction;
    [Tooltip("Snap angle in degrees (e.g. 45)")]
    public float snapAngle = 45f;
    [Tooltip("Cooldown between snap turns (seconds)")]
    public float snapCooldown = 0.25f;
    private float _lastSnapTime = -10f;
    [Tooltip("If the vertical axis (push up) on the stick is above this value, snap turns are disabled (use for teleport input)")]
    [Range(0f, 1f)] public float snapDisableVerticalThreshold = 0.75f;
    [Tooltip("Teleport boolean action - when true, snap turns are disabled (optional)")]
    public SteamVR_Action_Boolean teleportAction;
    [Tooltip("Input source for teleport action")]
    public SteamVR_Input_Sources teleportHand = SteamVR_Input_Sources.Any;

    [Header("Menu")]
    public SteamVR_Action_Boolean menuAction;                  // 绑定你的菜单按钮
    public SteamVR_Input_Sources menuHand = SteamVR_Input_Sources.LeftHand;
    public GameObject SystemMenuPrefab;                      // 系统菜单UI预制体
    private GameObject _systemMenuInstance;

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
            if (menuAction == null)
            {
                // menuAction = SteamVR_Actions.default_Menu;
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

        // Snap turn（瞬时转向）: 检测 SnapTurnLeft / SnapTurnRight action 的 down 事件并立即旋转
        // 如果摇杆正在向上推（用于 teleport），则禁用 snap turn
        float verticalPush = 0f;
        if (turnAction != null)
            verticalPush = Mathf.Max(verticalPush, turnAction.GetAxis(turnHand).y);
        if (moveAction != null)
            verticalPush = Mathf.Max(verticalPush, moveAction.GetAxis(moveHand).y);

        bool teleportActive = (teleportAction != null) && teleportAction.GetState(teleportHand);

        if (!teleportActive && verticalPush <= snapDisableVerticalThreshold && Time.time - _lastSnapTime >= snapCooldown)
        {
            bool didSnap = false;
            float snapYaw = 0f;

            if (snapTurnRightAction != null && snapTurnRightAction.GetStateDown(turnHand))
            {
                snapYaw = Mathf.Abs(snapAngle);
                didSnap = true;
            }
            else if (snapTurnLeftAction != null && snapTurnLeftAction.GetStateDown(turnHand))
            {
                snapYaw = -Mathf.Abs(snapAngle);
                didSnap = true;
            }

            if (didSnap)
            {
                Vector3 pivot = new Vector3(hmd.position.x, transform.position.y, hmd.position.z);
                transform.RotateAround(pivot, Vector3.up, snapYaw);
                _lastSnapTime = Time.time;
            }
        }

        // 显示系统菜单
        if (menuAction != null && menuAction.GetStateDown(menuHand))
        {
            if (_systemMenuInstance == null && SystemMenuPrefab != null)
            {
                _systemMenuInstance = Instantiate(SystemMenuPrefab, transform.Find("SteamVRObjects/VRCamera/UI"), false);
            }
        }

        // 可选诊断日志
        if (logInputEverySecond && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VRMove] focused={Application.isFocused}, grounded={cc.isGrounded}, mv={mv}, speed={moveSpeed}, vertY={verticalVelocity:0.00}");
        }
    }
}