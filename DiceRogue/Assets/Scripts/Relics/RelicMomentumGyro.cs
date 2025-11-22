using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Momentum Gyro: rewards using all rolls, penalizes early submit
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Momentum Gyro", fileName = "Relic_MomentumGyro")]
    public class RelicMomentumGyro : RelicBase
    {
        public float fullBonus = 1.5f;
        public float partialPenalty = 0.8f;

        private void Reset()
        {
            relicName = "Momentum Gyro";
            rarity = RelicRarity.Common;
            description = "If all rolls used: ×1.5 mult. If not: ×0.8 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.rollsUsed >= context.maxRollsPerHand)
            {
                context.multiplier *= fullBonus;
            }
            else
            {
                context.multiplier *= partialPenalty;
            }
        }
    }
}

