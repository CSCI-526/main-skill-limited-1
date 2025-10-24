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
            Debug.Log("[AnalyticsTester] Testing simplified Unity Analytics...");
            
            // Test player progression
            UnityGameAnalytics.TrackPlayerProgression(450, 3, 1);
            
            // Test dice usage
            UnityGameAnalytics.TrackDiceUsage("Test Dice");
            UnityGameAnalytics.TrackDiceUsage("Heavy Dice");
            
            // Test score combinations
            UnityGameAnalytics.TrackScoreCombination("Three of a Kind");
            UnityGameAnalytics.TrackScoreCombination("Full House");
            
            Debug.Log("[AnalyticsTester] All simplified analytics tests completed!");
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
