using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Big One - 25% chance to roll 1
    /// </summary>
    [System.Serializable]
    public class BigOne : BaseDice
    {
        public BigOne()
        {
            diceName = "Big One";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            // 25% chance for 1, 75% for others (2-6)
            float rand = Random.value;
            if (rand < 0.25f)
            {
                lastRollValue = 1;
            }
            else
            {
                lastRollValue = Random.Range(2, 7); // 2-6 inclusive
            }

            return lastRollValue;
        }
    }
}

