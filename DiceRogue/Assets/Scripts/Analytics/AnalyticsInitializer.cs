using UnityEngine;
using DiceGame.Analytics;

namespace DiceGame
{
    /// <summary>
    /// Automatically creates the UnityGameAnalytics GameObject if it doesn't exist
    /// Attach this to any GameObject in your scene to ensure analytics is initialized
    /// </summary>
    public class AnalyticsInitializer : MonoBehaviour
    {
        void Awake()
        {
            // Check if UnityGameAnalytics already exists
            if (FindObjectOfType<UnityGameAnalytics>() == null)
            {
                // Create the analytics GameObject
                GameObject analyticsGO = new GameObject("UnityGameAnalytics");
                analyticsGO.AddComponent<UnityGameAnalytics>();
                
                Debug.Log("[AnalyticsInitializer] Created UnityGameAnalytics GameObject");
            }
            else
            {
                Debug.Log("[AnalyticsInitializer] UnityGameAnalytics already exists");
            }
        }
    }
}
