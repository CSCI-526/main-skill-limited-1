using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Collector's Seal: boosts when 3+ of same value, with capped stronger base for 4-5 kind
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Collector's Seal", fileName = "Relic_CollectorsSeal")]
    public class RelicCollectorsSeal : RelicBase
    {
        public int baseOnThreePlus = 15;
        public float multOnThreePlus = 1.1f;
        public int baseOnFourPlus = 25;

        private void Reset()
        {
            relicName = "Collector's Seal";
            rarity = RelicRarity.Legendary;
            description = "Three of a Kind: +15 base, ×1.1 mult. Four/Five of a Kind: +25 base.";
        }

        public override void Apply(ScoringContext context)
        {
            var most = RelicUtils.MostFrequent(context.submittedValues);
            if (most.count >= 4)
            {
                context.additionalBase += baseOnFourPlus;
            }
            else if (most.count >= 3)
            {
                context.additionalBase += baseOnThreePlus;
                context.multiplier *= multOnThreePlus;
            }
        }
    }
}

