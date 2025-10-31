using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Straight Edge: Straight buffs; set-kind nerf
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Straight Edge", fileName = "Relic_StraightEdge")]
    public class RelicStraightEdge : RelicBase
    {
        public int baseBonus = 15;
        public float straightMult = 1.2f;
        public float setKindPenalty = 0.9f;

        private void Reset()
        {
            relicName = "Straight Edge";
            rarity = RelicRarity.Common;
            description = "Straights: +15 base, ×1.2 mult. Three/Four of a Kind: ×0.9 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            bool large = RelicUtils.IsLargeStraight(context.submittedValues);
            bool small = !large && RelicUtils.IsSmallStraight(context.submittedValues);
            var most = RelicUtils.MostFrequent(context.submittedValues);

            if (large || small)
            {
                context.additionalBase += baseBonus;
                context.multiplier *= straightMult;
            }
            else if (most.count >= 3) // three or four of a kind presence
            {
                context.multiplier *= setKindPenalty;
            }
        }
    }
}

