using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Card;

public class CardManager : MonoBehaviour
{
    /// <summary>
    /// List to store card spawn points.
    /// </summary>
    [Tooltip("Card point spawn location")] 
    public List<Transform> cardPoints = new List<Transform>();
    /// <summary>
    /// Card Prefab.
    /// </summary>
    [Tooltip("Card Prefab (Must contain CardBehavior component)")]
    public GameObject cardPrefabs;
    /// <summary>
    /// Store the Cards that the manange spawn.
    /// </summary>
    private readonly List<GameObject> _spawnedCards = new List<GameObject>();
    /// <summary>
    /// Status for checking card selection.
    /// </summary>
    private bool _awaitingSelection = false;

    /// <summary>
    /// Card selection phase.
    /// </summary>
    /// <returns></returns>
    public IEnumerator ShowAndWaitSelection()
    {
        if (cardPoints.Count == 0 || cardPrefabs == null)
        {
            Debug.LogWarning("CardManager: missing card points or prefabs. Skipping card phase.");
            yield break;
        }

        
        List<CardEffectType> all = new List<CardEffectType>
    {
        CardEffectType.DoubleShot,
        CardEffectType.FireRate,
        CardEffectType.BulletSpeed,
        CardEffectType.PlayerHP
    };
        Shuffle(all);

        ClearCards();

        int count = Mathf.Min(3, cardPoints.Count); // 生成三张或不超过点位数量
        for (int i = 0; i < count; i++)
        {
            var prefab = cardPrefabs;
            var p = cardPoints[i];
            var card = Instantiate(prefab, p.position, p.rotation, transform);
            

            var behaviour = card.GetComponent<Card>();
            if (behaviour != null)
            {
                behaviour.Initialize(this, all[i]); // 指定随机不重复的效果
            }
            else
            {
                Debug.LogWarning("CardManager: card prefab missing CardBehaviour.");
            }
            _spawnedCards.Add(card);
        }

        _awaitingSelection = true;
        while (_awaitingSelection)
            yield return null;


        // 清理未被选中的卡（选中的卡通常在行为里会自毁，这里统一兜底清理）
        ClearCards();
    }

    void ClearCards()
    {
        for (int i = 0; i < _spawnedCards.Count; i++)
        {
            if (_spawnedCards[i] != null)
                _spawnedCards[i].GetComponent<Card>().DestroyCard();
        }
        _spawnedCards.Clear();

    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void NotifySelected()
    {
        _awaitingSelection = false;
    }
}
