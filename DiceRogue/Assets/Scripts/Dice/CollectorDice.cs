using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Collector Dice (Rare) - If roll the same number as last roll, x1.5
    /// Note: The multiplier effect should be handled in the scoring/damage calculation
    /// </summary>
    [System.Serializable]
    public class CollectorDice : BaseDice
    {
        private int previousRollValue = 0;

        public CollectorDice()
        {
            diceName = "Collector Dice";
            tier = DiceTier.Rare;
            cost = 2;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // Store the previous value before rolling
            previousRollValue = lastRollValue;
            
            // Roll new value
            lastRollValue = Random.Range(1, 7);
            return lastRollValue;
        }

        /// <summary>
        /// Check if current roll matches the previous roll
        /// </summary>
        public bool HasMatchedPreviousRoll()
        {
            return previousRollValue > 0 && lastRollValue == previousRollValue;
        }

        /// <summary>
        /// Get the multiplier for this dice (1.5x if current roll matches previous roll)
        /// </summary>
        public float GetMultiplier()
        {
            return HasMatchedPreviousRoll() ? 1.5f : 1.0f;
        }

        public override void ResetLockAndValue()
        {
            base.ResetLockAndValue();
            previousRollValue = 0;
        }
    }
}

