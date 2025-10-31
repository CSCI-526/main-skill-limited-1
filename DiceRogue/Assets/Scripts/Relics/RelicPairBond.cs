using System.Linq;
using UnityEngine;

namespace DiceGame.Relics
{
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
}

