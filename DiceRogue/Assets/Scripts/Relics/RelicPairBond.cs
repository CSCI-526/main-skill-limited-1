using System.Linq;
using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Pair Bond: one pair ×1.2 mult, two pair ×1.5 mult
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Pair Bond", fileName = "Relic_PairBond")]
    public class RelicPairBond : RelicBase
    {
        public float onePairMult = 1.2f;
        public float twoPairMult = 1.5f;

        private void Reset()
        {
            relicName = "Pair Bond";
            rarity = RelicRarity.Common;
            description = "One pair: ×1.2 mult. Two pair: ×1.5 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            var counts = context.submittedValues.GroupBy(v => v).Select(g => g.Count()).ToList();
            int pairCount = counts.Count(c => c >= 2);

            if (pairCount == 1)
            {
                context.multiplier *= onePairMult;
            }
            else if (pairCount >= 2)
            {
                context.multiplier *= twoPairMult;
            }
        }
    }
}

