using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using DiceGame;

namespace DiceRogue.Boot
{
    // Persistent loader that auto-finds the fader and never leaves you stuck black.
    public class RunLoader : MonoBehaviour
    {
        public static RunLoader Instance { get; private set; }

        [Header("Scene Names")]
        public string mainSceneName = "MainScene";
        public string battleSceneName = "BattleScene";
        public string tutorialSceneName = "TutorialScene";
        public string gameOverSceneName = "GameOverScene";

        [Header("Fader")]
        public ScreenWipeFader wipeFader;   // assign if you want, else we auto-find
        public bool autoFadeOnBoot = true;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureFader();
        }

        void Start()
        {
            EnsureFader();

            // If the overlay starts black, reveal it so the menu is visible/clickable.
            if (autoFadeOnBoot && wipeFader != null && wipeFader.IsCovered())
            {
                StartCoroutine(wipeFader.FadeIn());
            }
            else if (wipeFader != null && wipeFader.IsCovered())
            {
                // Emergency: if not auto-fading on boot, at least unblock clicks
                wipeFader.ForceReveal();
            }
        }

        public void StartRun()
        {
            GameStateManager.Instance.State.IsTutorialMode = false; // Ensure normal mode
            
            // Reset game data for new run (money and dice backpack)
            // Preserve bestScore and hasCompletedTutorial
            ResetGameDataForNewRun();
            
            StartCoroutine(LoadSceneWithWipe(battleSceneName));
        }
        
        /// <summary>
        /// Reset game data for a new run (money and dice backpack)
        /// Preserves bestScore and hasCompletedTutorial
        /// Uses PlayerResourceManager for cross-scene resource management
        /// </summary>
        private void ResetGameDataForNewRun()
        {
            var stateManager = GameStateManager.Instance;
            if (stateManager == null || stateManager.SaveData == null)
            {
                Debug.LogWarning("[RunLoader] Cannot reset game data - GameStateManager or SaveData is null");
                return;
            }
            
            // Save bestScore and hasCompletedTutorial before reset
            int bestScore = stateManager.SaveData.bestScore;
            bool hasCompletedTutorial = stateManager.SaveData.hasCompletedTutorial;
            
            // Reset money and dice backpack in SaveData
            stateManager.SaveData.money = 0;
            stateManager.SaveData.diceTypeIds.Clear();
            stateManager.SaveData.relicNames.Clear();
            
            // Restore preserved values
            stateManager.SaveData.bestScore = bestScore;
            stateManager.SaveData.hasCompletedTutorial = hasCompletedTutorial;
            
            // Reset PlayerResourceManager resources (if it exists)
            var resourceManager = PlayerResourceManager.Instance;
            if (resourceManager != null)
            {
                resourceManager.ResetAllResources();
                Debug.Log("[RunLoader] Reset PlayerResourceManager resources");
            }
            else
            {
                // If PlayerResourceManager doesn't exist yet, it will be initialized with the reset SaveData
                // Save the reset data so PlayerResourceManager can load it
                stateManager.Save();
                Debug.Log("[RunLoader] PlayerResourceManager not found yet - saved reset data to SaveData");
            }
            
            Debug.Log("[RunLoader] Reset game data for new run - money and dice backpack cleared");
        }

        public void StartTutorial()
        {
            GameStateManager.Instance.State.IsTutorialMode = true; // Set tutorial mode flag
            
            // Reset game data for tutorial run (money and dice backpack)
            // Preserve bestScore and hasCompletedTutorial
            ResetGameDataForNewRun();
            
            StartCoroutine(LoadSceneWithWipe(battleSceneName)); // Load BattleScene, not TutorialScene
        }

        public void LoadGameOverScene()
        {
            StartCoroutine(LoadSceneWithWipe(gameOverSceneName));
        }

        public IEnumerator LoadSceneWithWipe(string sceneName)
        {
            Debug.Log($"[RunLoader] LoadSceneWithWipe called for scene: '{sceneName}'");
            
            // Validate scene name
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[RunLoader] Scene name is null or empty! Cannot load scene.");
                yield break;
            }
            
            // Check if scene exists in build settings
            bool sceneExists = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    sceneExists = true;
                    Debug.Log($"[RunLoader] Scene '{sceneName}' found in build settings at index {i}");
                    break;
                }
            }
            
            if (!sceneExists)
            {
                Debug.LogError($"[RunLoader] Scene '{sceneName}' not found in build settings! Attempting direct load anyway...");
                // Try to load anyway - sometimes scenes exist but aren't in build settings
            }
            
            // Ensure fader exists
            EnsureFader();
            if (wipeFader == null)
            {
                Debug.LogWarning("[RunLoader] No fader found - skipping fade animation, loading scene directly");
            }
            else
            {
                Debug.Log("[RunLoader] Fader found - starting fade out");
                yield return wipeFader.FadeOut();
            }

            // Load scene
            Debug.Log($"[RunLoader] Loading scene '{sceneName}'...");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            if (asyncLoad == null)
            {
                Debug.LogError($"[RunLoader] Failed to start loading scene '{sceneName}'! AsyncOperation is null.");
                yield break;
            }
            
            // Wait for scene to load
            while (!asyncLoad.isDone)
            {
                Debug.Log($"[RunLoader] Loading scene '{sceneName}'... {asyncLoad.progress * 100:F0}%");
                yield return null;
            }
            
            Debug.Log($"[RunLoader] Scene '{sceneName}' loaded successfully!");

            // Scene changed; fader may be under the persistent loader or scene—regrab it
            // Wait a frame for scene to fully initialize
            yield return null;
            
            EnsureFader();
            if (wipeFader != null)
            {
                Debug.Log("[RunLoader] Fading in...");
                
                // Start fade in with timeout protection
                float fadeStartTime = Time.unscaledTime;
                float maxFadeTime = 2f; // Maximum 2 seconds for fade in
                
                // Start fade in coroutine
                StartCoroutine(wipeFader.FadeIn());
                
                // Wait for fade in with timeout protection
                while (wipeFader != null && wipeFader.IsCovered() && (Time.unscaledTime - fadeStartTime) < maxFadeTime)
                {
                    yield return null;
                }
                
                // If fade is taking too long or fader is still covered, force reveal
                if (wipeFader != null && wipeFader.IsCovered())
                {
                    Debug.LogWarning("[RunLoader] Fade in taking too long or fader still covered - forcing reveal");
                    wipeFader.ForceReveal();
                }
            }
            else
            {
                Debug.LogWarning("[RunLoader] No fader found after scene load - scene should be visible");
                // Try to find and force reveal any fader in the scene
                var faderInScene = FindObjectOfType<ScreenWipeFader>(true);
                if (faderInScene != null)
                {
                    Debug.Log("[RunLoader] Found fader in scene - forcing reveal");
                    faderInScene.ForceReveal();
                }
            }
            
            Debug.Log($"[RunLoader] LoadSceneWithWipe completed for '{sceneName}'");
        }

        void EnsureFader()
        {
            if (wipeFader != null) return;

            // 1) Child of this loader?
            wipeFader = GetComponentInChildren<ScreenWipeFader>(true);
            if (wipeFader != null) return;

            // 2) Anywhere in the active scene?
            wipeFader = FindObjectOfType<ScreenWipeFader>(true);
        }
    }
}
