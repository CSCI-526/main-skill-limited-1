using UnityEngine;
using System.Collections.Generic;

public class RewardSceneManager : MonoBehaviour
{
    public RewardDiceUI cardPrefab;
    public Transform cardsParent;

    private List<RewardDiceUI> spawnedCards = new List<RewardDiceUI>();
    private RewardDiceUI selectedCard = null;

    void Start()
    {
        var options = GenerateThreeOptions();
        foreach (var opt in options)
        {
            var card = Instantiate(cardPrefab, cardsParent);
            card.Bind(opt, () => OnChoose(card));
            card.SetHighlight(false); // 預設不亮
            spawnedCards.Add(card);
        }
    }

    void OnChoose(RewardDiceUI chosenCard)
    {
        // 取消其他卡的外框
        foreach (var card in spawnedCards)
        {
            card.SetHighlight(false);
        }

        // 打開這張卡的外框
        chosenCard.SetHighlight(true);
        selectedCard = chosenCard;

        Debug.Log($"[Reward] 你選了 {chosenCard.titleText.text}");
    }

    List<RewardOption> GenerateThreeOptions()
    {
        var list = new List<RewardOption>();
        for (int i = 0; i < 3; i++)
        {
            var t = (DiceType)Random.Range(0, 3);
            switch (t)
            {
                case DiceType.Common:
                    list.Add(new RewardOption(t, "Common Die", "Balanced D6 (1–6). No special effect.", 1));
                    break;
                case DiceType.Rare:
                    list.Add(new RewardOption(t, "Rare Die", "Slightly favors 4–6. More consistent highs.", 2));
                    break;
                case DiceType.Legendary:
                    list.Add(new RewardOption(t, "Legendary Die", "High variance (spikes). Risk vs reward.", 3));
                    break;
            }
        }
        return list;
    }
}