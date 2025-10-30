using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Crown of Excess: if total cost ≥ HB then mult ×1.15, else if ≤ HB-2 then ×0.95
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Crown of Excess", fileName = "Relic_CrownOfExcess")]
    public class RelicCrownOfExcess : RelicBase
    {
        public float rewardMult = 1.15f;
        public float thriftPenalty = 0.95f;

        private void Reset()
        {
            relicName = "Crown of Excess";
            rarity = RelicRarity.Rare;
            description = "Cost ≥ budget: ×1.15 mult. Cost ≤ budget-2: ×0.95 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.totalSelectedCost >= context.handBudget)
            {
                context.multiplier *= rewardMult;
            }
            else if (context.totalSelectedCost <= context.handBudget - 2)
            {
                context.multiplier *= thriftPenalty;
            }
        }
    }
}

