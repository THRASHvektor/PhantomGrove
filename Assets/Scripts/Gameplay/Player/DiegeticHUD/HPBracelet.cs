using UnityEngine;


public class HPBracelet : MonoBehaviour
{
    
    public PlayerHealth playerHealth;   
    public PlayerState playerState;    

   
    public Renderer braceletRenderer;  

   
    public string ratioProp = "_BlendPos";

   
    public bool pollEveryFrame = true;

    private Material[] mats;
    private int idRatio;

    void Awake()
    {
        
        if (!playerHealth) playerHealth = GetComponentInParent<PlayerHealth>() ?? FindObjectOfType<PlayerHealth>(true);
        if (!playerState) playerState = GetComponentInParent<PlayerState>();

        if (!braceletRenderer)
            braceletRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>(true);

      

        mats = braceletRenderer.materials;  
        idRatio = Shader.PropertyToID(ratioProp);
    }

    void Start()
    {
        ApplyRatio(GetHealthRatio());

        EventDispatcher<string>.addListener(GameEvents.Gameplay.Events.UpdateHPDisplay, OnHPEvent);
    }

    void OnDestroy()
    {
        EventDispatcher<string>.removeListener(GameEvents.Gameplay.Events.UpdateHPDisplay, OnHPEvent);
    }

    void OnHPEvent(string _)
    {
        ApplyRatio(GetHealthRatio());
    }

    void Update()
    {
        if (pollEveryFrame)
            ApplyRatio(GetHealthRatio());
    }

    public void SetHealthRatio(float ratio01)
    {
        ApplyRatio(Mathf.Clamp01(ratio01));
    }

    float GetHealthRatio()
    {
        if (playerHealth)
        {
            float cur = playerHealth.currentHealth;
            float max = Mathf.Max(1, playerHealth.maxHealth);
            return Mathf.Clamp01(cur / max);
        }
        if (playerState)
        {
            float cur = (float)playerState.healthCurrent;
            float max = Mathf.Max(0.0001f, (float)playerState.healthMax);
            return Mathf.Clamp01(cur / max);
        }
        return 1f;
    }

  
    void ApplyRatio(float ratio)
    {
        if (mats == null || mats.Length == 0) return;
        for (int i = 0; i < mats.Length; i++)
        {
            var m = mats[i];
            if (m && m.HasProperty(idRatio))
                m.SetFloat(idRatio, ratio);
        }
    }
}

