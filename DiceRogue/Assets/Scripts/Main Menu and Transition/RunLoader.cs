using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRogue.Boot
{
    public class RunLoader : MonoBehaviour
    {
        public static RunLoader Instance { get; private set; }

        public string mainSceneName = "MainScene";
        public string battleSceneName = "BattleScene";

        // NEW: assign this in the Inspector
        public ScreenWipeFader wipeFader;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            if (wipeFader != null) StartCoroutine(wipeFader.FadeIn()); // reveal menu on boot
        }

        public void StartRun()
        {
            StartCoroutine(LoadSceneWithWipe(battleSceneName));
        }

        IEnumerator LoadSceneWithWipe(string sceneName)
        {
            if (wipeFader != null) yield return wipeFader.FadeOut();
            yield return SceneManager.LoadSceneAsync(sceneName);
            if (wipeFader != null) yield return wipeFader.FadeIn();
        }
    }
}
