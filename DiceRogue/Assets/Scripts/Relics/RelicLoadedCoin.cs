using UnityEngine;

namespace DiceGame.Relics
{
    /// <summary>
    /// Loaded Coin: +0.25 per 6; if any 1, multiplier becomes ×0.8
    /// </summary>
    [CreateAssetMenu(menuName = "DiceRogue/Relics/Loaded Coin", fileName = "Relic_LoadedCoin")]
    public class RelicLoadedCoin : RelicBase
    {
        public float perSix = 0.25f;
        public float multiplierOnOne = 0.8f;

        private void Reset()
        {
            relicName = "Loaded Coin";
            rarity = RelicRarity.Rare;
            description = "When submitted: Each 6 adds +25% multiplier. If any 1 is submitted, multiplier becomes ×0.8.";
        }

        public override void Apply(ScoringContext context)
        {
            int sixes = context.CountValue(6);
            int ones = context.CountValue(1);
            
            // Apply +25% per 6
            float add = sixes * perSix;
            context.multiplier *= (1f + add);
            
            // If any 1, set multiplier to ×0.8
            if (ones > 0)
            {
                context.multiplier = multiplierOnOne;
            }
        }
    }
}

