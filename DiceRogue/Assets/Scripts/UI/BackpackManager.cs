using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace DiceGame.UI
{
    public enum BackpackMode { Selection, ViewOnly }

    public class BackpackManager : MonoBehaviour
    {
        public GameObject backpackPanel;
        public Button openBackpackButton;
        public Button closeBackpackButton;
        public DiceSelectionUI diceSelectionUI;

        private CooldownSystem _cooldownSystem;
        private Action<List<BaseDice>> _onDiceSelected;
        private bool _isSelectionRequired;

        public void Initialize(CooldownSystem cooldownSystem, Action<List<BaseDice>> onDiceSelected)
        {
            _cooldownSystem = cooldownSystem;
            _onDiceSelected = onDiceSelected;

            if (closeBackpackButton != null)
            {
                closeBackpackButton.onClick.AddListener(HideBackpack);
            }

            if (backpackPanel != null)
            {
                backpackPanel.SetActive(false);
            }

            if (diceSelectionUI != null)
            {
                diceSelectionUI.Initialize(OnSubmitSelection);
            }
        }

        public void ToggleBackpack()
        {
            // This is now controlled by BattleController's OpenBackpackForViewing
        }

        public void ShowBackpack(BackpackMode mode)
        {
            if (diceSelectionUI != null)
            {
                diceSelectionUI.SetMode(mode);
            }

            if (closeBackpackButton != null)
            {
                closeBackpackButton.gameObject.SetActive(mode == BackpackMode.ViewOnly);
            }

            if (backpackPanel != null && !backpackPanel.activeSelf)
            {
                backpackPanel.SetActive(true);
            }
            
            RefreshDiceList();
        }

        public void HideBackpack()
        {
            if (backpackPanel != null && backpackPanel.activeSelf)
            {
                backpackPanel.SetActive(false);
            }
        }

        private void RefreshDiceList()
        {
            if (_cooldownSystem != null && diceSelectionUI != null)
            {
                var allDice = _cooldownSystem.GetAllDice();
                diceSelectionUI.DisplayDice(allDice);
            }
        }

        private void OnSubmitSelection(List<BaseDice> selectedDice)
        {
            _onDiceSelected?.Invoke(selectedDice);
            HideBackpack();
        }

        void OnDestroy()
        {
            if (openBackpackButton != null)
            {
                openBackpackButton.onClick.RemoveListener(ToggleBackpack);
            }

            if (closeBackpackButton != null)
            {
                closeBackpackButton.onClick.RemoveListener(HideBackpack);
            }
        }
    }
}
