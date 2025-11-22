using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Straight Edge: Small straight ×1.2 mult, Large straight ×1.5 mult
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Straight Edge", fileName = "Relic_StraightEdge")]
    public class RelicStraightEdge : RelicBase
    {
        public float smallStraightMult = 1.2f;
        public float largeStraightMult = 1.5f;

        private void Reset()
        {
            relicName = "Straight Edge";
            rarity = RelicRarity.Common;
            description = "Small straight: ×1.2 mult. Large straight: ×1.5 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            bool large = RelicUtils.IsLargeStraight(context.submittedValues);
            bool small = !large && RelicUtils.IsSmallStraight(context.submittedValues);

            if (large)
            {
                context.multiplier *= largeStraightMult;
            }
            else if (small)
            {
                context.multiplier *= smallStraightMult;
            }
        }
    }
}

