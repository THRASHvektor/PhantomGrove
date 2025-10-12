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

    // Start is called before the first frame update
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
        // spawn one ball for each points at start.
        for(int i = 0; i < spawnPoints.Count; i++)
        {
            SpawnAt(spawnPoints[i], i);
        }
    }

    /// <summary>
    /// Instantiates a ball at the given spawn point.
    /// </summary>
    /// <param name="spawnPoint">spawn point location (Transform).</param>
    /// <param name="index"></param>
    private void SpawnAt(Transform spawnPoint, int index = -1)
    {
        if(spawnPoint == null)
        {
            return;
        }
        GameObject hitball = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        hitball.name = $"SpawnedBall_{index}";
        TargetBall tb = hitball.GetComponent<TargetBall>();
        if(tb != null)
        {
            tb.Initialize(spawnPoint.position, spawnPoint.rotation);
        }

    }
}
