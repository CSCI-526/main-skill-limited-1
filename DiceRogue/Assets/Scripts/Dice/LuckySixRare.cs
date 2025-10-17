using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Lucky Six (Rare) - If roll 6, x1.5
    /// Note: The multiplier effect should be handled in the scoring/damage calculation
    /// </summary>
    [System.Serializable]
    public class LuckySix : BaseDice
    {
        public LuckySix()
        {
            diceName = "Lucky Six";
            tier = DiceTier.Rare;
            cost = 2;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            lastRollValue = Random.Range(1, 7);
            return lastRollValue;
        }

        /// <summary>
        /// Get the multiplier for this dice (1.5x if rolled a 6)
        /// </summary>
        public float GetMultiplier()
        {
            return lastRollValue == 6 ? 1.5f : 1.0f;
        }
    }
}

