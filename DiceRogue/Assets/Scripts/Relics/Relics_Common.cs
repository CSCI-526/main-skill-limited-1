using UnityEngine;
using System.Linq;

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

    /// <summary>
    /// Pair Bond: rewards any pair with base, bigger mult for two pair/full house, penalty if no pair
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Pair Bond", fileName = "Relic_PairBond")]
    public class RelicPairBond : RelicBase
    {
        public int baseOnAnyPair = 10;
        public float multOnTwoPlus = 1.15f;
        public float missPenalty = 0.95f;

        private void Reset()
        {
            relicName = "Pair Bond";
            rarity = RelicRarity.Common;
            description = "Any pair: +10 base. Two Pair/Full House: ×1.15 mult. No pairs: ×0.95 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            var counts = context.submittedValues.GroupBy(v => v).Select(g => g.Count()).OrderByDescending(x => x).ToList();
            bool anyPair = counts.Any(c => c >= 2);
            bool twoPairOrFH = counts.Count(c => c >= 2) >= 2 || (counts.Contains(3) && counts.Contains(2));

            if (anyPair)
            {
                context.additionalBase += baseOnAnyPair;
                if (twoPairOrFH)
                {
                    context.multiplier *= multOnTwoPlus;
                }
            }
            else
            {
                context.multiplier *= missPenalty;
            }
        }
    }

    /// <summary>
    /// Momentum Gyro: rewards using all rolls, penalizes early submit
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Momentum Gyro", fileName = "Relic_MomentumGyro")]
    public class RelicMomentumGyro : RelicBase
    {
        public int baseBonus = 10;
        public float multBonus = 1.15f;
        public float earlyPenalty = 0.9f;

        private void Reset()
        {
            relicName = "Momentum Gyro";
            rarity = RelicRarity.Common;
            description = "Max rolls used: +10 base, ×1.15 mult. Early submit (≤1 roll): ×0.9 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.rollsUsed >= context.maxRollsPerHand)
            {
                context.additionalBase += baseBonus;
                context.multiplier *= multBonus;
            }
            else if (context.rollsUsed <= 1)
            {
                context.multiplier *= earlyPenalty;
            }
        }
    }
}


