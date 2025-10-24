using UnityEngine;
using DiceGame.Analytics;

namespace DiceGame.Testing
{
    /// <summary>
    /// Simple tester for Unity Analytics - attach to any GameObject to test
    /// </summary>
    public class AnalyticsTester : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private bool testOnStart = false;
        
        void Start()
        {
            if (testOnStart)
            {
                TestAnalytics();
            }
        }
        
        [ContextMenu("Test Analytics")]
        public void TestAnalytics()
        {
            Debug.Log("[AnalyticsTester] Testing Unity Analytics...");
            
            // Test session start
            UnityGameAnalytics.TrackSessionStarted();
            
            // Test level start
            UnityGameAnalytics.TrackLevelStarted(1, 300);
            
            // Test dice usage
            UnityGameAnalytics.TrackDiceUsed("Test Dice", "Common", 1, 1);
            
            // Test hand completion
            UnityGameAnalytics.TrackHandCompleted(1, 150, 150, "Three of a Kind", 3);
            
            // Test score combination
            UnityGameAnalytics.TrackScoreCombination("Three of a Kind", 60, 15, 1.5f, 1.0f, 150, 1);
            
            // Test battle completion
            UnityGameAnalytics.TrackBattleCompleted(1, 450, 300, true, 3, 120.5f);
            
            Debug.Log("[AnalyticsTester] All analytics tests completed!");
        }
        
        void Update()
        {
            // Press T to test analytics
            if (Input.GetKeyDown(KeyCode.T))
            {
                TestAnalytics();
            }
        }
    }
}
