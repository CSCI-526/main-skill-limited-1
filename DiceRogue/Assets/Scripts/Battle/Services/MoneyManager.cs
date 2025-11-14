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
            Debug.Log($"[MoneyManager] Money added: +{amount}, Total: {_money}");
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
            Debug.Log($"[MoneyManager] Money subtracted: -{amount}, Remaining: {_money}");
            return true;
        }

        /// <summary>
        /// Set money directly (for special mechanisms that allow negative values)
        /// </summary>
        public void Set(int amount)
        {
            _money = amount;
            Debug.Log($"[MoneyManager] Money set to: {_money}");
        }

        /// <summary>
        /// Reset money to 0
        /// </summary>
        public void Reset()
        {
            _money = 0;
            Debug.Log("[MoneyManager] Money reset to 0");
        }
    }
}

