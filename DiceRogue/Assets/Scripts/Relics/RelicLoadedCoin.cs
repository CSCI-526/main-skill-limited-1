using UnityEngine;

namespace DiceGame.Relics
{
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
}

