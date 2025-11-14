using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Manages relics with two separate pools:
    /// 1. Global relic pool: All relics that can be obtained this run
    /// 2. Player backpack: Relics the player has acquired
    /// </summary>
    public class RelicManager
    {
        // Global relic pool - all relics available this run
        private readonly List<RelicBase> _globalRelicPool = new();
        
        // Player backpack - relics the player has acquired
        private readonly List<RelicBase> _playerBackpack = new();

        /// <summary>
        /// Get all relics in the global pool (available to obtain)
        /// </summary>
        public IReadOnlyList<RelicBase> GlobalRelicPool => _globalRelicPool;

        /// <summary>
        /// Get all relics in the player's backpack (acquired relics)
        /// </summary>
        public IReadOnlyList<RelicBase> PlayerBackpack => _playerBackpack;

        /// <summary>
        /// Legacy property for compatibility - returns player backpack
        /// </summary>
        public IReadOnlyList<RelicBase> Equipped => _playerBackpack;

        /// <summary>
        /// Initialize the global relic pool with all available relics
        /// Creates relic instances directly from code instead of loading from Resources
        /// </summary>
        public void InitializeGlobalRelicPool()
        {
            _globalRelicPool.Clear();
            
            // Create all relic instances directly from code
            var relics = new List<RelicBase>
            {
                CreateRelicTightPurse(),
                CreateRelicStraightEdge(),
                CreateRelicMomentumGyro(),
                CreateRelicPairBond(),
                CreateRelicFillerBattery(),
                CreateRelicLoadedCoin(),
                CreateRelicCooldownRadiator(),
                CreateRelicCrownOfExcess(),
                CreateRelicEchoPrism(),
                CreateRelicCollectorsSeal()
            };
            
            // Filter out null relics and add to pool
            foreach (var relic in relics)
            {
                if (relic != null)
                {
                    _globalRelicPool.Add(relic);
                }
            }
            
            Debug.Log($"[RelicManager] Initialized global relic pool with {_globalRelicPool.Count} relic(s) from code");
        }

        #region Relic Creation Methods

        private RelicTightPurse CreateRelicTightPurse()
        {
            var relic = ScriptableObject.CreateInstance<RelicTightPurse>();
            relic.relicName = "Tight Purse";
            relic.rarity = RelicRarity.Rare;
            relic.description = "+1 hand budget. Unspent budget ≥1: ×0.95 mult.";
            relic.unique = true;
            relic.unspentPenalty = 0.95f;
            return relic;
        }

        private RelicStraightEdge CreateRelicStraightEdge()
        {
            var relic = ScriptableObject.CreateInstance<RelicStraightEdge>();
            relic.relicName = "Straight Edge";
            relic.rarity = RelicRarity.Common;
            relic.description = "Straights: +15 base, ×1.2 mult. Three/Four of a Kind: ×0.9 mult.";
            relic.unique = true;
            relic.baseBonus = 15;
            relic.straightMult = 1.2f;
            relic.setKindPenalty = 0.9f;
            return relic;
        }

        private RelicMomentumGyro CreateRelicMomentumGyro()
        {
            var relic = ScriptableObject.CreateInstance<RelicMomentumGyro>();
            relic.relicName = "Momentum Gyro";
            relic.rarity = RelicRarity.Common;
            relic.description = "Max rolls used: +10 base, ×1.15 mult. Early submit (≤1 roll): ×0.9 mult.";
            relic.unique = true;
            relic.baseBonus = 10;
            relic.multBonus = 1.15f;
            relic.earlyPenalty = 0.9f;
            return relic;
        }

        private RelicPairBond CreateRelicPairBond()
        {
            var relic = ScriptableObject.CreateInstance<RelicPairBond>();
            relic.relicName = "Pair Bond";
            relic.rarity = RelicRarity.Common;
            relic.description = "Any pair: +10 base. Two Pair/Full House: ×1.15 mult. No pairs: ×0.95 mult.";
            relic.unique = true;
            relic.baseOnAnyPair = 10;
            relic.multOnTwoPlus = 1.15f;
            relic.missPenalty = 0.95f;
            return relic;
        }

        private RelicFillerBattery CreateRelicFillerBattery()
        {
            var relic = ScriptableObject.CreateInstance<RelicFillerBattery>();
            relic.relicName = "Filler Battery";
            relic.rarity = RelicRarity.Rare;
            relic.description = "With filler dice: +1 reroll. Next hand: -1 budget.";
            relic.unique = true;
            relic.rerolls = 1;
            relic.nextHandBudgetCost = -1;
            return relic;
        }

        private RelicLoadedCoin CreateRelicLoadedCoin()
        {
            var relic = ScriptableObject.CreateInstance<RelicLoadedCoin>();
            relic.relicName = "Loaded Coin";
            relic.rarity = RelicRarity.Rare;
            relic.description = "Each 6: +5% mult (max +25%). Any 1s: mult capped at ×0.85.";
            relic.unique = true;
            relic.perSix = 0.05f;
            relic.maxBonus = 0.25f;
            relic.floorOnOne = 0.85f;
            return relic;
        }

        private RelicCooldownRadiator CreateRelicCooldownRadiator()
        {
            var relic = ScriptableObject.CreateInstance<RelicCooldownRadiator>();
            relic.relicName = "Cooldown Radiator";
            relic.rarity = RelicRarity.Rare;
            relic.description = "Can select one cooling die. Next hand: +1 extra cooldown.";
            relic.unique = true;
            relic.futureExtraCooldown = 1;
            return relic;
        }

        private RelicCrownOfExcess CreateRelicCrownOfExcess()
        {
            var relic = ScriptableObject.CreateInstance<RelicCrownOfExcess>();
            relic.relicName = "Crown of Excess";
            relic.rarity = RelicRarity.Rare;
            relic.description = "Cost ≥ budget: ×1.15 mult. Cost ≤ budget-2: ×0.95 mult.";
            relic.unique = true;
            relic.rewardMult = 1.15f;
            relic.thriftPenalty = 0.95f;
            return relic;
        }

        private RelicEchoPrism CreateRelicEchoPrism()
        {
            var relic = ScriptableObject.CreateInstance<RelicEchoPrism>();
            relic.relicName = "Echo Prism";
            relic.rarity = RelicRarity.Rare;
            relic.description = "Highest dice value added to base score again.";
            relic.unique = true;
            return relic;
        }

        private RelicCollectorsSeal CreateRelicCollectorsSeal()
        {
            var relic = ScriptableObject.CreateInstance<RelicCollectorsSeal>();
            relic.relicName = "Collector's Seal";
            relic.rarity = RelicRarity.Legendary;
            relic.description = "Three of a Kind: +15 base, ×1.1 mult. Four/Five of a Kind: +25 base.";
            relic.unique = true;
            relic.baseOnThreePlus = 15;
            relic.multOnThreePlus = 1.1f;
            relic.baseOnFourPlus = 25;
            return relic;
        }

        #endregion

        /// <summary>
        /// Initialize the global relic pool with a custom list of relics
        /// </summary>
        public void InitializeGlobalRelicPool(List<RelicBase> relics)
        {
            _globalRelicPool.Clear();
            if (relics != null)
            {
                _globalRelicPool.AddRange(relics);
                Debug.Log($"[RelicManager] Initialized global relic pool with {_globalRelicPool.Count} relic(s)");
            }
        }

        /// <summary>
        /// Add a relic to the player's backpack (called by shop, rewards, etc.)
        /// </summary>
        /// <param name="relic">The relic to add</param>
        /// <returns>True if added successfully, false if failed (e.g., duplicate unique relic)</returns>
        public bool AddRelicToBackpack(RelicBase relic)
        {
            if (relic == null)
            {
                Debug.LogWarning("[RelicManager] Cannot add null relic to backpack");
                return false;
            }

            // Check for unique relics - prevent duplicates
            if (relic.unique)
            {
                foreach (var r in _playerBackpack)
                {
                    if (r != null && r.relicName == relic.relicName)
                    {
                        Debug.LogWarning($"[RelicManager] Cannot add duplicate unique relic: {relic.relicName}");
                        return false;
                    }
                }
            }

            _playerBackpack.Add(relic);
            Debug.Log($"[RelicManager] Added relic to backpack: {relic.relicName} ({relic.rarity})");
            return true;
        }

        /// <summary>
        /// Add a relic to the player's backpack by name (searches global pool)
        /// </summary>
        /// <param name="relicName">Name of the relic to add</param>
        /// <returns>True if added successfully</returns>
        public bool AddRelicToBackpackByName(string relicName)
        {
            var relic = _globalRelicPool.FirstOrDefault(r => r != null && r.relicName == relicName);
            if (relic == null)
            {
                Debug.LogWarning($"[RelicManager] Relic not found in global pool: {relicName}");
                return false;
            }
            return AddRelicToBackpack(relic);
        }

        /// <summary>
        /// Legacy method for compatibility - adds relic to backpack
        /// </summary>
        public bool AddRelic(RelicBase relic)
        {
            return AddRelicToBackpack(relic);
        }

        /// <summary>
        /// Remove a relic from the player's backpack
        /// </summary>
        public bool RemoveRelic(RelicBase relic)
        {
            return _playerBackpack.Remove(relic);
        }

        /// <summary>
        /// Clear the player's backpack (but keep global pool)
        /// </summary>
        public void ClearBackpack()
        {
            _playerBackpack.Clear();
            Debug.Log("[RelicManager] Cleared player backpack");
        }

        /// <summary>
        /// Clear all relics (both global pool and backpack)
        /// </summary>
        public void Clear()
        {
            _globalRelicPool.Clear();
            _playerBackpack.Clear();
            Debug.Log("[RelicManager] Cleared all relics");
        }

        /// <summary>
        /// Apply all relics from player backpack to scoring context
        /// </summary>
        public void ApplyAll(ScoringContext context)
        {
            if (context == null) return;
            for (int i = 0; i < _playerBackpack.Count; i++)
            {
                var relic = _playerBackpack[i];
                if (relic == null) continue;
                relic.Apply(context);
            }
        }
    }
}


