using System.Linq;
using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Echo Prism: duplicate highest dice value for sum only
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Echo Prism", fileName = "Relic_EchoPrism")]
    public class RelicEchoPrism : RelicBase
    {
        private void Reset()
        {
            relicName = "Echo Prism";
            rarity = RelicRarity.Rare;
            description = "Highest dice value added to base score again.";
        }

        public override void Apply(ScoringContext context)
        {
            if (context.submittedValues.Count == 0) return;
            int max = context.submittedValues.Max();
            context.additionalBase += max; // adds to Base+Sum as an additive
        }
    }
}

