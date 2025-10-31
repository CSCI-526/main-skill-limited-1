using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Base ScriptableObject for relics. Do not integrate with BattleController yet.
    /// </summary>
    public abstract class RelicBase : ScriptableObject
    {
        [Header("Meta")]
        public string relicName = "New Relic";
        [TextArea]
        public string description;
        public RelicRarity rarity = RelicRarity.Common;

        [Header("Config")]
        public bool unique = true; // prevent duplicates by default

        /// <summary>
        /// Called when computing score. Relics should modify the provided context.
        /// This method should be pure relative to external systems.
        /// </summary>
        public abstract void Apply(ScoringContext context);
    }
}


