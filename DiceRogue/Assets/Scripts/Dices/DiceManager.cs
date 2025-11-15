using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Manages dice with two separate pools:
    /// 1. Global dice pool: All dice types that can be obtained this run
    /// 2. Player dice backpack: Dice the player has acquired
    /// </summary>
    public class DiceManager
    {
        // Global dice pool - all dice types available this run
        private readonly List<BaseDice> _globalDicePool = new();
        
        // Player backpack - dice the player has acquired
        private readonly List<BaseDice> _playerDiceBackpack = new();

        /// <summary>
        /// Get all dice types in the global pool (available to obtain)
        /// </summary>
        public IReadOnlyList<BaseDice> GlobalDicePool => _globalDicePool;

        /// <summary>
        /// Get all dice in the player's backpack (acquired dice)
        /// </summary>
        public IReadOnlyList<BaseDice> PlayerDiceBackpack => _playerDiceBackpack;

        /// <summary>
        /// Initialize the global dice pool with all available dice types
        /// Uses DicePool.GetNonFiller() to automatically get non-Filler dice types
        /// </summary>
        public void InitializeGlobalDicePool()
        {
            _globalDicePool.Clear();
            
            // Use GetNonFiller() to automatically filter out Filler dice
            var nonFillerDice = DicePool.GetNonFiller();
            _globalDicePool.AddRange(nonFillerDice);
            
            Debug.Log($"[DiceManager] Initialized global dice pool with {_globalDicePool.Count} dice type(s)");
        }

        /// <summary>
        /// Add a dice to the player's backpack (called by rewards, shop, etc.)
        /// </summary>
        /// <param name="dice">The dice to add</param>
        /// <returns>True if added successfully, false if failed (e.g., duplicate)</returns>
        public bool AddDiceToBackpack(BaseDice dice)
        {
            if (dice == null)
            {
                Debug.LogWarning("[DiceManager] Cannot add null dice to backpack");
                return false;
            }

            // Check if dice of this type already exists in backpack
            string typeName = dice.GetType().Name;
            if (_playerDiceBackpack.Any(d => d != null && d.GetType().Name == typeName))
            {
                Debug.LogWarning($"[DiceManager] Dice already in backpack: {typeName}");
                return false;
            }

            // Create a new instance to avoid reference issues
            var newDice = CreateDiceFromTypeName(typeName);
            if (newDice != null)
            {
                _playerDiceBackpack.Add(newDice);
                Debug.Log($"[DiceManager] Added dice to backpack: {newDice.diceName} ({newDice.tier})");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Add a dice to the player's backpack by type name (searches global pool)
        /// </summary>
        /// <param name="diceTypeName">Type name of the dice to add (e.g., "D8", "HeavyDice")</param>
        /// <returns>True if added successfully</returns>
        public bool AddDiceToBackpackByName(string diceTypeName)
        {
            if (string.IsNullOrEmpty(diceTypeName))
            {
                Debug.LogWarning("[DiceManager] Cannot add dice with empty type name");
                return false;
            }

            // Check if already in backpack
            if (_playerDiceBackpack.Any(d => d != null && d.GetType().Name == diceTypeName))
            {
                Debug.LogWarning($"[DiceManager] Dice already in backpack: {diceTypeName}");
                return false;
            }

            // Create dice from type name
            var dice = CreateDiceFromTypeName(diceTypeName);
            if (dice != null)
            {
                _playerDiceBackpack.Add(dice);
                Debug.Log($"[DiceManager] Added dice to backpack by name: {dice.diceName} ({dice.tier})");
                return true;
            }

            Debug.LogWarning($"[DiceManager] Could not create dice from type name: {diceTypeName}");
            return false;
        }

        /// <summary>
        /// Remove a dice from the player's backpack
        /// </summary>
        /// <param name="dice">The dice to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveDiceFromBackpack(BaseDice dice)
        {
            if (dice == null)
            {
                return false;
            }

            bool removed = _playerDiceBackpack.Remove(dice);
            if (removed)
            {
                Debug.Log($"[DiceManager] Removed dice from backpack: {dice.diceName}");
            }
            return removed;
        }

        /// <summary>
        /// Remove a dice from the player's backpack by type name
        /// </summary>
        /// <param name="diceTypeName">Type name of the dice to remove</param>
        /// <returns>True if removed successfully</returns>
        public bool RemoveDiceFromBackpackByName(string diceTypeName)
        {
            if (string.IsNullOrEmpty(diceTypeName))
            {
                return false;
            }

            var dice = _playerDiceBackpack.FirstOrDefault(d => d != null && d.GetType().Name == diceTypeName);
            if (dice != null)
            {
                return RemoveDiceFromBackpack(dice);
            }

            return false;
        }

        /// <summary>
        /// Clear the player's dice backpack (but keep global pool)
        /// </summary>
        public void ClearBackpack()
        {
            _playerDiceBackpack.Clear();
            Debug.Log("[DiceManager] Cleared player dice backpack");
        }

        /// <summary>
        /// Clear all dice (both global pool and backpack)
        /// </summary>
        public void Clear()
        {
            _globalDicePool.Clear();
            _playerDiceBackpack.Clear();
            Debug.Log("[DiceManager] Cleared all dice");
        }

        /// <summary>
        /// Load player dice backpack from save data
        /// </summary>
        /// <param name="saveData">Save data containing dice type IDs</param>
        public void LoadFromSaveData(SaveData saveData)
        {
            _playerDiceBackpack.Clear();

            if (saveData == null || saveData.diceTypeIds == null)
            {
                Debug.LogWarning("[DiceManager] SaveData is null or diceTypeIds is null");
                return;
            }

            foreach (var typeId in saveData.diceTypeIds)
            {
                if (string.IsNullOrEmpty(typeId))
                {
                    continue;
                }

                var dice = CreateDiceFromTypeName(typeId);
                if (dice != null)
                {
                    _playerDiceBackpack.Add(dice);
                }
                else
                {
                    Debug.LogWarning($"[DiceManager] Could not create dice from typeId: {typeId}");
                }
            }

            Debug.Log($"[DiceManager] Loaded {_playerDiceBackpack.Count} dice from save data");
        }

        /// <summary>
        /// Save player dice backpack to save data
        /// </summary>
        /// <param name="saveData">Save data to write dice type IDs to</param>
        public void SaveToSaveData(SaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[DiceManager] SaveData is null");
                return;
            }

            saveData.diceTypeIds.Clear();
            saveData.diceTypeIds.AddRange(
                _playerDiceBackpack
                    .Where(d => d != null && d.tier != DiceTier.Filler)
                    .Select(d => d.GetType().Name)
            );

            Debug.Log($"[DiceManager] Saved {saveData.diceTypeIds.Count} dice to save data");
        }

        /// <summary>
        /// Create a dice instance from type name
        /// Searches global pool first, then falls back to DicePool.GetAll()
        /// </summary>
        /// <param name="typeName">Type name of the dice (e.g., "D8", "HeavyDice")</param>
        /// <returns>New dice instance, or null if not found</returns>
        private BaseDice CreateDiceFromTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            // First, try to find prototype in global pool
            var prototype = _globalDicePool.FirstOrDefault(d => d != null && d.GetType().Name == typeName);

            // If not found in global pool, try DicePool.GetAll()
            if (prototype == null)
            {
                var allDice = DicePool.GetAll();
                prototype = allDice.FirstOrDefault(d => d != null && d.GetType().Name == typeName);
            }

            if (prototype != null)
            {
                // Create a new instance using reflection
                var diceType = prototype.GetType();
                var newDice = System.Activator.CreateInstance(diceType) as BaseDice;

                if (newDice != null)
                {
                    // Copy properties from prototype
                    newDice.diceName = prototype.diceName;
                    newDice.description = prototype.description;
                    newDice.tier = prototype.tier;
                    newDice.cost = prototype.cost;
                    newDice.cooldownAfterUse = prototype.cooldownAfterUse;
                    newDice.cooldownRemain = 0;
                    newDice.isLocked = false;
                    newDice.lastRollValue = 0;

                    return newDice;
                }
            }

            Debug.LogWarning($"[DiceManager] Could not create dice from typeName: {typeName}");
            return null;
        }
    }
}

