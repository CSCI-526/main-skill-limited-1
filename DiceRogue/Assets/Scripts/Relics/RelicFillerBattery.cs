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
            rarity = RelicRarity.Rare;
            description = "With filler dice: +1 reroll charge.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.hasFillerInHand)
            {
                context.bonusRerolls += rerollCharge;
            }
        }
    }
}

