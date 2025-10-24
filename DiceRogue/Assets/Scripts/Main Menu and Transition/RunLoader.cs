using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRogue.Boot
{
    // Persistent loader that auto-finds the fader and never leaves you stuck black.
    public class RunLoader : MonoBehaviour
    {
        public static RunLoader Instance { get; private set; }

        [Header("Scene Names")]
        public string mainSceneName = "MainScene";
        public string battleSceneName = "BattleScene";

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
            StartCoroutine(GoToSceneRoutine(battleSceneName));
        }

        // Generic wipe transition to the specified scene
        public void GoToScene(string sceneName)
        {
            StartCoroutine(GoToSceneRoutine(sceneName));
        }

        // Public helper to return to Main Menu with the same wipe effect
        public void GoToMainMenu()
        {
            StartCoroutine(GoToSceneRoutine(mainSceneName));
        }

        public IEnumerator GoToSceneRoutine(string sceneName)
        {
            yield return LoadSceneWithWipe(sceneName);
        }

        IEnumerator LoadSceneWithWipe(string sceneName)
        {
            EnsureFader();
            if (wipeFader != null) yield return wipeFader.FadeOut();

            yield return SceneManager.LoadSceneAsync(sceneName);

            // Scene changed; fader may be under the persistent loader or scene—regrab it
            EnsureFader();
            if (wipeFader != null)
            {
                yield return wipeFader.FadeIn();
                wipeFader.ForceReveal();
            }
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
