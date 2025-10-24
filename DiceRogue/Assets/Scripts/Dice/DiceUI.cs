using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Core;

namespace DiceGame.Core
{
    public class DiceUI : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text nameText;
        public TMP_Text rarityText;
        public TMP_Text costText;
        public TMP_Text descriptionText;
        public Image background;

        public void SetData(BaseDice dice)
        {
            // Text
            descriptionText.text = dice.description;
            costText.text = $"Cost: {dice.cost}";

            // Default
            Color nameColor = Color.white;
            Color rarityColor = Color.white;
            string rarityTextContent = dice.tier.ToString();
            string nameRich = dice.diceName;

            switch (dice.tier)
            {
                case DiceTier.Common:
                    if (dice.cost == 0) // Normal Dice
                    {
                        nameColor = Color.white;
                        rarityColor = new Color32(200, 200, 200, 255);
                        if (background) background.color = new Color32(30, 55, 75, 200);
                    }
                    else
                    {
                        nameColor = new Color32(80, 180, 255, 255);   // Blue
                        rarityColor = new Color32(80, 180, 255, 255);
                        if (background) background.color = new Color32(25, 60, 90, 200);
                    }
                    break;

                case DiceTier.Rare:
                    nameColor = new Color32(180, 100, 255, 255);     // Purple
                    rarityColor = new Color32(180, 100, 255, 255);
                    if (background) background.color = new Color32(45, 25, 75, 200);
                    break;

                case DiceTier.Legendary:
                    nameRich = Rainbowify(dice.diceName);
                    rarityTextContent = Rainbowify("Legendary");
                    if (background) background.color = new Color32(60, 35, 0, 200);
                    break;
            }

            if (dice.tier == DiceTier.Legendary)
                nameText.text = nameRich;
            else
                nameText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{dice.diceName}</color>";

            rarityText.text = $"Rarity: <color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityTextContent}</color>";
        }


        private string Rainbowify(string text)
        {
            string[] colors = { "#FFD700", "#FF8C00", "#FF4500", "#FF1493", "#9400D3", "#4B0082", "#1E90FF", "#00CED1", "#32CD32" };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                string c = colors[i % colors.Length];
                sb.Append($"<color={c}>{text[i]}</color>");
            }
            return sb.ToString();
        }
    }
}