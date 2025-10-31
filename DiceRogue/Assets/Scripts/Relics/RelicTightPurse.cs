using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Tight Purse: +1 HB, but penalize if unspent
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Tight Purse", fileName = "Relic_TightPurse")]
    public class RelicTightPurse : RelicBase
    {
        public float unspentPenalty = 0.95f;

        private void Reset()
        {
            relicName = "Tight Purse";
            rarity = RelicRarity.Rare;
            description = "+1 hand budget. Unspent budget ≥1: ×0.95 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            context.handBudget += 1;
            if (context.handBudget - context.totalSelectedCost >= 1)
            {
                context.multiplier *= unspentPenalty;
            }
        }
    }
}

