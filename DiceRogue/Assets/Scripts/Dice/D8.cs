using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// D8 (Legendary) - Rolls 1-8, only once per hand
    /// If 7, x5; if 8, x10
    /// Note: The "only once per hand" restriction should be enforced by BattleController
    /// </summary>
    [System.Serializable]
    public class D8 : BaseDice
    {
        private bool hasRolledThisHand = false;

        public D8()
        {
            diceName = "D8";
            tier = DiceTier.Legendary;
            cost = 3;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // Only roll once per hand
            if (hasRolledThisHand)
            {
                return lastRollValue;
            }

            lastRollValue = Random.Range(1, 9); // 1-8 inclusive
            hasRolledThisHand = true;
            return lastRollValue;
        }

        /// <summary>
        /// Reset for a new hand
        /// </summary>
        public override void ResetLockAndValue()
        {
            base.ResetLockAndValue();
            hasRolledThisHand = false;
        }

        /// <summary>
        /// Get the multiplier for this dice (5x for 7, 10x for 8)
        /// </summary>
        public float GetMultiplier()
        {
            if (lastRollValue == 7) return 5.0f;
            if (lastRollValue == 8) return 10.0f;
            return 1.0f;
        }
    }
}

