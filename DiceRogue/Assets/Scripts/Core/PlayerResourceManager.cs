using System.Collections.Generic;
using UnityEngine;
using DiceGame.Relics;

namespace DiceGame
{
    /// <summary>
    /// 玩家资源管理器（单例，DontDestroyOnLoad）
    /// 管理跨场景的玩家资源：金钱、骰子背包、遗物等
    /// </summary>
    public class PlayerResourceManager : MonoBehaviour
    {
        private static PlayerResourceManager _instance;
        public static PlayerResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PlayerResourceManager");
                    _instance = go.AddComponent<PlayerResourceManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private MoneyManager _moneyManager;
        private DiceManager _diceManager;
        private RelicManager _relicManager;
        private GameStateManager _stateManager;

        /// <summary>
        /// 获取金钱管理器（只读访问）
        /// </summary>
        public MoneyManager MoneyManager => _moneyManager;

        /// <summary>
        /// 获取骰子管理器（只读访问）
        /// </summary>
        public DiceManager DiceManager => _diceManager;

        /// <summary>
        /// 获取遗物管理器（只读访问）
        /// </summary>
        public RelicManager RelicManager => _relicManager;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        /// <summary>
        /// 初始化所有资源管理器
        /// </summary>
        private void Initialize()
        {
            _stateManager = GameStateManager.Instance;

            // 初始化 MoneyManager（从 SaveData 加载）
            int initialMoney = _stateManager?.SaveData?.money ?? 0;
            _moneyManager = new MoneyManager(initialMoney);
            Debug.Log($"[PlayerResourceManager] Initialized MoneyManager with {initialMoney} money");

            // 初始化 DiceManager
            _diceManager = new DiceManager();
            _diceManager.InitializeGlobalDicePool();
            if (_stateManager?.SaveData != null)
            {
                _diceManager.LoadFromSaveData(_stateManager.SaveData);
            }
            Debug.Log($"[PlayerResourceManager] Initialized DiceManager with {_diceManager.PlayerDiceBackpack.Count} dice in backpack");

            // 初始化 RelicManager
            _relicManager = new RelicManager();
            _relicManager.InitializeGlobalRelicPool();
            if (_stateManager?.SaveData?.relicNames != null)
            {
                foreach (var relicName in _stateManager.SaveData.relicNames)
                {
                    _relicManager.AddRelicToBackpackByName(relicName);
                }
            }
            Debug.Log($"[PlayerResourceManager] Initialized RelicManager with {_relicManager.PlayerBackpack.Count} relics");

            Debug.Log("[PlayerResourceManager] All resource managers initialized successfully");
        }

        #region Money Methods

        /// <summary>
        /// 获取当前金钱
        /// </summary>
        public int GetMoney()
        {
            return _moneyManager?.Money ?? 0;
        }

        /// <summary>
        /// 添加金钱
        /// </summary>
        public void AddMoney(int amount)
        {
            if (_moneyManager != null)
            {
                _moneyManager.Add(amount);
                SaveMoneyToSaveData();
                Debug.Log($"[PlayerResourceManager] Money added: +{amount}, Total: {GetMoney()}");
            }
        }

