using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace DiceGame.Analytics
{
    /// <summary>
    /// Unity Analytics system for tracking core game metrics:
    /// 1. Player rounds/score progression
    /// 2. Dice usage frequency
    /// 3. Score combination frequency
    /// </summary>
    public class UnityGameAnalytics : MonoBehaviour
    {
        private static UnityGameAnalytics instance;
        private bool isInitialized = false;
        
        async void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Initialize Unity Services
                try
                {
                    await UnityServices.InitializeAsync();
                    isInitialized = true;
                    Debug.Log("[UnityAnalytics] Unity Services initialized successfully");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[UnityAnalytics] Failed to initialize Unity Services: {e.Message}");
                    Debug.LogWarning("[UnityAnalytics] Analytics will work in debug mode only");
                    isInitialized = true; // Still allow debug logging
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Track when a hand is completed with score information
        /// </summary>
        public static void TrackHandCompleted(int handNumber, int finalScore, int totalScore, string comboName, int diceSubmitted)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping hand_completed event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping hand_completed event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"hand_number", handNumber},
                {"hand_score", finalScore},
                {"total_score", totalScore},
                {"combo_name", comboName},
                {"dice_submitted", diceSubmitted}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("hand_completed");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: hand_completed");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send hand_completed: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] hand_completed: Hand {handNumber}, Score: {finalScore}, Total: {totalScore}, Combo: {comboName}");
        }
        
        /// <summary>
        /// Track when a dice is used in a hand
        /// </summary>
        public static void TrackDiceUsed(string diceName, string diceTier, int diceCost, int handNumber)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping dice_used event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping dice_used event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"dice_name", diceName},
                {"dice_tier", diceTier},
                {"dice_cost", diceCost},
                {"hand_number", handNumber}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("dice_used");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: dice_used");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send dice_used: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] dice_used: {diceName} ({diceTier}) in hand {handNumber}");
        }
        
        /// <summary>
        /// Track score combinations achieved
        /// </summary>
        public static void TrackScoreCombination(string comboName, int baseScore, int diceSum, float comboMultiplier, float diceMultiplier, int finalScore, int handNumber)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping score_combination event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping score_combination event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"combo_name", comboName},
                {"base_score", baseScore},
                {"dice_sum", diceSum},
                {"combo_multiplier", comboMultiplier},
                {"dice_multiplier", diceMultiplier},
                {"final_score", finalScore},
                {"hand_number", handNumber}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("score_combination");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: score_combination");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send score_combination: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] score_combination: {comboName} = {finalScore} points in hand {handNumber}");
        }
        
        /// <summary>
        /// Track when a battle/level is completed
        /// </summary>
        public static void TrackBattleCompleted(int level, int totalScore, int targetScore, bool targetReached, int handsCompleted, float sessionDuration)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping battle_completed event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping battle_completed event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"level", level},
                {"total_score", totalScore},
                {"target_score", targetScore},
                {"target_reached", targetReached},
                {"hands_completed", handsCompleted},
                {"session_duration", sessionDuration}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("battle_completed");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: battle_completed");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send battle_completed: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] battle_completed: Level {level}, Score: {totalScore}/{targetScore}, Target: {targetReached}, Hands: {handsCompleted}");
        }
        
        /// <summary>
        /// Track when a new level starts
        /// </summary>
        public static void TrackLevelStarted(int level, int targetScore)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping level_started event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping level_started event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"level", level},
                {"target_score", targetScore}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("level_started");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: level_started");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send level_started: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] level_started: Level {level}, Target: {targetScore}");
        }
        
        /// <summary>
        /// Track when a player starts a new session
        /// </summary>
        public static void TrackSessionStarted()
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping session_started event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping session_started event");
                return;
            }
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("session_started");
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log("[UnityAnalytics] Sent to Unity Analytics: session_started");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send session_started: {e.Message}");
            }
            
            Debug.Log("[UnityAnalytics] session_started");
        }
    }
}
