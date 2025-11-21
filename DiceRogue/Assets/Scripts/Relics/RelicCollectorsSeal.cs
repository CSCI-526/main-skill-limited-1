using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Collector's Seal: boosts when 3+ of same value, with different effects for 3, 4, and 5 of a kind
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Collector's Seal", fileName = "Relic_CollectorsSeal")]
    public class RelicCollectorsSeal : RelicBase
    {
        public float multOnThree = 1.25f;
        public float multOnFour = 1.5f;
        public float multOnFive = 2.0f;

        private void Reset()
        {
            relicName = "Collector's Seal";
            rarity = RelicRarity.Legendary;
            description = "Three of a Kind: ×1.25 mult. Four of a Kind: ×1.5 mult. Five of a Kind: ×2 mult.";
        }

        public override void Apply(ScoringContext context)
        {
            var most = RelicUtils.MostFrequent(context.submittedValues);
            if (most.count >= 5)
            {
                context.multiplier *= multOnFive;
            }
            else if (most.count >= 4)
            {
                context.multiplier *= multOnFour;
            }
            else if (most.count >= 3)
            {
                context.multiplier *= multOnThree;
            }
        }
    }
}

