using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Factory for creating a random pool of 8 dice from all available dice types
    /// </summary>
    public static class DicePoolFactory
    {
        /// <summary>
        /// Create a random pool of 8 dice from all available dice types
        /// </summary>
        public static List<BaseDice> CreateRandomPool(int poolSize = 8, int cooldownTurns = 1)
        {
            // Get all available dice types
            var allDiceTypes = GetAllDiceTypes();
            
            // Shuffle the list
            var shuffled = allDiceTypes.OrderBy(x => Random.value).ToList();
            
            // Take the first poolSize dice
            var selectedTypes = shuffled.Take(poolSize).ToList();
            
            // Create instances
            var pool = new List<BaseDice>();
            foreach (var diceType in selectedTypes)
            {
                var dice = CreateDiceInstance(diceType, cooldownTurns);
                if (dice != null)
                {
                    pool.Add(dice);
                    Debug.Log($"[DicePoolFactory] Added {dice.diceName} ({dice.tier}, cost: {dice.cost})");
                }
            }
            
            Debug.Log($"[DicePoolFactory] Created pool of {pool.Count} dice");
            return pool;
        }
        
        /// <summary>
        /// Get all available dice types with their metadata
        /// </summary>
        private static List<DiceTypeInfo> GetAllDiceTypes()
        {
            return new List<DiceTypeInfo>
            {
                // Common dice
                new DiceTypeInfo { Type = DiceType.BigOne, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.BigSix, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.CounterDice, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.EvenDice, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.OddDice, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.HeavyDice, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.LightDice, Tier = DiceTier.Common },
                new DiceTypeInfo { Type = DiceType.MirrorDice, Tier = DiceTier.Common },
                
                // Rare dice
                new DiceTypeInfo { Type = DiceType.CollectorDice, Tier = DiceTier.Rare },
                new DiceTypeInfo { Type = DiceType.LuckySix, Tier = DiceTier.Rare },
                new DiceTypeInfo { Type = DiceType.PlusOne, Tier = DiceTier.Rare },
                new DiceTypeInfo { Type = DiceType.SevenSevenSeven, Tier = DiceTier.Rare },
                new DiceTypeInfo { Type = DiceType.TwinBond, Tier = DiceTier.Rare },
                new DiceTypeInfo { Type = DiceType.WeightedEdge, Tier = DiceTier.Rare },
                
                // Legendary dice
                new DiceTypeInfo { Type = DiceType.D8, Tier = DiceTier.Legendary },
                new DiceTypeInfo { Type = DiceType.GoldenDice, Tier = DiceTier.Legendary },
                new DiceTypeInfo { Type = DiceType.ZombieDice, Tier = DiceTier.Legendary },
            };
        }
        
        /// <summary>
        /// Create an instance of a specific dice type
        /// </summary>
        private static BaseDice CreateDiceInstance(DiceTypeInfo typeInfo, int cooldownTurns)
        {
            BaseDice dice = typeInfo.Type switch
            {
                // Common dice
                DiceType.BigOne => new BigOne(),
                DiceType.BigSix => new BigSix(),
                DiceType.CounterDice => new CounterDice(),
                DiceType.EvenDice => new EvenDice(),
                DiceType.OddDice => new OddDice(),
                DiceType.HeavyDice => new HeavyDice(),
                DiceType.LightDice => new LightDice(),
                DiceType.MirrorDice => new MirrorDice(),
                
                // Rare dice
                DiceType.CollectorDice => new CollectorDice(),
                DiceType.LuckySix => new LuckySix(),
                DiceType.PlusOne => new PlusOne(),
                DiceType.SevenSevenSeven => new SevenSevenSeven(),
                DiceType.TwinBond => new TwinBond(),
                DiceType.WeightedEdge => new WeightedEdge(),
                
                // Legendary dice
                DiceType.D8 => new D8(),
                DiceType.GoldenDice => new GoldenDice(),
                DiceType.ZombieDice => new ZombieDice(),
                
                _ => null
            };
            
            if (dice != null)
            {
                dice.cooldownAfterUse = cooldownTurns;
                dice.cooldownRemain = 0;
            }
            
            return dice;
        }
        
        /// <summary>
        /// Dice type enumeration
        /// </summary>
        private enum DiceType
        {
            // Common
            BigOne, BigSix, CounterDice, EvenDice, OddDice, HeavyDice, LightDice, MirrorDice,
            
            // Rare
            CollectorDice, LuckySix, PlusOne, SevenSevenSeven, TwinBond, WeightedEdge,
            
            // Legendary
            D8, GoldenDice, ZombieDice
        }
        
        /// <summary>
        /// Dice type metadata
        /// </summary>
        private class DiceTypeInfo
        {
            public DiceType Type { get; set; }
            public DiceTier Tier { get; set; }
        }
    }
}

