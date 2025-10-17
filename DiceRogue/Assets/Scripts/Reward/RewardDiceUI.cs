using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // ← 記得加這個！

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

    public void Bind(RewardOption opt, System.Action onClick)
    {
        titleText.text = opt.displayName;
        effectText.text = opt.effectText;
        costText.text = $"Cost {opt.cost}";

        switch (opt.type)
        {
            case DiceType.Common: background.color = new Color32(230, 230, 230, 255); break;
            case DiceType.Rare: background.color = new Color32(179, 217, 255, 255); break;
            case DiceType.Legendary: background.color = new Color32(255, 229, 153, 255); break;
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