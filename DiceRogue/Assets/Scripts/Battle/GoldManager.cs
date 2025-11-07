using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 全局金币系统：支持添加、消耗、查询
    /// </summary>
    public static class GoldManager
    {
        private const string GOLD_KEY = "PlayerGold";

        public static int CurrentGold => PlayerPrefs.GetInt(GOLD_KEY, 0);

        public static void AddGold(int amount)
        {
            if (amount <= 0) return;
            int newTotal = Mathf.Max(0, CurrentGold + amount);
            PlayerPrefs.SetInt(GOLD_KEY, newTotal);
            PlayerPrefs.Save();
            Debug.Log($"[GoldManager] Added {amount} gold. Total: {newTotal}");
        }

        public static bool SpendGold(int amount)
        {
            if (amount <= 0 || CurrentGold < amount)
                return false;

            PlayerPrefs.SetInt(GOLD_KEY, CurrentGold - amount);
            PlayerPrefs.Save();
            Debug.Log($"[GoldManager] Spent {amount} gold. Remaining: {CurrentGold}");
            return true;
        }

        public static void ResetGold()
        {
            PlayerPrefs.SetInt(GOLD_KEY, 0);
            PlayerPrefs.Save();
        }
    }
}
