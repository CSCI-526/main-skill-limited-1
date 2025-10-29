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

    /// <summary>
    /// Cooldown Radiator: can include one cooling die (not enforced here); payback next hand cooldown
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Cooldown Radiator", fileName = "Relic_CooldownRadiator")]
    public class RelicCooldownRadiator : RelicBase
    {
        public int futureExtraCooldown = 1;

        private void Reset()
        {
            relicName = "Cooldown Radiator";
            rarity = RelicRarity.Rare;
            description = "Can select one cooling die. Next hand: +1 extra cooldown.";
        }

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

        private void Reset()
        {
            relicName = "Filler Battery";
            rarity = RelicRarity.Rare;
            description = "With filler dice: +1 reroll. Next hand: -1 budget.";
        }

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

        private void Reset()
        {
            relicName = "Loaded Coin";
            rarity = RelicRarity.Rare;
            description = "Each 6: +5% mult (max +25%). Any 1s: mult capped at ×0.85.";
        }

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

    /// <summary>
    /// Echo Prism: duplicate highest die value for sum only
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Echo Prism", fileName = "Relic_EchoPrism")]
    public class RelicEchoPrism : RelicBase
    {
        private void Reset()
        {
            relicName = "Echo Prism";
            rarity = RelicRarity.Rare;
            description = "Highest die value added to base score again.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.submittedValues.Count == 0) return;
            int max = context.submittedValues.Max();
            context.additionalBase += max; // adds to Base+Sum as an additive
        }
    }
}


