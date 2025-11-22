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
                // TEMPORARILY BANNED: CreateRelicCooldownRadiator(),
                // TEMPORARILY BANNED: CreateRelicCrownOfExcess(),
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
            relic.description = "Last cast: ×3 mult.";
            relic.unique = true;
            relic.lastCastMult = 3.0f;
            return relic;
        }

        private RelicStraightEdge CreateRelicStraightEdge()
        {
            var relic = ScriptableObject.CreateInstance<RelicStraightEdge>();
            relic.relicName = "Straight Edge";
            relic.rarity = RelicRarity.Common;
            relic.description = "Small straight: ×1.2 mult. Large straight: ×1.5 mult.";
            relic.unique = true;
            relic.smallStraightMult = 1.2f;
            relic.largeStraightMult = 1.5f;
            return relic;
        }

        private RelicMomentumGyro CreateRelicMomentumGyro()
        {
            var relic = ScriptableObject.CreateInstance<RelicMomentumGyro>();
            relic.relicName = "Momentum Gyro";
            relic.rarity = RelicRarity.Common;
            relic.description = "If all rolls used: ×1.5 mult. If not: ×0.8 mult.";
            relic.unique = true;
            relic.fullBonus = 1.5f;
            relic.partialPenalty = 0.8f;
            return relic;
        }

        private RelicPairBond CreateRelicPairBond()
        {
            var relic = ScriptableObject.CreateInstance<RelicPairBond>();
            relic.relicName = "Pair Bond";
            relic.rarity = RelicRarity.Common;
            relic.description = "One pair: ×1.2 mult. Two pair: ×1.5 mult.";
            relic.unique = true;
            relic.onePairMult = 1.2f;
            relic.twoPairMult = 1.5f;
            return relic;
        }

        private RelicFillerBattery CreateRelicFillerBattery()
        {
            var relic = ScriptableObject.CreateInstance<RelicFillerBattery>();
            relic.relicName = "Filler Battery";
            relic.rarity = RelicRarity.Rare;
            relic.description = "With filler dice: +1 reroll charge.";
            relic.unique = true;
            relic.rerollCharge = 1;
            return relic;
        }

        private RelicLoadedCoin CreateRelicLoadedCoin()
        {
            var relic = ScriptableObject.CreateInstance<RelicLoadedCoin>();
            relic.relicName = "Loaded Coin";
            relic.rarity = RelicRarity.Rare;
            relic.description = "When submitted: Each 6 adds +25% multiplier. If any 1 is submitted, multiplier becomes ×0.8.";
            relic.unique = true;
            relic.perSix = 0.25f;
            relic.multiplierOnOne = 0.8f;
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
            relic.description = "Dice sum added to base score again.";
            relic.unique = true;
            return relic;
        }

        private RelicCollectorsSeal CreateRelicCollectorsSeal()
        {
            var relic = ScriptableObject.CreateInstance<RelicCollectorsSeal>();
            relic.relicName = "Collector's Seal";
            relic.rarity = RelicRarity.Legendary;
            relic.description = "Three of a Kind: ×1.25 mult. Four of a Kind: ×1.5 mult. Five of a Kind: ×2 mult.";
            relic.unique = true;
            relic.multOnThree = 1.25f;
            relic.multOnFour = 1.5f;
            relic.multOnFive = 2.0f;
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


