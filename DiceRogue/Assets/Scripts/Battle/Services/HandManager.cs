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
        private int _rollsUsed = 0;
        private int _maxRollsPerHand = 3;
        private bool _isHandActive = false;

        public int RollsUsed => _rollsUsed;
        public int MaxRollsPerHand => _maxRollsPerHand;
        public bool IsHandActive => _isHandActive;
        public bool CanRoll => _isHandActive && _rollsUsed < _maxRollsPerHand;

        /// <summary>
        /// Set maximum rolls per hand (default: 3)
        /// </summary>
        public void SetMaxRolls(int maxRolls)
        {
            _maxRollsPerHand = maxRolls;
        }

        /// <summary>
        /// Start a new hand
        /// </summary>
        public void StartHand()
        {
            _rollsUsed = 0;
            _isHandActive = true;
            Debug.Log($"[HandManager] New hand started - {_maxRollsPerHand} rolls available");
        }

        /// <summary>
        /// Increment roll counter and return the current roll number
        /// </summary>
        public int IncrementRoll()
        {
            if (!CanRoll)
            {
                Debug.LogWarning("[HandManager] Cannot roll - max rolls reached or hand not active");
                return _rollsUsed;
            }

            _rollsUsed++;
            Debug.Log($"[HandManager] Roll {_rollsUsed}/{_maxRollsPerHand}");
            return _rollsUsed;
        }

        /// <summary>
        /// End the current hand
        /// </summary>
        public void EndHand()
        {
            _isHandActive = false;
            Debug.Log($"[HandManager] Hand ended after {_rollsUsed} rolls");
        }

        /// <summary>
        /// Reset hand state (for Reset button)
        /// </summary>
        public void Reset()
        {
            _rollsUsed = 0;
            _isHandActive = false;
            Debug.Log("[HandManager] Hand state reset");
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

