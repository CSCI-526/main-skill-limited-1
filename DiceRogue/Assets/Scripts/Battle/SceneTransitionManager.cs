using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceGame
{
    /// <summary>
    /// 管理战斗场景的场景转换逻辑
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        /// <summary>
        /// Transition to reward scene after level completion
        /// </summary>
        public void TransitionToRewardScene()
        {
            Debug.Log("[SceneTransitionManager] Transitioning to RewardScene");
            SceneManager.LoadScene("RewardScene");
        }
        
        /// <summary>
        /// Transition to game over scene with wipe animation (with fallback)
        /// </summary>
        public IEnumerator TransitionToGameOverScene()
        {
            Debug.Log("[SceneTransitionManager] Transitioning to GameOverScene");
            
            bool transitionStarted = false;
            
            // Try to use RunLoader for wipe animation
            if (DiceRogue.Boot.RunLoader.Instance != null)
            {
                Debug.Log("[SceneTransitionManager] RunLoader.Instance found - calling LoadGameOverScene()");
                string sceneName = DiceRogue.Boot.RunLoader.Instance.gameOverSceneName;
                Debug.Log($"[SceneTransitionManager] GameOver scene name: '{sceneName}'");
                
                if (!string.IsNullOrEmpty(sceneName))
                {
                    DiceRogue.Boot.RunLoader.Instance.LoadGameOverScene();
                    Debug.Log("[SceneTransitionManager] LoadGameOverScene() called successfully - transition should start now");
                    transitionStarted = true;
                    
                    // Wait a few frames to ensure the transition coroutine starts and begins loading
                    yield return new WaitForSeconds(0.1f);
                    
                    // Check if we're still in BattleScene (transition might have failed)
                    string currentScene = SceneManager.GetActiveScene().name;
                    if (currentScene == "BattleScene")
                    {
                        Debug.LogWarning("[SceneTransitionManager] Still in BattleScene after transition attempt - waiting longer...");
                        yield return new WaitForSeconds(0.5f);
                        
                        // Check again
                        currentScene = SceneManager.GetActiveScene().name;
                        if (currentScene == "BattleScene")
                        {
                            Debug.LogError("[SceneTransitionManager] Transition failed! Using direct scene load as fallback.");
                            transitionStarted = false;
                        }
                    }
                }
                else
                {
                    Debug.LogError("[SceneTransitionManager] GameOver scene name is null or empty!");
                    transitionStarted = false;
                }
            }
            else
            {
                Debug.LogError("[SceneTransitionManager] RunLoader.Instance is NULL! Cannot use wipe transition.");
                transitionStarted = false;
            }
            
            // Fallback: Direct scene load if transition didn't work
            if (!transitionStarted)
            {
                Debug.LogWarning("[SceneTransitionManager] Using fallback: Direct SceneManager.LoadScene");
                SceneManager.LoadScene("GameOverScene");
            }
        }
    }
}

