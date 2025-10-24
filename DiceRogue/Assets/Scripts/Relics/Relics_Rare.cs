using UnityEngine;
using System.Linq;

namespace DiceGame.Relics
{
    /// <summary>
    /// Tight Purse: +1 HB, but penalize if unspent
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Tight Purse", fileName = "Relic_TightPurse")]
    public class RelicTightPurse : RelicBase
    {
        public float unspentPenalty = 0.95f;

        public override void Apply(ScoringContext context)
        {
            context.handBudget += 1;
            if (context.handBudget - context.totalSelectedCost >= 1)
            {
                context.multiplier *= unspentPenalty;
            }
        }
    }

    /// <summary>
    /// Cooldown Radiator: can include one cooling die (not enforced here); payback next hand cooldown
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Cooldown Radiator", fileName = "Relic_CooldownRadiator")]
    public class RelicCooldownRadiator : RelicBase
    {
        public int futureExtraCooldown = 1;

        public override void Apply(ScoringContext context)
        {
            // Only record the debt; actual selection rule handled by gameplay later
            context.nextHandExtraCooldown = Mathf.Max(context.nextHandExtraCooldown, futureExtraCooldown);
        }
    }

    /// <summary>
    /// Filler Battery: ignore filler penalty, +1 reroll; next hand HB -1
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Filler Battery", fileName = "Relic_FillerBattery")]
    public class RelicFillerBattery : RelicBase
    {
        public int rerolls = 1;
        public int nextHandBudgetCost = -1;

        public override void Apply(ScoringContext context)
        {
            if (context.hasFillerInHand)
            {
                context.bonusRerolls += rerolls;
                context.nextHandBudgetDelta += nextHandBudgetCost;
            }
        }
    }

    /// <summary>
    /// Loaded Coin: +0.05 per 6 (max +0.25); if any 1, floor at 0.85
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Loaded Coin", fileName = "Relic_LoadedCoin")]
    public class RelicLoadedCoin : RelicBase
    {
        public float perSix = 0.05f;
        public float maxBonus = 0.25f;
        public float floorOnOne = 0.85f;

        public override void Apply(ScoringContext context)
        {
            int sixes = context.CountValue(6);
            int ones = context.CountValue(1);
            float add = Mathf.Min(maxBonus, sixes * perSix);
            context.multiplier *= (1f + add);
            if (ones > 0 && context.multiplier < floorOnOne)
            {
                context.multiplier = floorOnOne;
            }
        }
    }

    /// <summary>
    /// Crown of Excess: if total cost ≥ HB then mult ×1.15, else if ≤ HB-2 then ×0.95
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Crown of Excess", fileName = "Relic_CrownOfExcess")]
    public class RelicCrownOfExcess : RelicBase
    {
        public float rewardMult = 1.15f;
        public float thriftPenalty = 0.95f;

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

    /// <summary>
    /// Echo Prism: duplicate highest die value for sum only
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Echo Prism", fileName = "Relic_EchoPrism")]
    public class RelicEchoPrism : RelicBase
    {
        public override void Apply(ScoringContext context)
        {
            if (context.submittedValues.Count == 0) return;
            int max = context.submittedValues.Max();
            context.additionalBase += max; // adds to Base+Sum as an additive
        }
    }
}


