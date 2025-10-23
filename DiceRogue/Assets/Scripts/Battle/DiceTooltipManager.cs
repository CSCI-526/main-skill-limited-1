using UnityEngine;
using TMPro;

namespace DiceGame.Core
{
    public class DiceTooltipManager : MonoBehaviour
    {
        public static DiceTooltipManager Instance;

        public GameObject tooltipPanel;
        public TMP_Text nameText;
        public TMP_Text descText;
        public TMP_Text extraText;

        private RectTransform panelRect;

        private void Awake()
        {
            Instance = this;
            panelRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipPanel.SetActive(false);
        }

        private void Update()
        {
            if (tooltipPanel.activeSelf)
            {
                Vector2 pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform as RectTransform,
                    Input.mousePosition, null, out pos);
                panelRect.anchoredPosition = pos + new Vector2(20f, -20f);
            }
        }

        public void ShowTooltip(BaseDice dice)
        {
            nameText.text = dice.diceName;
            descText.text = dice.description;
            extraText.text = $"Rarity: {dice.tier}   Cost: {dice.cost}";
            tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
        }
    }
}