        /// <summary>
        /// 花费金钱
        /// </summary>
        /// <returns>True if successful, false if insufficient funds</returns>
        public bool SpendMoney(int amount)
        {
            if (_moneyManager != null && _moneyManager.Subtract(amount))
            {
                SaveMoneyToSaveData();
                Debug.Log($"[PlayerResourceManager] Money spent: -{amount}, Remaining: {GetMoney()}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置金钱（用于同步）
        /// </summary>
        public void SetMoney(int amount)
        {
            if (_moneyManager != null)
            {
                _moneyManager.Set(amount);
                SaveMoneyToSaveData();
                Debug.Log($"[PlayerResourceManager] Money set to: {amount}");
            }
        }

        /// <summary>
        /// 同步 MoneyManager 与 SaveData
        /// </summary>
        public void SyncMoneyFromSaveData()
        {
            if (_stateManager?.SaveData != null && _moneyManager != null)
            {
                int saveDataMoney = _stateManager.SaveData.money;
                int currentMoney = _moneyManager.Money;

                if (currentMoney != saveDataMoney)
                {
                    Debug.Log($"[PlayerResourceManager] Syncing MoneyManager: {currentMoney} -> {saveDataMoney}");
                    _moneyManager.Set(saveDataMoney);
                }
            }
        }

        /// <summary>
        /// 保存金钱到 SaveData
        /// </summary>
        private void SaveMoneyToSaveData()
        {
            if (_stateManager?.SaveData != null && _moneyManager != null)
            {
                _stateManager.SaveData.money = _moneyManager.Money;
                _stateManager.Save();
            }
        }

        #endregion

        #region Dice Methods

        /// <summary>
        /// 添加骰子到背包
        /// </summary>
        public bool AddDiceToBackpack(BaseDice dice)
        {
            if (_diceManager != null)
            {
                bool success = _diceManager.AddDiceToBackpack(dice);
                if (success)
                {
                    SaveDiceToSaveData();
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// 通过名称添加骰子到背包
        /// </summary>
        public bool AddDiceToBackpackByName(string diceTypeName)
        {
            if (_diceManager != null)
            {
                bool success = _diceManager.AddDiceToBackpackByName(diceTypeName);
                if (success)
                {
                    SaveDiceToSaveData();
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// 获取玩家骰子背包
        /// </summary>
        public IReadOnlyList<BaseDice> GetPlayerDiceBackpack()
        {
            return _diceManager?.PlayerDiceBackpack ?? new List<BaseDice>();
        }

        /// <summary>
        /// 保存骰子到 SaveData
        /// </summary>
        private void SaveDiceToSaveData()
        {
            if (_stateManager?.SaveData != null && _diceManager != null)
            {
                _diceManager.SaveToSaveData(_stateManager.SaveData);
                _stateManager.Save();
            }
        }

        #endregion

        #region Relic Methods

        /// <summary>
        /// 添加遗物到背包
        /// </summary>
        public bool AddRelicToBackpack(RelicBase relic)
        {
            if (_relicManager != null)
            {
                bool success = _relicManager.AddRelicToBackpack(relic);
                if (success)
                {
                    SaveRelicsToSaveData();
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// 通过名称添加遗物到背包
        /// </summary>
        public bool AddRelicToBackpackByName(string relicName)
        {
            if (_relicManager != null)
            {
                bool success = _relicManager.AddRelicToBackpackByName(relicName);
                if (success)
                {
                    SaveRelicsToSaveData();
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// 获取玩家遗物背包
        /// </summary>
        public IReadOnlyList<RelicBase> GetPlayerRelics()
        {
            return _relicManager?.PlayerBackpack ?? new List<RelicBase>();
        }

        /// <summary>
        /// 保存遗物到 SaveData
        /// </summary>
        private void SaveRelicsToSaveData()
        {
            if (_stateManager?.SaveData != null && _relicManager != null)
            {
                _stateManager.SaveData.relicNames.Clear();
                foreach (var relic in _relicManager.PlayerBackpack)
                {
                    if (relic != null)
                    {
                        _stateManager.SaveData.relicNames.Add(relic.relicName);
                    }
                }
                _stateManager.Save();
            }
        }

        #endregion

        #region Sync Methods

        /// <summary>
        /// 从 SaveData 同步所有资源
        /// </summary>
        public void SyncAllFromSaveData()
        {
            SyncMoneyFromSaveData();

            if (_stateManager?.SaveData != null)
            {
                // 同步骰子
                if (_diceManager != null)
                {
                    _diceManager.LoadFromSaveData(_stateManager.SaveData);
                }

                // 同步遗物
                if (_relicManager != null)
                {
                    _relicManager.ClearBackpack();
                    foreach (var relicName in _stateManager.SaveData.relicNames)
                    {
                        _relicManager.AddRelicToBackpackByName(relicName);
                    }
                }
            }

            Debug.Log("[PlayerResourceManager] Synced all resources from SaveData");
        }

        /// <summary>
        /// 保存所有资源到 SaveData
        /// </summary>
        public void SaveAllToSaveData()
        {
            SaveMoneyToSaveData();
            SaveDiceToSaveData();
            SaveRelicsToSaveData();
            Debug.Log("[PlayerResourceManager] Saved all resources to SaveData");
        }

        #endregion

        #region Reset Methods

        /// <summary>
        /// 重置所有资源（用于新游戏）
        /// </summary>
        public void ResetAllResources()
        {
            if (_moneyManager != null)
            {
                _moneyManager.Reset();
            }

            if (_diceManager != null)
            {
                _diceManager.ClearBackpack();
            }

            if (_relicManager != null)
            {
                _relicManager.ClearBackpack();
            }

            SaveAllToSaveData();
            Debug.Log("[PlayerResourceManager] Reset all resources");
        }

        #endregion
    }
}

