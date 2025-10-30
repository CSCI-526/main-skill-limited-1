using UnityEngine;

namespace DiceGame.Relics
{
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
}

