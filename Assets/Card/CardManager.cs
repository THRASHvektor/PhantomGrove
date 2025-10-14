using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;


public class CardManager : MonoBehaviour
{
    public GameObject card;
    // Start is called before the first frame update
    public void OnCardClicked(Card clickedCard)
    {
        
        // 获取所有子物体中的卡牌组件
        Card[] childCards = GetComponentsInChildren<Card>();

        foreach (Card card in childCards)
        {
            if (card != clickedCard && card != null)
            {
                card.DestroyCard();
            }
        }
    }
    void Start()
    {
        GameObject leftCard = Instantiate(card,transform);
        GameObject midCard = Instantiate(card, transform);
        GameObject rightCard = Instantiate(card, transform);
        leftCard.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
        midCard.transform.localPosition = new Vector3(0f, 0f, 0f);
        rightCard.transform.localPosition = new Vector3(1.5f, 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
