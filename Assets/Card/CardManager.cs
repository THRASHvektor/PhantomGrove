using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Card;

public class CardManager : MonoBehaviour
{
    [Tooltip("卡牌摆放点（CardPoint 子物件）")] 
    public List<Transform> cardPoints = new List<Transform>();

    [Tooltip("卡牌预制体（预制体需带 CardBehaviour 组件）")]
    public GameObject cardPrefabs;

    // 选中卡牌时派发（解耦点：外部决定如何处理效果）
    //public event Action<CardEffectSO> OnCardSelected;

    private readonly List<GameObject> _spawnedCards = new List<GameObject>();
    private bool _awaitingSelection = false;

    public IEnumerator ShowAndWaitSelection()
    {
        if (cardPoints.Count == 0 || cardPrefabs == null)
        {
            Debug.LogWarning("CardManager: missing card points or prefabs. Skipping card phase.");
            yield break;
        }

        // 随机不重复选择卡面
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
