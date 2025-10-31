using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Handles hand composition logic: selecting dice from pool and filling with normal dice.
    /// Separates dice selection strategy from battle flow.
    /// </summary>
    public class HandCompositionService
    {
        /// <summary>
        /// Compose a full hand by selecting special dice and filling with normal dice
        /// </summary>
        /// <param name="availableDice">Special dice available from the pool</param>
        /// <param name="targetHandSize">Desired hand size (e.g., 5)</param>
        /// <param name="shuffle">Whether to shuffle available dice for variety</param>
        /// <returns>Composed hand with special + normal dice</returns>
        public List<BaseDice> ComposeHand(List<BaseDice> availableDice, int targetHandSize, bool shuffle = true)
        {
            var hand = new List<BaseDice>();

            // Select special dice from available pool
            if (availableDice.Count > 0)
            {
                var selectedSpecialDice = SelectSpecialDice(availableDice, targetHandSize, shuffle);
                hand.AddRange(selectedSpecialDice);

                Debug.Log($"[HandCompositionService] Selected {selectedSpecialDice.Count} special dice:");
                foreach (var dice in selectedSpecialDice)
                {
                    Debug.Log($"  Selected: {dice.diceName}");
                }
            }
            else
            {
                Debug.LogWarning("[HandCompositionService] No special dice available from pool!");
            }

            // Fill remaining slots with normal dice
            if (hand.Count < targetHandSize)
            {
                var normalDice = FillWithNormalDice(hand.Count, targetHandSize);
                hand.AddRange(normalDice);

                Debug.Log($"[HandCompositionService] Filled {normalDice.Count} slots with Normal Dice");
                foreach (var dice in normalDice)
                {
                    Debug.Log($"  Added: {dice.diceName}");
                }
            }

            Debug.Log($"[HandCompositionService] Final hand composition: {hand.Count} dice total ({hand.Count - (targetHandSize - hand.Count)} special + {targetHandSize - hand.Count} normal)");

            return hand;
        }

        /// <summary>
        /// Select special dice from the available pool
        /// </summary>
        /// <param name="availableDice">Available special dice</param>
        /// <param name="maxCount">Maximum number to select</param>
        /// <param name="shuffle">Whether to shuffle for randomness</param>
        /// <returns>Selected special dice</returns>
        public List<BaseDice> SelectSpecialDice(List<BaseDice> availableDice, int maxCount, bool shuffle = true)
        {
            if (availableDice == null || availableDice.Count == 0)
                return new List<BaseDice>();

            // Determine how many to select
            int selectCount = Mathf.Min(maxCount, availableDice.Count);

            // Shuffle if requested for variety
            var diceToSelect = shuffle 
                ? availableDice.OrderBy(x => Random.value).ToList()
                : new List<BaseDice>(availableDice);

            // Take the first N dice
            return diceToSelect.Take(selectCount).ToList();
        }

        /// <summary>
        /// Fill remaining hand slots with normal dice
        /// </summary>
        /// <param name="currentCount">Current number of dice in hand</param>
        /// <param name="targetSize">Target hand size</param>
        /// <returns>List of normal dice to fill the hand</returns>
        public List<BaseDice> FillWithNormalDice(int currentCount, int targetSize)
        {
            var normalDice = new List<BaseDice>();
            int neededCount = targetSize - currentCount;

            for (int i = 0; i < neededCount; i++)
            {
                var normalDie = new NormalDice
                {
                    diceName = $"Normal Dice #{i + 1}"
                };
                normalDice.Add(normalDie);
            }

            return normalDice;
        }

        /// <summary>
        /// Reset all dice states in a hand (unlock and clear values)
        /// </summary>
        public void ResetHandDice(List<BaseDice> dice)
        {
            foreach (var d in dice)
            {
                d.ResetLockAndValue();
            }
        }

        /// <summary>
        /// Get composition summary for analytics/logging
        /// </summary>
        public (int specialCount, int normalCount) GetHandComposition(List<BaseDice> hand)
        {
            int normalCount = hand.Count(d => d is NormalDice);
            int specialCount = hand.Count - normalCount;
            return (specialCount, normalCount);
        }

        /// <summary>
        /// Composes a hand based on player's selection and fills the rest with normal dice.
        /// </summary>
        public List<BaseDice> ComposeHandWithSelection(List<BaseDice> playerSelection, int targetHandSize)
        {
            var hand = new List<BaseDice>(playerSelection);

            // Fill remaining slots with normal dice
            if (hand.Count < targetHandSize)
            {
                var normalDice = FillWithNormalDice(hand.Count, targetHandSize);
                hand.AddRange(normalDice);
            }

            return hand.Take(targetHandSize).ToList(); // Ensure hand does not exceed target size
        }
    }
}

