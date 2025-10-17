using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Even Dice - Only rolls even numbers (2,4,6)
    /// </summary>
    [System.Serializable]
    public class EvenDice : BaseDice
    {
        [Header("Faces (size=6)")]
        public int[] faces = new int[6] { 2, 2, 4, 4, 6, 6 };

        public EvenDice()
        {
            diceName = "Even Dice";
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

