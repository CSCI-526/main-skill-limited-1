using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Echo Prism: duplicate dice sum for base score
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Echo Prism", fileName = "Relic_EchoPrism")]
    public class RelicEchoPrism : RelicBase
    {
        private void Reset()
        {
            relicName = "Echo Prism";
            rarity = RelicRarity.Rare;
            description = "Dice sum added to base score again.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.submittedValues.Count == 0) return;
            int sum = context.Sum;
            context.additionalBase += sum; // adds to Base+Sum as an additive
        }
    }
}

