using System.Collections.Generic;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Factory for creating and managing DiceView instances
    /// Handles view creation, binding, and cleanup
    /// </summary>
    public class DiceViewFactory
    {
        private readonly GameObject _diceViewPrefab;
        private readonly Transform _parentTransform;

        public DiceViewFactory(GameObject prefab, Transform parent)
        {
            _diceViewPrefab = prefab;
            _parentTransform = parent;
        }

        /// <summary>
        /// Create views for a list of dice, filling empty slots with placeholders
        /// </summary>
        public List<DiceView> CreateViews(List<BaseDice> dice, int totalSlots)
        {
            var views = new List<DiceView>();

            // Create views for actual dice
            foreach (var die in dice)
            {
                var view = CreateView(die);
                views.Add(view);
            }

            // Fill remaining slots with placeholder views
            while (views.Count < totalSlots)
            {
                var placeholderView = CreatePlaceholderView(views.Count + 1);
                views.Add(placeholderView);
            }

            Debug.Log($"[DiceViewFactory] Created {dice.Count} dice views + {totalSlots - dice.Count} placeholder views");
            return views;
        }

        /// <summary>
        /// Create a single view for a dice
        /// </summary>
        private DiceView CreateView(BaseDice dice)
        {
            var go = Object.Instantiate(_diceViewPrefab, _parentTransform);
            var view = go.GetComponent<DiceView>();
            view.Bind(dice);
            return view;
        }

        /// <summary>
        /// Create a placeholder view for empty slot
        /// </summary>
        private DiceView CreatePlaceholderView(int slotNumber)
        {
            var go = Object.Instantiate(_diceViewPrefab, _parentTransform);
            var view = go.GetComponent<DiceView>();
            
            // Create a placeholder dice for display
            var placeholderDice = new NormalDice
            {
                diceName = $"Empty_{slotNumber}",
                tier = DiceTier.Filler,
                cost = 0,
                lastRollValue = 0,
                isLocked = false
            };
            
            view.Bind(placeholderDice);
            view.SetDisplayValue("-"); // Show "-" instead of 0
            return view;
        }

        /// <summary>
        /// Destroy all views in the list
        /// </summary>
        public void DestroyViews(List<DiceView> views)
        {
            foreach (var view in views)
            {
                if (view != null && view.gameObject != null)
                {
                    Object.Destroy(view.gameObject);
                }
            }
            
            views.Clear();
            Debug.Log("[DiceViewFactory] All views destroyed");
        }

        /// <summary>
        /// Refresh all views to show current dice state
        /// </summary>
        public void RefreshViews(List<DiceView> views)
        {
            foreach (var view in views)
            {
                if (view != null)
                {
                    view.Refresh();
                }
            }
        }
    }
}

