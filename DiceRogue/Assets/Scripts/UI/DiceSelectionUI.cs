using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

namespace DiceGame.UI
{
    public class DiceSelectionUI : MonoBehaviour
    {
        public GameObject diceButtonPrefab;
        public Transform diceListContainer;
        public Button submitButton;

        private List<BaseDice> _allDice;
        private List<BaseDice> _selectedDice = new List<BaseDice>();
        private Action<List<BaseDice>> _onSubmit;
        private Dictionary<BaseDice, DiceButton> _diceButtons = new Dictionary<BaseDice, DiceButton>();
        private const int MaxSelectionCount = 5;

        public void Initialize(Action<List<BaseDice>> onSubmit)
        {
            _onSubmit = onSubmit;
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmit);
            }
        }

        public void DisplayDice(List<BaseDice> allDice)
        {
            _allDice = allDice;
            _selectedDice.Clear();
            _diceButtons.Clear();

            foreach (Transform child in diceListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var dice in _allDice)
            {
                GameObject buttonGO = Instantiate(diceButtonPrefab, diceListContainer);
                var diceButton = buttonGO.GetComponent<DiceButton>();
                if (diceButton != null)
                {
                    diceButton.Setup(dice, OnDiceButtonClicked);
                    _diceButtons[dice] = diceButton;
                }
            }
        }

        public void SetMode(BackpackMode mode)
        {
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(mode == BackpackMode.Selection);
            }
        }

        private void OnDiceButtonClicked(BaseDice dice, DiceButton button)
        {
            if (_selectedDice.Contains(dice))
            {
                _selectedDice.Remove(dice);
                button.SetSelected(false);
            }
            else
            {
                if (_selectedDice.Count < MaxSelectionCount)
                {
                    _selectedDice.Add(dice);
                    button.SetSelected(true);
                }
            }

            UpdateInteractableStates();
        }

        private void UpdateInteractableStates()
        {
            bool limitReached = _selectedDice.Count >= MaxSelectionCount;

            foreach (var pair in _diceButtons)
            {
                // A button should be interactable if the limit is not reached,
                // OR if it's already selected (so it can be deselected).
                bool isSelected = _selectedDice.Contains(pair.Key);
                pair.Value.SetInteractable(!limitReached || isSelected);
            }
        }

        private void OnSubmit()
        {
            _onSubmit?.Invoke(_selectedDice);
        }

        void OnDestroy()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmit);
            }
        }
    }
}
