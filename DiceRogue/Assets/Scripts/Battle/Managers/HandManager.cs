using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Manages hand lifecycle: rolls, locks, submission state
    /// </summary>
    public class HandManager
    {
        private int _rollsUsedTotal = 0;
        private int _rollsUsedThisHand = 0;
        private int _totalRollBudget = 3;
        private bool _isHandActive = false;

        /// <summary>
        /// Rolls consumed during the current hand.
        /// </summary>
        public int RollsUsed => _rollsUsedThisHand;

        /// <summary>
        /// Total rolls consumed across all hands in the current battle.
        /// </summary>
        public int TotalRollsUsed => _rollsUsedTotal;

        /// <summary>
        /// Total roll budget shared across all hands in the current battle.
        /// </summary>
        public int TotalRollBudget => _totalRollBudget;

        /// <summary>
        /// Rolls remaining for the current battle.
        /// </summary>
        public int RollsRemaining => Mathf.Max(0, _totalRollBudget - _rollsUsedTotal);

        public bool IsHandActive => _isHandActive;
        public bool CanRoll => _isHandActive && _rollsUsedTotal < _totalRollBudget;

        /// <summary>
        /// Set total roll budget shared across all hands (default: 3)
        /// </summary>
        public void SetMaxRolls(int maxRolls)
        {
            _totalRollBudget = Mathf.Max(0, maxRolls);
        }

        /// <summary>
        /// Add bonus rolls to the total roll budget (for relics that grant extra rerolls)
        /// </summary>
        public void AddBonusRolls(int bonus)
        {
            if (bonus > 0)
            {
                _totalRollBudget += bonus;
                Debug.Log($"[HandManager] Added {bonus} bonus rolls. New budget: {_totalRollBudget}");
            }
        }

        /// <summary>
        /// Start a new hand
        /// </summary>
        public void StartHand()
        {
            _rollsUsedThisHand = 0;
            _isHandActive = true;
            Debug.Log($"[HandManager] New hand started - {RollsRemaining}/{_totalRollBudget} rolls remaining");
        }

        /// <summary>
        /// Increment roll counter and return the current roll number
        /// </summary>
        public int IncrementRoll()
        {
            if (!CanRoll)
            {
                Debug.LogWarning("[HandManager] Cannot roll - roll budget exhausted or hand not active");
                return _rollsUsedThisHand;
            }

            _rollsUsedTotal++;
            _rollsUsedThisHand++;
            Debug.Log($"[HandManager] Roll {_rollsUsedThisHand} this hand ({_rollsUsedTotal}/{_totalRollBudget} total)");
            return _rollsUsedThisHand;
        }

        /// <summary>
        /// End the current hand
        /// </summary>
        public void EndHand()
        {
            _isHandActive = false;
            Debug.Log($"[HandManager] Hand ended after {_rollsUsedThisHand} rolls (total {_rollsUsedTotal}/{_totalRollBudget})");
        }

        /// <summary>
        /// Reset hand state (for Reset button)
        /// </summary>
        public void Reset()
        {
            _rollsUsedTotal = 0;
            _rollsUsedThisHand = 0;
            _isHandActive = false;
            Debug.Log("[HandManager] Hand state reset (total roll budget restored)");
        }

        /// <summary>
        /// Get submitted dice from a list (locked dice with valid values)
        /// </summary>
        public List<BaseDice> GetSubmittedDice(List<BaseDice> allDice)
        {
            var submitted = new List<BaseDice>();
            
            foreach (var dice in allDice)
            {
                if (dice.isLocked && dice.lastRollValue > 0 && dice.tier != DiceTier.Filler)
                {
                    submitted.Add(dice);
                }
            }

            return submitted;
        }

        /// <summary>
        /// Get submitted values from submitted dice
        /// </summary>
        public List<int> GetSubmittedValues(List<BaseDice> submittedDice)
        {
            return submittedDice.Select(d => d.lastRollValue).ToList();
        }

        /// <summary>
        /// Validate that there are dice to submit
        /// </summary>
        public bool CanSubmit(List<BaseDice> allDice)
        {
            if (!_isHandActive)
            {
                Debug.LogWarning("[HandManager] No active hand to submit");
                return false;
            }

            var submitted = GetSubmittedDice(allDice);
            if (submitted.Count == 0)
            {
                Debug.LogWarning("[HandManager] No locked dice to submit!");
                return false;
            }

            return true;
        }
    }
}

