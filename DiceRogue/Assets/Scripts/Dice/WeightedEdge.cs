using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Weighted Edge (Rare) - Only rolls 3 or 6
    /// </summary>
    [System.Serializable]
    public class WeightedEdge : BaseDice
    {
        [Header("Faces (size=6)")]
        public int[] faces = new int[6] { 3, 3, 3, 6, 6, 6 };

        public WeightedEdge()
        {
            diceName = "Weighted Edge";
            tier = DiceTier.Rare;
            cost = 2;
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

