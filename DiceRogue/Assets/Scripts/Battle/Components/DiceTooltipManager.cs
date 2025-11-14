using UnityEngine;
using TMPro;
using DiceGame.Relics;

namespace DiceGame.Core
{
    /// <summary>
    /// Unified tooltip manager for both dice and relics
    /// </summary>
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
            // Default
            Color nameColor = Color.white;
            Color rarityColor = Color.white;
            string rarityText = dice.tier.ToString();
            string nameTextColored = dice.diceName;

            switch (dice.tier)
            {
                case DiceTier.Common:
                    if (dice.cost == 0)
                    {
                        // Normal Dice
                        nameColor = Color.white;
                        rarityColor = new Color32(200, 200, 200, 255); // White
                    }
                    else
                    {
                        // Common Dice: Blue
                        nameColor = new Color32(80, 180, 255, 255);
                        rarityColor = new Color32(80, 180, 255, 255);
                    }
                    break;

                case DiceTier.Rare:
                    nameColor = new Color32(180, 100, 255, 255);     // Purple
                    rarityColor = new Color32(180, 100, 255, 255);
                    break;

                case DiceTier.Legendary:
                    // Gold
                    nameColor = new Color32(255, 215, 0, 255);  // Gold
                    rarityColor = new Color32(255, 215, 0, 255);
                    break;
            }

            nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{dice.diceName}</color>";

            descText.text = dice.description;
            
            extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>";
            //extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>   Cost: {dice.cost}";

            tooltipPanel.SetActive(true);
        }

        /// <summary>
        /// Show tooltip for a relic
        /// </summary>
        public void ShowTooltip(RelicBase relic)
        {
            if (relic == null)
            {
                Debug.LogWarning("[DiceTooltipManager] Relic is null");
                return;
            }

            Color nameColor = Color.white;
            Color rarityColor = Color.white;
            string rarityText = relic.rarity.ToString();
            string nameTextColored = relic.relicName;

            switch (relic.rarity)
            {
                case RelicRarity.Common:
                    nameColor = new Color32(143, 238, 143, 255);  // Light Green
                    rarityColor = new Color32(143, 238, 143, 255);
                    break;

                case RelicRarity.Rare:
                    nameColor = new Color32(147, 112, 219, 255);  // Purple
                    rarityColor = new Color32(147, 112, 219, 255);
                    break;

                case RelicRarity.Legendary:
                    // Gold
                    nameColor = new Color32(255, 215, 0, 255);  // Gold
                    rarityColor = new Color32(255, 215, 0, 255);
                    break;
            }

            // Set name with color
            nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{relic.relicName}</color>";

            // Set description
            descText.text = string.IsNullOrEmpty(relic.description) 
                ? "<i>No description available</i>" 
                : relic.description;

            // Set rarity info (relics don't have cost, just show rarity)
            extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>";

            tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
        }
    }
}