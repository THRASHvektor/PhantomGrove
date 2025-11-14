using UnityEngine;

// HandSphereDebug: 在手上生成一个球体用于遮挡/渲染调试。
// 用法：把脚本挂在场景中任意对象，选择左/右手，点击 Run On Start 或使用 ContextMenu 操作。
public class HandSphereDebug : MonoBehaviour
{
    public enum HandSide { Left, Right }

    [Header("General")]
    public HandSide hand = HandSide.Left;
    public bool runOnStart = true;
    public bool autoFollowHand = true;
    public float radius = 0.05f;

    [Header("Material Settings")]
    public Material baseMaterial; // 如果为空，会创建一个 Standard Opaque 材质
    public Color color = Color.white;
    public int renderQueue = 2000;
    public bool forceZWrite = true;

    [Header("Parenting")]
    public bool parentToAttachmentPoint = true; // 把球体 parent 到 hand 的 ObjectAttachmentPoint（模拟被拿着）

    private GameObject sphereInstance;
    private Valve.VR.InteractionSystem.Hand targetHand;

    void Start()
    {
        if (runOnStart)
            SpawnSphere();
    }

    [ContextMenu("Spawn Sphere")]
    public void SpawnSphere()
    {
        if (sphereInstance != null) DestroyImmediate(sphereInstance);

        // find player/hand
        Valve.VR.InteractionSystem.Player player = null;
        try { player = Valve.VR.InteractionSystem.Player.instance; } catch { player = null; }
        if (player == null) player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
        if (player == null)
        {
            Debug.LogWarning("HandSphereDebug: SteamVR Player not found in scene.");
            return;
        }

        targetHand = (hand == HandSide.Left) ? player.leftHand : player.rightHand;
        if (targetHand == null)
        {
            Debug.LogWarning("HandSphereDebug: target hand is null.");
            return;
        }

        sphereInstance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereInstance.name = "HandDebugSphere_" + hand.ToString();
        sphereInstance.transform.localScale = Vector3.one * radius * 2f;

        // Create or assign material
        Material mat;
        if (baseMaterial != null)
        {
            mat = new Material(baseMaterial); // instance so changes are local
        }
        else
        {
            mat = new Material(Shader.Find("Standard"));
            mat.enableInstancing = false;
        }

        // Ensure material behaves as opaque Standard so it writes depth
        EnsureStandardOpaque(mat, forceZWrite, renderQueue);

        var rend = sphereInstance.GetComponent<Renderer>();
        rend.sharedMaterial = mat;

        // disable collider so it won't interfere
        var col = sphereInstance.GetComponent<Collider>();
        if (col) DestroyImmediate(col);

        PositionSphereOnce();

        if (parentToAttachmentPoint && targetHand != null)
        {
            Transform attach = GetAttachmentTransform(targetHand);
            if (attach != null)
            {
                sphereInstance.transform.SetParent(attach, true);
            }
            else
            {
                // fallback to hand transform
                sphereInstance.transform.SetParent(targetHand.transform, true);
            }
        }
    }

    [ContextMenu("Destroy Sphere")]
    public void DestroySphere()
    {
        if (sphereInstance != null)
        {
            DestroyImmediate(sphereInstance);
            sphereInstance = null;
        }
    }

    [ContextMenu("Apply Material Settings")]
    public void ApplyMaterialSettings()
    {
        if (sphereInstance == null)
        {
            Debug.LogWarning("HandSphereDebug: no sphere to apply settings to.");
            return;
        }

        var rend = sphereInstance.GetComponent<Renderer>();
        if (rend == null) return;
        var mat = rend.sharedMaterial;
        if (mat == null) return;

        mat.color = color;
        mat.renderQueue = renderQueue;
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", forceZWrite ? 1 : 0);

        Debug.Log($"HandSphereDebug: applied material settings (queue={mat.renderQueue}, _ZWrite={(mat.HasProperty("_ZWrite")?mat.GetInt("_ZWrite").ToString():"n/a")})");
    }

