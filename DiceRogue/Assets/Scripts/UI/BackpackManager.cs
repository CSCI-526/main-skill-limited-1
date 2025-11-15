using System.Collections.Generic;
using System.Linq;
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
        private DiceManager _diceManager;  // Dice manager for accessing player backpack
        private Action<List<BaseDice>> _onDiceSelected;
        private bool _isSelectionRequired;

        public void Initialize(CooldownSystem cooldownSystem, Action<List<BaseDice>> onDiceSelected)
        {
            _cooldownSystem = cooldownSystem;
            _diceManager = null; // No DiceManager for backward compatibility
            _onDiceSelected = onDiceSelected;
            InitializeInternal();
        }

        /// <summary>
        /// Initialize with DiceManager for enhanced backpack functionality
        /// </summary>
        /// <param name="cooldownSystem">Cooldown system for getting available dice</param>
        /// <param name="diceManager">Dice manager for accessing player backpack</param>
        /// <param name="onDiceSelected">Callback when dice are selected</param>
        public void Initialize(CooldownSystem cooldownSystem, DiceManager diceManager, Action<List<BaseDice>> onDiceSelected)
        {
            _cooldownSystem = cooldownSystem;
            _diceManager = diceManager;
            _onDiceSelected = onDiceSelected;
            InitializeInternal();
        }

        private void InitializeInternal()
        {
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
            if (diceSelectionUI == null)
            {
                return;
            }

            List<BaseDice> diceToDisplay = null;

            // Determine which dice to display based on mode
            if (diceSelectionUI != null)
            {
                // Check current mode by checking if submit button is active
                bool isSelectionMode = diceSelectionUI.submitButton != null && diceSelectionUI.submitButton.gameObject.activeSelf;

                if (isSelectionMode)
                {
                    // Selection mode: show available dice from CooldownSystem (for hand selection)
                    // Filter out NormalDice - they are temporary fillers and shouldn't be selectable
                    if (_cooldownSystem != null)
                    {
                        diceToDisplay = _cooldownSystem.GetAvailableDice()
                            .Where(d => !(d is NormalDice))
                            .ToList();
                        Debug.Log($"[BackpackManager] Selection mode: Displaying {diceToDisplay.Count} available dice from CooldownSystem (NormalDice filtered out)");
                    }
                }
                else
                {
                    // ViewOnly mode: show all dice from player backpack (if DiceManager available)
                    if (_diceManager != null)
                    {
                        diceToDisplay = _diceManager.PlayerDiceBackpack.ToList();
                        Debug.Log($"[BackpackManager] ViewOnly mode: Displaying {diceToDisplay.Count} dice from player backpack");
                    }
                    else if (_cooldownSystem != null)
                    {
                        // Fallback: show all dice from CooldownSystem if DiceManager not available
                        // Filter out NormalDice - they are temporary fillers
                        diceToDisplay = _cooldownSystem.GetAllDice()
                            .Where(d => !(d is NormalDice))
                            .ToList();
                        Debug.Log($"[BackpackManager] ViewOnly mode (fallback): Displaying {diceToDisplay.Count} dice from CooldownSystem (NormalDice filtered out)");
                    }
                }
            }

            // Fallback: if mode detection failed, use CooldownSystem
            // Filter out NormalDice - they are temporary fillers
            if (diceToDisplay == null && _cooldownSystem != null)
            {
                diceToDisplay = _cooldownSystem.GetAllDice()
                    .Where(d => !(d is NormalDice))
                    .ToList();
                Debug.Log($"[BackpackManager] Fallback: Displaying {diceToDisplay.Count} dice from CooldownSystem (NormalDice filtered out)");
            }

            // Display dice
            if (diceToDisplay != null)
            {
                diceSelectionUI.DisplayDice(diceToDisplay);
            }
            else
            {
                Debug.LogWarning("[BackpackManager] Cannot refresh dice list - no data source available");
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
