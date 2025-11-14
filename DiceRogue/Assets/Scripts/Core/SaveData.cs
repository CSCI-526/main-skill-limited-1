using System.Collections.Generic;
using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 需要持久化的玩家数据
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        public int money = 0;
        public List<string> relicNames = new List<string>();
        public List<string> diceTypeIds = new List<string>();
        public bool hasCompletedTutorial = false;
        
        // 可选：统计信息
        public int bestScore = 0;
    }
}

