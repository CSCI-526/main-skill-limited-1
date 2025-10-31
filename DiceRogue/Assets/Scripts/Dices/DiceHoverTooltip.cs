using UnityEngine;
using TMPro;

namespace DiceGame.Core
{
    public class DiceHoverTooltip : MonoBehaviour
    {
        public static DiceHoverTooltip Instance;

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
            extraText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityText}</color>   Cost: {dice.cost}";

            tooltipPanel.SetActive(true);
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


        public void HideTooltip()
        {
            tooltipPanel.SetActive(false);
        }
    }
}