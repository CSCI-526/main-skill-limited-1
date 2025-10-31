using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace DiceGame.UI
{
    public class DiceButton : MonoBehaviour
    {
        public TMP_Text diceNameText;
        public Button button;
        public Image selectionIndicator;

        private BaseDice _dice;
        private Action<BaseDice, DiceButton> _onClick;

        public void Setup(BaseDice dice, Action<BaseDice, DiceButton> onClick)
        {
            _dice = dice;
            _onClick = onClick;

            string displayText = dice.diceName;
            bool isInteractable = dice.cooldownRemain == 0;

            if (!isInteractable)
            {
                displayText += " (in cooldown)";
            }

            if (diceNameText != null)
            {
                diceNameText.text = displayText;
            }

            if (button != null)
            {
                button.interactable = isInteractable;
                button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.enabled = isSelected;
            }
        }

        public void SetInteractable(bool interactable)
        {
            // Only allow making a button interactable if the dice is not on cooldown
            if (button != null && _dice.cooldownRemain == 0)
            {
                button.interactable = interactable;
            }
        }

        private void HandleClick()
        {
            _onClick?.Invoke(_dice, this);
        }

        void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}
