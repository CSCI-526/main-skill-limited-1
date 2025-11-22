using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Tight Purse: ×3 multiplier on last cast
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Tight Purse", fileName = "Relic_TightPurse")]
    public class RelicTightPurse : RelicBase
    {
        public float lastCastMult = 3.0f;

        private void Reset()
        {
            relicName = "Tight Purse";
            rarity = RelicRarity.Rare;
            description = "Last cast: ×3 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            // If this is the last cast (handsRemaining == 1 means after this cast there will be 0 hands left)
            if (context.handsRemaining == 1)
            {
                context.multiplier *= lastCastMult;
            }
        }
    }
}

