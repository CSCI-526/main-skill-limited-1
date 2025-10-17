using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Twin Bond (Rare) - Copy a random dice
    /// Note: This requires access to other dice in the hand during Roll()
    /// Implementation will need to be handled by the BattleController
    /// </summary>
    [System.Serializable]
    public class TwinBond : BaseDice
    {
        public TwinBond()
        {
            diceName = "Twin Bond";
            tier = DiceTier.Rare;
            cost = 2;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // Default behavior: roll normally
            // The actual "copy" logic should be handled by BattleController
            // which has access to all dice in the hand
            lastRollValue = Random.Range(1, 7);
            return lastRollValue;
        }

        /// <summary>
        /// Call this method to copy another dice's value
        /// </summary>
        public void CopyValue(int value)
        {
            lastRollValue = value;
        }
    }
}

