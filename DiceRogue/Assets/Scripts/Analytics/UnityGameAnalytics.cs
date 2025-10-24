using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace DiceGame.Analytics
{
    /// <summary>
    /// Simplified Unity Analytics system for tracking 3 core metrics:
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
                    
                    // Start data collection (required for analytics to work)
                    AnalyticsService.Instance.StartDataCollection();
                    
                    isInitialized = true;
                    Debug.Log("[UnityAnalytics] Unity Services initialized and data collection started");
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
        /// Track player progression (score and rounds)
        /// </summary>
        public static void TrackPlayerProgression(int totalScore, int handsCompleted, int levelReached)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping player_progression event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping player_progression event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"total_score", totalScore},
                {"hands_completed", handsCompleted},
                {"level_reached", levelReached}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("player_progression");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: player_progression");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send player_progression: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] player_progression: Score {totalScore}, Hands {handsCompleted}, Level {levelReached}");
        }
        
        /// <summary>
        /// Track dice usage frequency
        /// </summary>
        public static void TrackDiceUsage(string diceName)
        {
            if (instance == null)
            {
                Debug.LogWarning("[UnityAnalytics] Instance not created yet, skipping dice_usage event");
                return;
            }
            
            if (!instance.isInitialized)
            {
                Debug.LogWarning("[UnityAnalytics] Unity Services not initialized, skipping dice_usage event");
                return;
            }
            
            var parameters = new Dictionary<string, object>
            {
                {"dice_name", diceName}
            };
            
            // Send to Unity Analytics
            try
            {
                var customEvent = new CustomEvent("dice_usage");
                foreach (var param in parameters)
                {
                    customEvent.Add(param.Key, param.Value);
                }
                AnalyticsService.Instance.RecordEvent(customEvent);
                Debug.Log($"[UnityAnalytics] Sent to Unity Analytics: dice_usage");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UnityAnalytics] Failed to send dice_usage: {e.Message}");
            }
            
            Debug.Log($"[UnityAnalytics] dice_usage: {diceName}");
        }
        
        /// <summary>
        /// Track score combination frequency
        /// </summary>
        public static void TrackScoreCombination(string comboName)
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
                {"combo_name", comboName}
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
            
            Debug.Log($"[UnityAnalytics] score_combination: {comboName}");
        }
        
    }
}
