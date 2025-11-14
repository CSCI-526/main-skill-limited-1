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
        [Header("Legacy - kept for backward compatibility")]
        public GameObject diceButtonPrefab; // Deprecated: Use dice prefabs instead
        
        [Header("Dice Prefabs")]
        public Transform diceListContainer;
        public Button submitButton;
        
        [Header("Prefab Loading")]
        [Tooltip("Direct prefab references. Assign dice prefabs here for better performance.")]
        public DicePrefabReference[] dicePrefabReferences;
        
        [Tooltip("Path in Resources folder as fallback (e.g., 'Prefabs'). Leave empty to skip Resources loading.")]
        public string resourcesPath = "";

        [System.Serializable]
        public class DicePrefabReference
        {
            public string diceTypeName; // e.g., "D8", "BigOne"
            public GameObject prefab;
        }

        private List<BaseDice> _allDice;
        private List<BaseDice> _selectedDice = new List<BaseDice>();
        private Action<List<BaseDice>> _onSubmit;
        private Dictionary<BaseDice, DiceSelectionItem> _diceItems = new Dictionary<BaseDice, DiceSelectionItem>();
        private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();
        private const int MaxSelectionCount = 5;

        public void Initialize(Action<List<BaseDice>> onSubmit)
        {
            _onSubmit = onSubmit;
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmit);
            }
            
            // Build prefab cache from inspector references
            if (dicePrefabReferences != null)
            {
                foreach (var reference in dicePrefabReferences)
                {
                    if (reference.prefab != null && !string.IsNullOrEmpty(reference.diceTypeName))
                    {
                        _prefabCache[reference.diceTypeName] = reference.prefab;
                    }
                }
            }
        }

        public void DisplayDice(List<BaseDice> allDice)
        {
            _allDice = allDice;
            _selectedDice.Clear();
            _diceItems.Clear();

            foreach (Transform child in diceListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var dice in _allDice)
            {
                GameObject dicePrefab = LoadDicePrefab(dice);
                if (dicePrefab != null)
                {
                    GameObject diceGO = Instantiate(dicePrefab, diceListContainer);
                    var diceItem = diceGO.GetComponent<DiceSelectionItem>();
                    
                    // If DiceSelectionItem doesn't exist, add it
                    if (diceItem == null)
                    {
                        diceItem = diceGO.AddComponent<DiceSelectionItem>();
                    }
                    
                    diceItem.Setup(dice, OnDiceItemClicked);
                    _diceItems[dice] = diceItem;
                }
                else
                {
                    Debug.LogWarning($"[DiceSelectionUI] Could not load prefab for dice: {dice.GetType().Name}. Falling back to button prefab.");
                    // Fallback to button prefab if dice prefab not found
                    if (diceButtonPrefab != null)
                    {
                        GameObject buttonGO = Instantiate(diceButtonPrefab, diceListContainer);
                        var diceButton = buttonGO.GetComponent<DiceButton>();
                        if (diceButton != null)
                        {
                            diceButton.Setup(dice, OnDiceButtonClickedLegacy);
                        }
                    }
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

        private void OnDiceItemClicked(BaseDice dice, DiceSelectionItem item)
        {
            if (_selectedDice.Contains(dice))
            {
                _selectedDice.Remove(dice);
                item.SetSelected(false);
            }
            else
            {
                if (_selectedDice.Count < MaxSelectionCount)
                {
                    _selectedDice.Add(dice);
                    item.SetSelected(true);
                }
            }

            UpdateInteractableStates();
        }

        // Legacy method for backward compatibility with DiceButton
        private void OnDiceButtonClickedLegacy(BaseDice dice, DiceButton button)
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

            foreach (var pair in _diceItems)
            {
                // An item should be interactable if the limit is not reached,
                // OR if it's already selected (so it can be deselected).
                bool isSelected = _selectedDice.Contains(pair.Key);
                pair.Value.SetInteractable(!limitReached || isSelected);
            }
        }

        /// <summary>
        /// Load dice prefab by dice type name
        /// Tries: 1) Cache, 2) Inspector references, 3) Resources folder
        /// </summary>
        private GameObject LoadDicePrefab(BaseDice dice)
        {
            if (dice == null) return null;

            string prefabName = GetPrefabName(dice);
            if (string.IsNullOrEmpty(prefabName)) return null;

            // Check cache first
            if (_prefabCache.TryGetValue(prefabName, out GameObject cachedPrefab))
            {
                // Only return if not null (null means we tried and failed before)
                if (cachedPrefab != null)
                {
                    return cachedPrefab;
                }
                // If cached as null, try loading again (in case prefabs were added later)
            }

            // Try inspector references as fallback (in case Initialize wasn't called or cache wasn't populated)
            if (dicePrefabReferences != null)
            {
                foreach (var reference in dicePrefabReferences)
                {
                    if (reference.diceTypeName == prefabName && reference.prefab != null)
                    {
                        _prefabCache[prefabName] = reference.prefab;
                        return reference.prefab;
                    }
                }
            }

            // Try to load from Resources as fallback
            GameObject prefab = null;
            if (!string.IsNullOrEmpty(resourcesPath))
            {
                string resourcePath = $"{resourcesPath}/{prefabName}";
                prefab = Resources.Load<GameObject>(resourcePath);
                if (prefab != null)
                {
                    Debug.Log($"[DiceSelectionUI] Loaded prefab '{prefabName}' from Resources path: {resourcePath}");
                }
            }

            // If not found, try common Resources paths
            if (prefab == null)
            {
                string commonPath = $"Prefabs/{prefabName}";
                prefab = Resources.Load<GameObject>(commonPath);
                if (prefab != null)
                {
                    Debug.Log($"[DiceSelectionUI] Loaded prefab '{prefabName}' from Resources path: {commonPath}");
                }
            }

            // Cache the result (even if null to avoid repeated lookups)
            if (prefab != null)
            {
                _prefabCache[prefabName] = prefab;
            }
            else
            {
                // Cache null to avoid repeated failed lookups
                _prefabCache[prefabName] = null;
                Debug.LogWarning($"[DiceSelectionUI] Failed to load prefab '{prefabName}' from Resources. Tried paths: {(string.IsNullOrEmpty(resourcesPath) ? "" : $"{resourcesPath}/{prefabName}, ")}Prefabs/{prefabName}");
            }

            return prefab;
        }

        /// <summary>
        /// Get prefab name from dice type
        /// Maps dice class name to prefab name
        /// </summary>
        private string GetPrefabName(BaseDice dice)
        {
            if (dice == null) return null;

            // Get the class name (e.g., "D8", "BigOne", "ZombieDice")
            string typeName = dice.GetType().Name;

            // Handle special cases if needed
            // For now, most dice names match their prefab names directly
            return typeName;
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
