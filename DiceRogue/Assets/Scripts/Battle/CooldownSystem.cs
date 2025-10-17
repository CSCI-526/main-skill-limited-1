using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Manages 8-dice pool rotation with cooldown system
    /// - Tracks 8-dice pool with 1-turn cooldown after use
    /// - Hand counter (5 hands max)
    /// - Auto refresh when all hands used
    /// - Provides available dice for selection
    /// </summary>
    public class CooldownSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxDicePool = 8;
        [SerializeField] private int maxHands = 5;
        [SerializeField] private int cooldownTurns = 1;
        
        [Header("Debug Info")]
        [SerializeField] private int currentHandCount = 0;
        [SerializeField] private int handsRemaining = 5;
        
        // Core data
        private readonly List<BaseDice> _dicePool = new();
        private readonly List<BaseDice> _selectedDice = new();
        private readonly List<BaseDice> _availableDice = new();
        
        // State tracking
        private bool _isInitialized = false;
        
        /// <summary>
        /// Event triggered when dice pool needs refresh (all hands used)
        /// </summary>
        public System.Action OnDicePoolRefresh;
        
        /// <summary>
        /// Event triggered when hand counter updates
        /// </summary>
        public System.Action<int, int> OnHandCounterUpdate; // (current, remaining)
        
        /// <summary>
        /// Event triggered when dice availability changes
        /// </summary>
        public System.Action<List<BaseDice>> OnAvailableDiceChanged;

        void Awake()
        {
            InitializeDicePool();
        }

        /// <summary>
        /// Initialize the 8-dice pool from player's backpack
        /// If backpack has fewer than 8 dice, fill with normal dice
        /// </summary>
        private void InitializeDicePool()
        {
            _dicePool.Clear();
            
            // TODO: Get dice from player's backpack/inventory system
            // For now, simulate a backpack with some dice
            var backpackDice = GetPlayerBackpackDice();
            
            // Add backpack dice to pool
            foreach (var dice in backpackDice)
            {
                _dicePool.Add(dice);
            }
            
            // Fill remaining slots with normal dice to reach 8 total
            while (_dicePool.Count < maxDicePool)
            {
                var normalDice = new NormalDice
                {
                    diceName = $"Basic D6_{_dicePool.Count + 1}",
                    tier = DiceTier.Common,
                    cost = 1,
                    cooldownAfterUse = cooldownTurns,
                    cooldownRemain = 0
                };
                _dicePool.Add(normalDice);
            }
            
            UpdateAvailableDice();
            _isInitialized = true;
            
            Debug.Log($"[CooldownSystem] Initialized with {_dicePool.Count} dice in pool ({backpackDice.Count} from backpack)");
            LogDicePool();
        }

        /// <summary>
        /// Get dice from player's backpack/inventory
        /// Uses DicePoolFactory to create a random pool of 8 dice from all available types
        /// </summary>
        private List<BaseDice> GetPlayerBackpackDice()
        {
            // Use DicePoolFactory to create random pool of 8 dice
            Debug.Log("[CooldownSystem] Generating random dice pool from all available dice types...");
            var backpackDice = DicePoolFactory.CreateRandomPool(maxDicePool, cooldownTurns);
            
            return backpackDice;
        }

        /// <summary>
        /// Set dice from player's backpack/inventory system
        /// Call this method to provide dice from the inventory system
        /// </summary>
        /// <param name="backpackDice">Dice from player's inventory</param>
        public void SetPlayerBackpackDice(List<BaseDice> backpackDice)
        {
            if (backpackDice == null)
            {
                Debug.LogWarning("[CooldownSystem] Backpack dice list is null, using default");
                return;
            }

            Debug.Log($"[CooldownSystem] Setting dice pool from backpack: {backpackDice.Count} dice");
            
            // Clear current pool
            _dicePool.Clear();
            
            // Add backpack dice to pool
            foreach (var dice in backpackDice)
            {
                // Ensure cooldown settings are correct
                dice.cooldownAfterUse = cooldownTurns;
                dice.cooldownRemain = 0;
                _dicePool.Add(dice);
            }
            
            // Fill remaining slots with normal dice to reach 8 total
            while (_dicePool.Count < maxDicePool)
            {
                var normalDice = new NormalDice
                {
                    diceName = $"Basic D6_{_dicePool.Count + 1}",
                    tier = DiceTier.Common,
                    cost = 1,
                    cooldownAfterUse = cooldownTurns,
                    cooldownRemain = 0
                };
                _dicePool.Add(normalDice);
            }
            
            UpdateAvailableDice();
            Debug.Log($"[CooldownSystem] Dice pool updated: {_dicePool.Count} total ({backpackDice.Count} from backpack)");
            LogDicePool();
        }

        /// <summary>
        /// Get currently available dice (not on cooldown)
        /// </summary>
        public List<BaseDice> GetAvailableDice()
        {
            return new List<BaseDice>(_availableDice);
        }

        /// <summary>
        /// Get all dice in the pool (including on cooldown)
        /// </summary>
        public List<BaseDice> GetAllDice()
        {
            return new List<BaseDice>(_dicePool);
        }

        /// <summary>
        /// Select dice for current hand (up to 5 dice)
        /// </summary>
        /// <param name="selectedDice">List of dice to select</param>
        /// <returns>True if selection is valid</returns>
        public bool SelectDiceForHand(List<BaseDice> selectedDice)
        {
            if (selectedDice == null || selectedDice.Count == 0)
            {
                Debug.LogWarning("[CooldownSystem] Cannot select empty dice list");
                return false;
            }

            if (selectedDice.Count > 5)
            {
                Debug.LogWarning("[CooldownSystem] Cannot select more than 5 dice");
                return false;
            }

            // Validate all selected dice are available
            foreach (var dice in selectedDice)
            {
                if (!_availableDice.Contains(dice))
                {
                    Debug.LogWarning($"[CooldownSystem] Dice {dice.diceName} is not available (on cooldown)");
                    return false;
                }
            }

            _selectedDice.Clear();
            _selectedDice.AddRange(selectedDice);
            
            Debug.Log($"[CooldownSystem] Selected {_selectedDice.Count} dice for current hand");
            foreach (var dice in _selectedDice)
            {
                Debug.Log($"  - {dice.diceName} ({dice.tier}, cost: {dice.cost})");
            }
            
            return true;
        }

        /// <summary>
        /// Get currently selected dice for hand
        /// </summary>
        public List<BaseDice> GetSelectedDice()
        {
            return new List<BaseDice>(_selectedDice);
        }

        /// <summary>
        /// Complete current hand and apply cooldowns to submitted dice only
        /// </summary>
        /// <param name="submittedDice">List of dice that were locked and submitted</param>
        public void CompleteHand(List<BaseDice> submittedDice = null)
        {
            if (_selectedDice.Count == 0)
            {
                Debug.LogWarning("[CooldownSystem] No dice selected to complete hand");
                return;
            }

            Debug.Log($"[CooldownSystem] Completing hand with {_selectedDice.Count} dice");
            
            // Apply cooldown only to submitted dice (locked and submitted)
            if (submittedDice != null && submittedDice.Count > 0)
            {
                Debug.Log($"[CooldownSystem] Applying cooldown to {submittedDice.Count} submitted dice:");
                foreach (var dice in submittedDice)
                {
                    dice.cooldownRemain = dice.cooldownAfterUse + 1; // Set to 2 turns (1 + 1)
                    Debug.Log($"  - {dice.diceName} cooldown: 0 → {dice.cooldownRemain} (will be unavailable for 1 hand)");
                }
            }
            else
            {
                Debug.Log("[CooldownSystem] No submitted dice provided, applying cooldown to all selected dice");
                // Fallback: apply cooldown to all selected dice if no submitted dice provided
                foreach (var dice in _selectedDice)
                {
                    dice.cooldownRemain = dice.cooldownAfterUse + 1; // Set to 2 turns (1 + 1)
                    Debug.Log($"  - {dice.diceName} on cooldown for {dice.cooldownRemain} turns");
                }
            }
            
            // Clear selection
            _selectedDice.Clear();
            
            // Update hand counter
            currentHandCount++;
            handsRemaining = maxHands - currentHandCount;
            
            Debug.Log($"[CooldownSystem] Hand {currentHandCount}/{maxHands} completed. {handsRemaining} hands remaining.");
            
            // Trigger events
            OnHandCounterUpdate?.Invoke(currentHandCount, handsRemaining);
            
            // Check if we need to refresh (all hands used)
            if (handsRemaining <= 0)
            {
                RefreshDicePool();
            }
            else
            {
                // Don't advance cooldowns here - wait until next hand starts
                UpdateAvailableDice();
            }
        }

        /// <summary>
        /// Advance cooldowns by one turn (called before starting new hand)
        /// </summary>
        public void AdvanceCooldowns()
        {
            Debug.Log("[CooldownSystem] Advancing cooldowns...");
            
            foreach (var dice in _dicePool)
            {
                if (dice.cooldownRemain > 0)
                {
                    dice.cooldownRemain--;
                    Debug.Log($"  - {dice.diceName} cooldown: {dice.cooldownRemain + 1} → {dice.cooldownRemain}");
                    if (dice.cooldownRemain == 0)
                    {
                        Debug.Log($"    {dice.diceName} cooldown complete, now available");
                    }
                }
                else
                {
                    Debug.Log($"  - {dice.diceName} already available (cooldown: {dice.cooldownRemain})");
                }
            }
            
            UpdateAvailableDice();
        }

        /// <summary>
        /// Refresh the entire dice pool (when all hands are used)
        /// </summary>
        private void RefreshDicePool()
        {
            Debug.Log("[CooldownSystem] Refreshing dice pool - all hands used!");
            
            // Reset all cooldowns
            foreach (var dice in _dicePool)
            {
                dice.cooldownRemain = 0;
            }
            
            // Reset hand counter
            currentHandCount = 0;
            handsRemaining = maxHands;
            
            // Update available dice
            UpdateAvailableDice();
            
            // Trigger events
            OnDicePoolRefresh?.Invoke();
            OnHandCounterUpdate?.Invoke(currentHandCount, handsRemaining);
            
            Debug.Log("[CooldownSystem] Dice pool refreshed - ready for new set of hands");
        }

        /// <summary>
        /// Update the list of available dice (not on cooldown)
        /// </summary>
        private void UpdateAvailableDice()
        {
            _availableDice.Clear();
            
            Debug.Log("[CooldownSystem] Updating available dice list:");
            foreach (var dice in _dicePool)
            {
                if (dice.cooldownRemain == 0)
                {
                    _availableDice.Add(dice);
                    Debug.Log($"  AVAILABLE: {dice.diceName} (cooldown: {dice.cooldownRemain})");
                }
                else
                {
                    Debug.Log($"  ON COOLDOWN: {dice.diceName} (cooldown: {dice.cooldownRemain})");
                }
            }
            
            Debug.Log($"[CooldownSystem] Result: {_availableDice.Count}/{_dicePool.Count} dice available");
            OnAvailableDiceChanged?.Invoke(new List<BaseDice>(_availableDice));
        }

        /// <summary>
        /// Get current hand counter info
        /// </summary>
        public (int current, int remaining) GetHandCounter()
        {
            return (currentHandCount, handsRemaining);
        }

        /// <summary>
        /// Check if any dice are available for selection
        /// </summary>
        public bool HasAvailableDice()
        {
            return _availableDice.Count > 0;
        }

        /// <summary>
        /// Get total cost of currently selected dice
        /// </summary>
        public int GetSelectedDiceCost()
        {
            return _selectedDice.Sum(d => d.cost);
        }

        /// <summary>
        /// Check if selection is within budget
        /// </summary>
        public bool IsWithinBudget(int budget)
        {
            return GetSelectedDiceCost() <= budget;
        }

        /// <summary>
        /// Force refresh dice pool (for testing/debugging)
        /// </summary>
        [ContextMenu("Force Refresh Dice Pool")]
        public void ForceRefreshDicePool()
        {
            RefreshDicePool();
        }

        /// <summary>
        /// Log current dice pool status for debugging
        /// </summary>
        private void LogDicePool()
        {
            Debug.Log("[CooldownSystem] === DICE POOL STATUS ===");
            Debug.Log($"Hands: {currentHandCount}/{maxHands} (remaining: {handsRemaining})");
            Debug.Log($"Available: {_availableDice.Count}/{_dicePool.Count}");
            
            foreach (var dice in _dicePool)
            {
                string status = dice.cooldownRemain > 0 ? $"COOLDOWN({dice.cooldownRemain})" : "AVAILABLE";
                Debug.Log($"  {dice.diceName}: {dice.tier}, cost={dice.cost}, {status}");
            }
            
            if (_selectedDice.Count > 0)
            {
                Debug.Log("Selected for current hand:");
                foreach (var dice in _selectedDice)
                {
                    Debug.Log($"  - {dice.diceName}");
                }
            }
        }

        /// <summary>
        /// Reset the entire system to initial state
        /// </summary>
        public void ResetSystem()
        {
            Debug.Log("[CooldownSystem] Resetting system to initial state");
            
            currentHandCount = 0;
            handsRemaining = maxHands;
            _selectedDice.Clear();
            
            // Reset all dice cooldowns
            foreach (var dice in _dicePool)
            {
                dice.cooldownRemain = 0;
                dice.ResetLockAndValue();
            }
            
            UpdateAvailableDice();
            
            Debug.Log("[CooldownSystem] System reset complete");
        }

        // Unity Inspector debugging
        void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                LogDicePool();
            }
        }
    }
}
