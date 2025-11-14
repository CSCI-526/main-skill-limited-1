using System.Collections.Generic;

namespace DiceGame
{
    /// <summary>
    /// 游戏运行时状态（场景间传递，不持久化）
    /// </summary>
    public class GameState
    {
        // 场景转换状态
        public bool ContinuingFromReward = false;
        public int PendingLevel = 1;
        public int PendingTargetScore = 200;
        public List<string> PendingDiceTypeIds = new List<string>();
        
        // 教程模式
        public bool IsTutorialMode = false;
        
        // 游戏结束状态
        public int GameOverFinalScore = 0;
        public int GameOverTargetScore = 0;
        
        public void Reset()
        {
            ContinuingFromReward = false;
            PendingLevel = 1;
            PendingTargetScore = 200;
            PendingDiceTypeIds.Clear();
            IsTutorialMode = false;
            GameOverFinalScore = 0;
            GameOverTargetScore = 0;
        }
    }
}

