using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn balls at spawn points.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    /// <summary>
    /// Ball prefab for spawn.
    /// </summary>
    public GameObject ballPrefab;
    /// <summary>
    /// List contain the spawn points location (Transform).
    /// </summary>
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Wave settings")]
    [Tooltip("Base number of balls per wave (e.g. 5).")]
    public int baseCount = 5;

    [Tooltip("Number of waves per group before increasing count (e.g. 5).")]
    public int wavesPerGroup = 5;

    [Tooltip("Increase in ball count every group.")]
    public int increasePerGroup = 5;

    [Tooltip("Seconds to wait after a wave finishes before starting the next one.")]
    public float waitBetweenWaves = 5f;

    [Tooltip("Random spawn radius around each spawn point (0 = exact transform).")]
    public float spawnRadius = 0.5f;

    [Header("Monster settings")]
    [Tooltip("Monster Health (default in 30).")]
    public float monsterHP = 30f;
    [Tooltip("Monster Health increasment after wave group (default in 30).")]
    public float monsterIncreaseHP = 10f;
    public int CurrentWave { get; private set; } = 1;

    
    private int remainingBalls = 0;
    private bool waveInProgress = false;

    

    public Transform playerTransform;

    // Start is called before the first frame update

    public GameObject cardmanager;
    void Start()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("No ballprefab.");
            return;
        }
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points.");
            return;
        }
        if (cardmanager == null)
        {
            return;
        }
        StartCoroutine(SpawnWaveCoroutine(CurrentWave));
    }

    /// <summary>
    /// Instantiates a ball at the given spawn point.
    /// </summary>
    /// <param name="spawnPoint">spawn point location (Transform).</param>
    /// <param name="index"></param>
    //private void SpawnAt(Transform spawnPoint, int index = -1)
    //{
    //    if(spawnPoint == null)
    //    {
    //        return;
    //    }
    //    GameObject hitball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
    //    hitball.name = $"SpawnedBall_{index}";
    //    TargetBall tb = hitball.GetComponent<TargetBall>();
    //    if(tb != null)
    //    {
    //        tb.Initialize(spawnPoint.position, spawnPoint.rotation);
    //    }

    //}

    /// <summary>
    /// Computes ball count for a wave (base + group increases).
    /// </summary>
    private int GetBallCountForWave(int wave)
    {
        int group = (wave - 1) / Mathf.Max(1, wavesPerGroup);
        return baseCount + group * increasePerGroup;
    }

    /// <summary>
    /// Spawns a wave (internal coroutine to allow delay/start).
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(int wave)
    {
        waveInProgress = true;
        int count = GetBallCountForWave(wave);
        remainingBalls = count;

        for (int i = 0; i < count; i++)
        {
            Transform anchor = spawnPoints[i % spawnPoints.Count];
            Vector3 pos = anchor.position;
            if (spawnRadius > 0f)
            {
                pos += (Random.insideUnitSphere * spawnRadius);
                pos.y = anchor.position.y; // keep ground level if you want
            }

            GameObject go = Instantiate(ballPrefab, pos, anchor.rotation);
            go.layer = LayerMask.NameToLayer("Enemy");
            go.name = $"Wave{wave}_Ball_{i}";
            go.GetComponent<MonsterChase>().player = playerTransform;
            TargetBall tb = go.GetComponent<TargetBall>();
            if (tb != null)
            {
                tb.SetSpawner(this);
                tb.currentHealth = monsterHP;
                tb.maxHealth = monsterHP;
            }
            else
            {
                Debug.LogWarning("BallWaveSpawner: spawned prefab has no TargetBall component.");
            }
        }

        yield break;
    }

    private IEnumerator NextWaveDelayAndSpawn()
    {

        // Enter Card selection after a group wave.
        if ((CurrentWave % Mathf.Max(1, wavesPerGroup) == 0))
        {
            yield return cardmanager.GetComponent<CardManager>().ShowAndWaitSelection();
            monsterHP += monsterIncreaseHP;
        }

        // wait for configured delay
        yield return new WaitForSeconds(3f);

        // increment wave and spawn next
        CurrentWave++;
        yield return StartCoroutine(SpawnWaveCoroutine(CurrentWave));
        waveInProgress = true;
    }

    // Optional public method to force restart or set wave
    public void StartAtWave(int wave)
    {
        // stop any current wave (note: doesn't destroy existing balls)
        CurrentWave = Mathf.Max(1, wave);
        StopAllCoroutines();
        // optionally destroy existing spawned balls
        // then start
        StartCoroutine(SpawnWaveCoroutine(CurrentWave));
    }

    /// <summary>
    /// Called by TargetBall when it is destroyed/hit.
    /// </summary>
    /// <param name="tb">The target ball that was hit (may be already destroyed).</param>
    public void NotifyBallDestroyed(TargetBall tb)
    {
        remainingBalls = Mathf.Max(0, remainingBalls - 1);
        // optional debug
        Debug.Log($"Ball destroyed. Remaining: {remainingBalls}");

        if (remainingBalls == 0 && waveInProgress)
        {
            // wave finished
            waveInProgress = false;
            StartCoroutine(NextWaveDelayAndSpawn());
        }
    }
}