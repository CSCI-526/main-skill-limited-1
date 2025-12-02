using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Filler Battery: if with filler dice, reroll num charge 1
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Filler Battery", fileName = "Relic_FillerBattery")]
    public class RelicFillerBattery : RelicBase
    {
        public int rerollCharge = 1;

        private void Reset()
        {
            relicName = "Filler Battery";
            rarity = RelicRarity.Legendary;
            description = "With filler dice: +1 reroll charge.";
        }

        public override void Apply(ScoringContext context)
        {
            Debug.Log($"[RelicFillerBattery] Apply called: hasFillerInHand={context.hasFillerInHand}, rerollCharge={rerollCharge}, currentBonusRerolls={context.bonusRerolls}");
            if (context.hasFillerInHand)
            {
                context.bonusRerolls += rerollCharge;
                Debug.Log($"[RelicFillerBattery] Added {rerollCharge} bonus rerolls. New bonusRerolls={context.bonusRerolls}");
            }
            else
            {
                Debug.Log($"[RelicFillerBattery] No filler dice in hand, skipping bonus reroll");
            }
        }
    }
}

