using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Odd Dice - Only rolls odd numbers (1,3,5)
    /// </summary>
    [System.Serializable]
    public class OddDice : BaseDice
    {
        [Header("Faces (size=6)")]
        public int[] faces = new int[6] { 1, 1, 3, 3, 5, 5 };

        public OddDice()
        {
            diceName = "Odd Dice";
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