    void EnsureStandardOpaque(Material mat, bool zWrite, int rq)
    {
        if (mat == null) return;
        // Try to set Standard shader to Opaque mode (if using built-in Standard)
        if (mat.shader != null && mat.shader.name.Contains("Standard"))
        {
            mat.SetFloat("_Mode", 0); // 0 = Opaque
            mat.SetOverrideTag("RenderType", "");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", zWrite ? 1 : 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // Ensure render queue: use provided rq or set to Geometry if invalid
            mat.renderQueue = rq > 0 ? rq : (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }
        else
        {
            // Fallback: force renderQueue and ZWrite if properties exist
            mat.renderQueue = rq > 0 ? rq : (int)UnityEngine.Rendering.RenderQueue.Geometry;
            if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", zWrite ? 1 : 0);
        }
    }

    [ContextMenu("Dump Info")]
    public void DumpInfo()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- HandSphereDebug Dump ---");
        sb.AppendLine("Hand side: " + hand.ToString());

        Valve.VR.InteractionSystem.Player player = null;
        try { player = Valve.VR.InteractionSystem.Player.instance; } catch { player = null; }
        if (player == null) player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
        if (player == null)
        {
            sb.AppendLine("Player not found");
            Debug.Log(sb.ToString());
            return;
        }

        var h = (hand == HandSide.Left) ? player.leftHand : player.rightHand;
        if (h == null) sb.AppendLine("Hand object null");
        else
        {
            sb.AppendLine($"hand GameObject: {h.gameObject.name}");
            var rends = h.GetComponentsInChildren<Renderer>(true);
            sb.AppendLine($"hand renderers: {rends.Length}");
            foreach (var r in rends)
            {
                sb.AppendLine($" - {r.gameObject.name} layer:{LayerMask.LayerToName(r.gameObject.layer)} enabled:{r.enabled}");
                int mi = 0;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) sb.AppendLine($"    ({mi}) material: null");
                    else sb.AppendLine($"    ({mi}) material: {m.name} shader:{m.shader.name} queue:{m.renderQueue}");
                    mi++;
                }
            }

            var held = h.currentAttachedObject;
            if (held != null)
            {
                sb.AppendLine($"held object: {held.name} layer:{LayerMask.LayerToName(held.layer)}");
                var hr = held.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine($" held renderers: {hr.Length}");
                foreach (var r in hr)
                {
                    sb.AppendLine($"  - {r.gameObject.name}");
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m == null) sb.AppendLine("     material:null");
                        else sb.AppendLine($"     mat:{m.name} shader:{m.shader.name} queue:{m.renderQueue}");
                    }
                }
            }
            else sb.AppendLine("held object: (none)");
        }

        if (sphereInstance != null)
        {
            var sr = sphereInstance.GetComponent<Renderer>();
            if (sr != null && sr.sharedMaterial != null)
            {
                var m = sr.sharedMaterial;
                sb.AppendLine($"Sphere material: {m.name} shader:{m.shader.name} queue:{m.renderQueue}");
                if (m.HasProperty("_ZWrite")) sb.AppendLine($" Sphere _ZWrite:{m.GetInt("_ZWrite")}");
            }
        }

        Debug.Log(sb.ToString());
    }

    void Update()
    {
        if (autoFollowHand && sphereInstance != null && targetHand != null)
        {
            // if parented, no need to follow; otherwise manually position
            if (!parentToAttachmentPoint || sphereInstance.transform.parent == null)
                PositionSphereOnce();
        }
    }

    void PositionSphereOnce()
    {
        if (targetHand == null || sphereInstance == null) return;
        Transform attach = GetAttachmentTransform(targetHand);
        if (attach != null)
        {
            sphereInstance.transform.position = attach.position;
            sphereInstance.transform.rotation = attach.rotation;
        }
        else
        {
            sphereInstance.transform.position = targetHand.transform.position;
            sphereInstance.transform.rotation = targetHand.transform.rotation;
        }
    }

    Transform GetAttachmentTransform(Valve.VR.InteractionSystem.Hand h)
    {
        if (h == null) return null;
        // try ObjectAttachmentPoint first
        var ho = h.transform.Find("ObjectAttachmentPoint");
        if (ho != null) return ho;
        // fallback to a few common names
        var candidate = h.transform.Find("ObjectAttachment") ?? h.transform.Find("AttachPoint") ?? h.transform.Find("Attach") ;
        if (candidate != null) return candidate;
        // last fallback: return hand.transform
        return h.transform;
    }

    void OnDisable()
    {
        // keep scene clean
#if UNITY_EDITOR
        if (sphereInstance != null) DestroyImmediate(sphereInstance);
#else
        if (sphereInstance != null) Destroy(sphereInstance);
#endif
    }
}
