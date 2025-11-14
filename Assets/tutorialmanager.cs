using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tutorial manager that spawns a single monster at a configured monster point
/// and respawns it after death. Also optionally spawns up to 3 card prefabs
/// at configured card points. Card logic is intentionally not implemented.
/// Mirrors style of BallSpawner / CardManager for inspector-driven setup.
/// </summary>
public class tutorialmanager : MonoBehaviour
{
    [Header("Points")]
    [Tooltip("Single transform used as monster spawn point")]
    public Transform monsterPoint;

    [Tooltip("Up to three card spawn points (empty gameobjects)")]
    public List<Transform> cardPoints = new List<Transform>();

    [Header("Prefabs")]
    [Tooltip("Monster prefab to spawn (should contain TargetBall component)")]
    public GameObject monsterPrefab;

    [Tooltip("Optional card prefab to place at card points (card logic not implemented)")]
    public GameObject cardPrefab;

    [Header("Monster Settings")]
    [Tooltip("HP assigned to spawned monster (TargetBall.currentHealth / maxHealth)")]
    public int monsterHP = 300;

    [Tooltip("Seconds to wait before respawning the monster after death")]
    public float respawnDelay = 2.0f;

    [Header("Card Respawn")]
    [Tooltip("Seconds to wait before a card reappears after being hit/destroyed")]
    public float cardRespawnDelay = 2.0f;

    // internal reference to current spawned monster
    private GameObject currentMonster;

    void Start()
    {
        if (monsterPrefab == null)
        {
            Debug.LogError("tutorialmanager: monsterPrefab is not assigned.");
            return;
        }

        if (monsterPoint == null)
        {
            Debug.LogError("tutorialmanager: monsterPoint is not assigned.");
            return;
        }

        // spawn initial monster
        SpawnMonster();

        // optionally spawn cards (no logic)
        SpawnCardsAtPoints();
    }

    void SpawnMonster()
    {
        Vector3 pos = monsterPoint.position;
        Quaternion rot = monsterPoint.rotation;

        currentMonster = Instantiate(monsterPrefab, pos, rot);
        currentMonster.name = "Tutorial_Monster";
        // set layer to Enemy if exists
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            currentMonster.layer = enemyLayer;

        // disable MonsterChase if present on the prefab instance
        var chase = currentMonster.GetComponent<MonsterChase>();
        if (chase != null)
        {
            chase.enabled = false;
        }

        // set TargetBall health if present
        var tb = currentMonster.GetComponent<TargetBall>();
        if (tb != null)
        {
            tb.maxHealth = monsterHP;
            tb.currentHealth = monsterHP;
        }
        else
        {
            Debug.LogWarning("tutorialmanager: spawned monster has no TargetBall component.");
        }

        // start monitoring for death to respawn
        StartCoroutine(MonitorAndRespawn(currentMonster));
    }

    IEnumerator MonitorAndRespawn(GameObject spawned)
    {
        // wait until the spawned object is destroyed
        while (spawned != null)
            yield return null;

        // optional delay before respawn
        yield return new WaitForSeconds(respawnDelay);

        SpawnMonster();
    }

    void SpawnCardsAtPoints()
    {
        if (cardPrefab == null || cardPoints == null || cardPoints.Count == 0)
            return;

        // Fixed three card types for testing: Critical, FrostBullet, FireBullet
        var fixedList = new Card.CardEffectType[] {
            Card.CardEffectType.FrostBulletTest,
            Card.CardEffectType.FireBulletTest,
            Card.CardEffectType.CriticalTest,
            Card.CardEffectType.Damage,
            Card.CardEffectType.BulletSpeed,
        };

        int count = Mathf.Min(5, cardPoints.Count);
        for (int i = 0; i < count; i++)
        {
            var p = cardPoints[i];
            if (p == null) continue;

            var cardGO = Instantiate(cardPrefab, p.position, p.rotation, transform);
            cardGO.name = $"Tutorial_Card_{i}";

            var cardComp = cardGO.GetComponent<Card>();
            if (cardComp != null)
            {
                // Initialize with null manager so original CardManager won't clear other cards.
                cardComp.Initialize(null, fixedList[i]);
            }
            else
            {
                Debug.LogWarning("tutorialmanager: spawned card prefab missing Card component.");
            }

            // Start a monitor coroutine that will respawn this card at the same point after it's destroyed
            StartCoroutine(MonitorCardAndRespawn(p, fixedList[i]));
        }
    }

    IEnumerator MonitorCardAndRespawn(Transform spawnPoint, Card.CardEffectType effect)
    {
        // Keep respawning this slot indefinitely: wait for current occupant to be destroyed, then respawn
        while (true)
        {
            // find existing card at this spawn point (a child with matching name or close position)
            GameObject existing = null;
            // simple search: look for any Card under this manager that is within small distance of spawnPoint
            var cards = GetComponentsInChildren<Card>(true);
            foreach (var c in cards)
            {
                if (c == null) continue;
                if (Vector3.Distance(c.transform.position, spawnPoint.position) < 0.1f)
                {
                    existing = c.gameObject;
                    break;
                }
            }

            // If there's an existing card, wait until it's destroyed
            while (existing != null)
            {
                if (existing == null) break;
                yield return null;
            }

            // small delay before respawn
            yield return new WaitForSeconds(cardRespawnDelay);

            // spawn replacement
            var cardGO = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            cardGO.name = $"Tutorial_Card_respawn_{spawnPoint.name}";
            var cardComp = cardGO.GetComponent<Card>();
            if (cardComp != null)
            {
                cardComp.Initialize(null, effect);
            }
            else
            {
                Debug.LogWarning("tutorialmanager: respawned card prefab missing Card component.");
            }

            // now loop to wait for this newly spawned card to be destroyed
        }
    }
}
