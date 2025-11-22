using UnityEngine;
using TMPro;
using DiceGame.Relics;
using UnityEngine.UI;

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

                RectTransform canvasRect = transform as RectTransform;
                Vector2 tooltipSize = panelRect.sizeDelta;
                Vector2 offset = new Vector2(20f, -20f);
                Vector2 anchoredPos = pos + offset;

                float mouseXNorm = Input.mousePosition.x / Screen.width;
                float mouseYNorm = Input.mousePosition.y / Screen.height;

                if (mouseXNorm > 0.7f)
                    anchoredPos.x -= tooltipSize.x + 40f;
                if (mouseYNorm < 0.3f)
                    anchoredPos.y += tooltipSize.y + 40f;

                float maxX = canvasRect.rect.width / 2f - 10f;
                float minX = -maxX;
                float maxY = canvasRect.rect.height / 2f - 10f;
                float minY = -maxY;

                anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
                anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

                panelRect.anchoredPosition = anchoredPos;
            }
        }




        public void ShowTooltip(BaseDice dice)
        {
            descText.enableWordWrapping = true;
            descText.overflowMode = TextOverflowModes.Overflow;
            descText.rectTransform.sizeDelta = new Vector2(400, descText.rectTransform.sizeDelta.y);

            // 确保宽度限制存在
            var le = descText.GetComponent<LayoutElement>();
            if (le == null) le = descText.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 400f;

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
                    // Rainbow
                    string rainbow = "<color=#FFD700>L</color><color=#FF8C00>e</color><color=#FF4500>g</color><color=#FF1493>e</color><color=#9400D3>n</color><color=#4B0082>d</color><color=#1E90FF>a</color><color=#00CED1>r</color><color=#32CD32>y</color>";
                    rarityText = rainbow;
                    nameTextColored = Rainbowify(dice.diceName);
                    break;
            }

            if (dice.tier != DiceTier.Legendary)
                nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{dice.diceName}</color>";
            else
                nameText.text = nameTextColored;

            descText.text = dice.description;
            //extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>   Cost: {dice.cost}";
            extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>";



            tooltipPanel.SetActive(true);

            // 强制刷新全部子布局（关键）
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel.transform as RectTransform);
        }

        private string Rainbowify(string text)
        {
            string[] colors = { "#FFD700", "#FF8C00", "#FF4500", "#FF1493", "#9400D3", "#1E90FF", "#00CED1", "#32CD32" };
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                string c = colors[i % colors.Length];
                sb.Append($"<color={c}>{text[i]}</color>");
            }
            return sb.ToString();
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