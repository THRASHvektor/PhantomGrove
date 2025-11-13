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
    private Card clickedCard;

    /// <summary>
    /// Card selection phase.
    /// </summary>
    /// <returns></returns>
    public IEnumerator ShowAndWaitSelection()
    {
        // Prevent duplicate selection phases running at the same time
        if (_awaitingSelection)
        {
            Debug.LogWarning("CardManager: ShowAndWaitSelection called while selection already in progress. Ignoring.");
            yield break;
        }

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
        //CardEffectType.PlayerHP
        CardEffectType.Damage,
        CardEffectType.FrostBullet,
        CardEffectType.FireBullet,
        CardEffectType.Critical,
    };
        Shuffle(all);

        ClearCards(clickedCard);

        int count = Mathf.Min(3, cardPoints.Count); // �������Ż򲻳�����λ����
        for (int i = 0; i < count; i++)
        {
            var prefab = cardPrefabs;
            var p = cardPoints[i];
            var card = Instantiate(prefab, p.position, p.rotation, transform);
            

            var behaviour = card.GetComponent<Card>();
            if (behaviour != null)
            {
                behaviour.Initialize(this, all[i]); // ָ��������ظ���Ч��
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
        
        ClearCards(clickedCard);
    }

    void ClearCards(Card clickedCard)
    {
        Card[] childCards = GetComponentsInChildren<Card>();

        foreach (Card card in childCards)
        {
            if (card != clickedCard && card != null)
            {
                card.DestroyCard();
            }
        }
        _spawnedCards.Clear();

    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void NotifySelected(Card selectedCard)
    {
        // Only accept selection if we are actually waiting for one
        if (!_awaitingSelection)
        {
            Debug.LogWarning("CardManager: NotifySelected called but no selection is awaited. Ignoring.");
            return;
        }

        _awaitingSelection = false;
        clickedCard = selectedCard;

        // Immediately prevent other cards from being selectable: disable their colliders and mark them selected
        Card[] childCards = GetComponentsInChildren<Card>();
        foreach (Card card in childCards)
        {
            if (card == null || card == selectedCard) continue;

            // mark as selected to prevent their OnTriggerEnter handler from doing anything
            card.isSelect = true;

            // disable any colliders on the card so bullets won't hit them
            foreach (var col in card.GetComponentsInChildren<Collider>(true))
            {
                if (col != null) col.enabled = false;
            }

            // start fade/destroy sequence for non-selected cards
            card.DestroyCard();
        }
    }
}
