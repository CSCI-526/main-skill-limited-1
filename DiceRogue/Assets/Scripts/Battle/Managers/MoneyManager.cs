using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// Manages player money with simple add/subtract operations.
    /// Money cannot go below 0 (unless modified by special mechanisms).
    /// </summary>
    public class MoneyManager
    {
        private int _money;

        public int Money => _money;

        public MoneyManager(int initialMoney = 0)
        {
            _money = Mathf.Max(0, initialMoney);
        }

        /// <summary>
        /// Add money to player
        /// </summary>
        public void Add(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[MoneyManager] Attempted to add negative amount: {amount}. Use Subtract() instead.");
                return;
            }
            _money += amount;
        }

        /// <summary>
        /// Subtract money from player (cannot go below 0)
        /// </summary>
        public bool Subtract(int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"[MoneyManager] Attempted to subtract negative amount: {amount}. Use Add() instead.");
                return false;
            }
            
            if (_money < amount)
            {
                Debug.LogWarning($"[MoneyManager] Insufficient money. Current: {_money}, Required: {amount}");
                return false;
            }
            
            _money -= amount;
            return true;
        }

        /// <summary>
        /// Set money directly (for special mechanisms that allow negative values)
        /// </summary>
        public void Set(int amount)
        {
            _money = amount;
        }

        /// <summary>
        /// Reset money to 0
        /// </summary>
        public void Reset()
        {
            _money = 0;
        }
    }
}

