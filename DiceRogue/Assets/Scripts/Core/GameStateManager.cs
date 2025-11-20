using UnityEngine;

namespace DiceGame
{
    /// <summary>
    /// 游戏状态管理器（单例，DontDestroyOnLoad）
    /// 管理运行时状态和持久化数据
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        private static GameStateManager _instance;
        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("GameStateManager");
                    _instance = go.AddComponent<GameStateManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        // 运行时状态（不持久化）
        public GameState State { get; private set; }
        
        // 持久化数据
        public SaveData SaveData { get; private set; }
        
        private const string SAVE_KEY = "PlayerSaveData";
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            State = new GameState();
            LoadSaveData();
        }
        
        // 加载持久化数据
        public void LoadSaveData()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                SaveData = new SaveData();
            }
            else
            {
                SaveData = JsonUtility.FromJson<SaveData>(json);
            }
        }
        
        // 保存持久化数据
        public void Save()
        {
            string json = JsonUtility.ToJson(SaveData, true);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }
        
        // 重置运行时状态
        public void ResetState()
        {
            State.Reset();
        }
        
        // 重置持久化数据
        public void ResetSaveData()
        {
            SaveData = new SaveData();
            Save();
        }
    }
}

