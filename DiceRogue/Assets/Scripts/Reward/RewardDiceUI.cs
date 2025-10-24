using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // ← 記得加這個！
using DiceGame;

public class RewardDiceUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Bind in Inspector")]
    public Image background;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI effectText;
    public TextMeshProUGUI costText;
    public Button button;

    [Header("Highlight")]
    public GameObject highlightBorder;

    private bool isSelected = false;  // 玩家點選狀態

    public void Bind(BaseDice dice, System.Action onClick)
    {
        titleText.text = dice.diceName;

        effectText.text = dice.description;
        costText.text = $"Cost: {dice.cost}";

        switch (dice.tier)
        {
            case DiceTier.Common: background.color = new Color32(230, 230, 230, 255); break;
            case DiceTier.Rare: background.color = new Color32(179, 217, 255, 255); break;
            case DiceTier.Legendary: background.color = new Color32(255, 229, 153, 255); break;
            default: background.color = Color.white; break;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            isSelected = true;
            onClick?.Invoke();
        });

        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.SetActive(active);
    }

    // 👇 滑鼠移入時
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected) SetHighlight(true);
    }

    // 👇 滑鼠移出時
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected) SetHighlight(false);
    }

    // 如果你要在點選後取消 hover 效果
    public void SelectCard()
    {
        isSelected = true;
        SetHighlight(true);
    }

    public void DeselectCard()
    {
        isSelected = false;
        SetHighlight(false);
    }
}