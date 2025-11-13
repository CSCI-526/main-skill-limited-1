using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

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
            StartCoroutine(LoadSceneWithWipe(battleSceneName));
        }

        public void StartTutorial()
        {
            StartCoroutine(LoadSceneWithWipe(tutorialSceneName));
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
            EnsureFader();
            if (wipeFader != null)
            {
                Debug.Log("[RunLoader] Fading in...");
                yield return wipeFader.FadeIn();
            }
            else
            {
                Debug.LogWarning("[RunLoader] No fader found after scene load - skipping fade in");
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
