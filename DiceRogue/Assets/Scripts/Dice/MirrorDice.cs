using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Mirror Dice - Roll num = 7 - last num
    /// </summary>
    [System.Serializable]
    public class MirrorDice : BaseDice
    {
        public MirrorDice()
        {
            diceName = "Mirror Dice";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // If this is the first roll or lastRollValue is 0, roll normally
            if (lastRollValue == 0)
            {
                lastRollValue = Random.Range(1, 7);
            }
            else
            {
                // Mirror: 7 - last num
                lastRollValue = 7 - lastRollValue;
            }

            return lastRollValue;
        }
    }
}

