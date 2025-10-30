using UnityEngine;

namespace DiceGame.Relics
{
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

