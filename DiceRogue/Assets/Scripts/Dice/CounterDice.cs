using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Counter Dice - Has faces [1,2,2,5,5,6]
    /// </summary>
    [System.Serializable]
    public class CounterDice : BaseDice
    {
        [Header("Faces (size=6)")]
        public int[] faces = new int[6] { 1, 2, 2, 5, 5, 6 };

        public CounterDice()
        {
            diceName = "Counter Dice";
            tier = DiceTier.Common;
            cost = 1;
            cooldownAfterUse = 1;
        }

        public override int Roll()
        {
            if (isLocked) return lastRollValue;

            int idx = Random.Range(0, faces.Length);
            lastRollValue = faces[idx];
            return lastRollValue;
        }
    }
}

