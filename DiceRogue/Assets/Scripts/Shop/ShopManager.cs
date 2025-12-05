using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using DiceGame;           // BaseDice, DiceTier, DiceManager
using DiceGame.Core;      // GameStateManager
using DiceGame.Relics;    // RelicBase, RelicManager, RelicRarity
using DiceGame.Analytics; // UnityGameAnalytics
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public enum BuyResult
{
    Success,
    NotEnoughMoney,
    BackpackFull
}

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public ShopItemUI[] choiceSlots;    // 自選 3 格（Normal/Rare）

    [Header("Relic Shop UI")]
    [Tooltip("Relic 商店用的 4 個欄位（對應 ShopScene 中的 Relic_Row 裡的 ShopItemCard）")]
    public ShopItemUI[] relicSlots;    // 4 個遺物欄位，preview anchor 內已放 relic prefab


    [System.Serializable]
    public class DiceUiMap
    {
        [Tooltip("Dice 類型的 key，建議填入 BaseDice 子類型名稱（例如 CounterDice、D8、PlusOne）")] public string diceNameKey;
        [Tooltip("對應的骰子 UI 變體 prefab（顯示用）")] public GameObject uiPrefab;
        [Tooltip("Common 外框（可選）")] public GameObject framePrefabCommon;
        [Tooltip("Rare 外框（可選）")] public GameObject framePrefabRare;
        [Tooltip("Legendary 外框（可選）")] public GameObject framePrefabLegendary;
    }

    [Header("Dice UI Prefabs Mapping")]
    [Tooltip("把 BaseDice 子類名 → Dice UI 變體 prefab 的對照填在這裡")] public List<DiceUiMap> diceUiMaps = new List<DiceUiMap>();


    private static string SafeText(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? string.Empty : s;
    }

    private static string SafeName(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "(Unnamed Dice)" : s;
    }

    [Header("Pricing")]
    public int normalPrice = 6;         // 自選 Common 價
    public int rarePrice = 10;          // 自選 Rare 價
    public int legendaryPrice = 15;     // 傳奇價

    [Header("Relic Pricing")]
    [Tooltip("Common 遺物價格（比骰子高）")]
    public int relicCommonPrice = 10;
    [Tooltip("Rare 遺物價格（明顯比骰子貴）")]
    public int relicRarePrice = 15;
    [Tooltip("Legendary 遺物價格（極高價）")]
    public int relicLegendaryPrice = 20;

    [Header("Relic Tier Weights (抽選機率)")]
    [Tooltip("Common 遺物權重（最常出現）")] public float relicWeightCommon = 1.0f;
    [Tooltip("Rare 遺物權重（中等機率）")] public float relicWeightRare = 0.4f;
    [Tooltip("Legendary 遺物權重（非常低，確保稀有）")] public float relicWeightLegendary = 0.1f;

    [Header("Tier Weights (抽選機率)")]
    [Tooltip("Common 的權重（數值越大越常出現）")] public float weightCommon = 1.0f;
    [Tooltip("Normal 的權重（有的專案與 Common 等價，仍獨立提供以便調整）")] public float weightNormal = 1.0f;
    [Tooltip("Rare 的權重（建議 < 1）")] public float weightRare = 0.3f;
    [Tooltip("Legendary 的權重（建議很小，確保機率最低）")] public float weightLegendary = 0.1f;

    [Header("Probabilities")]
    [Range(0f, 1f)] public float legendaryAppearChance = 0.15f; // 傳奇出現機率

    [Header("Player Resource Manager")]
    [Tooltip("玩家資源管理器（跨場景單例，管理金錢、骰子、遺物）")]
    [SerializeField, HideInInspector]
    private PlayerResourceManager _resourceManager;

    [Header("Debug Gold (Temp for Testing)")]
    public bool useDebugGold = false;
    public int debugStartGold = 20;
    private int debugGold;

    [Header("Gold UI")] public TMP_Text walletText;

    [Header("Backpack UI (Optional)")]
    [Tooltip("整個背包區域的 Panel 根物件（用來顯示/隱藏整塊背包 UI）")]
    public GameObject backpackPanelRoot;
    [Tooltip("背包清單的父物件（通常掛有 Vertical Layout Group）")]
    public Transform backpackContentRoot;
    [Tooltip("用來顯示單個骰子資訊的簡單文字 prefab")]
    public GameObject backpackEntryPrefab;
    [Tooltip("切換 Dice / Relic 的按鈕（背包模式切換）")]
    public Button backpackSwitchButton;
    [Tooltip("切換按鈕上顯示的文字（預設顯示 RELIC，代表點擊後切換到遺物模式）")]
    public TMP_Text backpackSwitchLabel;

    // 當前背包顯示模式：false = Dice（預設），true = Relic
    private bool _showingRelics = false;


    [Header("Continue Button")]
    [Tooltip("Continue button to proceed to next level")]
    public Button continueButton;

    // 狀態
    private List<BaseDice> _choiceDice;        // 這次商店的 3 個自選
    private BaseDice _legendaryOffering;       // 這次商店的傳奇（可能為 null）
    private bool _legendarySold;

    private int _freeIndex = -1; // 本次商店中免費的那一格（其餘需付費）

    // 本次商店的遺物候選（Relic_Row 顯示的 4 個遺物）
    private List<RelicBase> _relicChoices = new List<RelicBase>();

    // 注意：不再使用本地 _backpackSnapshot，直接從 DiceManager.PlayerDiceBackpack 讀取

    private float GetTierWeight(DiceTier tier)
    {
        switch (tier)
        {
            case DiceTier.Legendary: return Mathf.Max(0f, weightLegendary);
            case DiceTier.Rare: return Mathf.Max(0f, weightRare);
            case DiceTier.Common:
            default: return Mathf.Max(0f, weightCommon);
        }
    }

    private float GetRelicWeight(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Legendary: return Mathf.Max(0f, relicWeightLegendary);
            case RelicRarity.Rare: return Mathf.Max(0f, relicWeightRare);
            case RelicRarity.Common:
            default: return Mathf.Max(0f, relicWeightCommon);
        }
    }

    private List<RelicBase> DrawWeightedUniqueRelics(List<RelicBase> pool, int count)
    {
        var remaining = new List<RelicBase>(pool);
        var result = new List<RelicBase>(Mathf.Min(count, remaining.Count));
        for (int pick = 0; pick < count && remaining.Count > 0; pick++)
        {
            float total = 0f;
            for (int i = 0; i < remaining.Count; i++)
            {
                total += GetRelicWeight(remaining[i].rarity);
            }
            if (total <= 0f)
            {
                int k = Random.Range(0, remaining.Count);
                result.Add(remaining[k]);
                remaining.RemoveAt(k);
                continue;
            }

            float r = Random.value * total;
            float acc = 0f;
            int chosen = 0;
            for (int i = 0; i < remaining.Count; i++)
            {
                acc += GetRelicWeight(remaining[i].rarity);
                if (r <= acc)
                {
                    chosen = i;
                    break;
                }
            }

            result.Add(remaining[chosen]);
            remaining.RemoveAt(chosen);
        }
        return result;
    }

    private List<BaseDice> DrawWeightedUnique(List<BaseDice> pool, int count)
    {
        // 依照權重（由稀有度決定）做「不重複」加權抽樣
        var remaining = new List<BaseDice>(pool);
        var result = new List<BaseDice>(Mathf.Min(count, remaining.Count));
        for (int pick = 0; pick < count && remaining.Count > 0; pick++)
        {
            // 累計權重
            float total = 0f;
            for (int i = 0; i < remaining.Count; i++) total += GetTierWeight(remaining[i].tier);
            if (total <= 0f)
            {
                // 權重都為 0 的極端情況，退化成等機率抽
                int k = Random.Range(0, remaining.Count);
                result.Add(remaining[k]);
                remaining.RemoveAt(k);
                continue;
            }

            // 按權重抽一個
            float r = Random.value * total;
            float acc = 0f;
            int chosen = 0;
            for (int i = 0; i < remaining.Count; i++)
            {
                acc += GetTierWeight(remaining[i].tier);
                if (r <= acc) { chosen = i; break; }
            }

            result.Add(remaining[chosen]);
            remaining.RemoveAt(chosen);
        }
        return result;
    }

    private void Awake()
    {
        // 獲取 PlayerResourceManager 實例（跨場景單例）
        _resourceManager = PlayerResourceManager.Instance;

        if (_resourceManager == null)
        {
            Debug.LogError("[Shop] PlayerResourceManager.Instance is null! Cannot initialize shop.");
            return;
        }

        Debug.Log("[Shop] PlayerResourceManager found successfully. Money and dice systems integrated.");

        // 同步資源管理器與 SaveData（確保數據是最新的）
        if (!useDebugGold)
        {
            _resourceManager.SyncAllFromSaveData();
            int currentMoney = _resourceManager.GetMoney();
            int saveDataMoney = GameStateManager.Instance?.SaveData?.money ?? 0;
            Debug.Log($"[Shop] Initial sync - PlayerResourceManager: ${currentMoney}, SaveData: ${saveDataMoney}");
        }

        // Temp debug gold for shop testing; 若不想用，將 useDebugGold 設為 false
        // ⚠️ WARNING: If useDebugGold is enabled in Inspector, it will override the real money system!
        if (useDebugGold)
        {
            debugGold = debugStartGold;
            Debug.LogWarning($"[Shop] ⚠️ DEBUG MODE ENABLED! Using debug gold ({debugGold}) instead of real money. To use real money, disable 'useDebugGold' in Inspector.");
        }
        else
        {
            Debug.Log("[Shop] Debug gold mode disabled. Using real money system from PlayerResourceManager.");
        }

        UpdateWalletUI();
    }

    private void Start()
    {
        // 預設打開背包面板
        if (backpackPanelRoot != null)
        {
            backpackPanelRoot.SetActive(true);
        }

        // 驗證並同步金錢系統狀態
        if (!useDebugGold && _resourceManager != null)
        {
            int moneyFromResourceManager = _resourceManager.GetMoney();
            int moneyFromSaveData = GameStateManager.Instance?.SaveData?.money ?? 0;
            Debug.Log($"[Shop] Money verification - PlayerResourceManager: ${moneyFromResourceManager}, SaveData: ${moneyFromSaveData}");

            if (moneyFromResourceManager != moneyFromSaveData)
            {
                Debug.LogWarning($"[Shop] ⚠️ Money mismatch detected! PlayerResourceManager has ${moneyFromResourceManager} but SaveData has ${moneyFromSaveData}. Syncing...");

                // 同步 PlayerResourceManager 與 SaveData
                _resourceManager.SyncMoneyFromSaveData();

                // 驗證同步後的值
                int syncedMoney = _resourceManager.GetMoney();
                Debug.Log($"[Shop] After sync - PlayerResourceManager: ${syncedMoney}, SaveData: ${moneyFromSaveData}");

                if (syncedMoney != moneyFromSaveData)
                {
                    Debug.LogError($"[Shop] ❌ Sync failed! PlayerResourceManager still has ${syncedMoney} but SaveData has ${moneyFromSaveData}");
                }
            }
        }

        // 更新錢包 UI（確保顯示正確的金額）
        UpdateWalletUI();

        // 設置繼續按鈕
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueToNextLevel);
            Debug.Log("[Shop] Continue button initialized");
        }
        else
        {
            Debug.LogWarning("[Shop] Continue button not assigned in Inspector!");
        }

        BuildShop();
        BuildBackpackSnapshot();

        // 初始化背包模式（預設顯示 Dice），並設定切換按鈕
        _showingRelics = false; // 預設顯示骰子
        SetupBackpackSwitchButton();

        RefreshBackpackUI();
        RenderShop();
    }

    /// <summary>生成本次商店內容。</summary>
    private void BuildShop()
    {
        _legendarySold = false;
        _legendaryOffering = null; // 不使用傳奇專區

        // 從非 Filler 的池子中，依權重（Legendary 權重最低）抽 5 顆，不重複
        var selectable = DicePool.GetNonFiller().ToList();
        
        // 過濾掉玩家已經擁有的骰子（根據類型名稱判斷）
        if (_resourceManager != null && _resourceManager.DiceManager != null)
        {
            var playerBackpack = _resourceManager.DiceManager.PlayerDiceBackpack;
            var ownedDiceTypes = new HashSet<string>(
                playerBackpack.Where(d => d != null).Select(d => d.GetType().Name)
            );
            
            int beforeCount = selectable.Count;
            selectable = selectable.Where(d => d != null && !ownedDiceTypes.Contains(d.GetType().Name)).ToList();
            
            if (selectable.Count == 0)
            {
                Debug.LogWarning("[Shop] All available dice are already owned by player. Showing empty shop.");
            }
            else if (beforeCount > selectable.Count)
            {
                Debug.Log($"[Shop] Filtered out {beforeCount - selectable.Count} owned dice from shop pool.");
            }
        }
        
        _choiceDice = DrawWeightedUnique(selectable, 5);

        // 確保本次商店中至少有一顆不是 Legendary 的骰子，才能保證一定有 FREE reward
        bool hasNonLegendary = _choiceDice.Any(d => d != null && d.tier != DiceTier.Legendary);
        if (!hasNonLegendary)
        {
            // 從整個池子裡找非 Legendary 的骰子進行替換
            var nonLegendPool = selectable.Where(d => d != null && d.tier != DiceTier.Legendary).ToList();
            if (nonLegendPool.Count > 0)
            {
                var replacement = nonLegendPool[Random.Range(0, nonLegendPool.Count)];
                if (_choiceDice.Count > 0)
                    _choiceDice[0] = replacement;   // 用第一格替換成非 Legendary
                else
                    _choiceDice.Add(replacement);
            }
            // 如果整個池子真的全部都是 Legendary，就沒辦法保證 FREE（極端情況），沿用原本結果
        }

        // 現在保證 _choiceDice 中至少有一顆非 Legendary，
        // 找到第一個非 Legendary 並把它換到 index 0，作為固定的 FREE 位置。
        _freeIndex = -1;
        if (_choiceDice.Count > 0)
        {
            int firstNonLegendIdx = -1;
            for (int i = 0; i < _choiceDice.Count; i++)
            {
                var d = _choiceDice[i];
                if (d != null && d.tier != DiceTier.Legendary)
                {
                    firstNonLegendIdx = i;
                    break;
                }
            }

            if (firstNonLegendIdx >= 0)
            {
                if (firstNonLegendIdx != 0)
                {
                    var tmp = _choiceDice[0];
                    _choiceDice[0] = _choiceDice[firstNonLegendIdx];
                    _choiceDice[firstNonLegendIdx] = tmp;
                }
                _freeIndex = 0; // 第一格永遠是 FREE
            }
        }

        // Log this shop's dice lineup for debugging
        if (_choiceDice != null && _choiceDice.Count > 0)
        {
            var sb = new StringBuilder();
            sb.Append("[Shop] Dice offerings: ");
            for (int i = 0; i < _choiceDice.Count; i++)
            {
                var d = _choiceDice[i];
                if (d == null) continue;

                int price;
                if (i == _freeIndex)
                {
                    price = 0;
                }
                else
                {
                    price = d.tier == DiceTier.Legendary ? legendaryPrice :
                            d.tier == DiceTier.Rare ? rarePrice :
                            normalPrice;
                }

                if (i > 0) sb.Append(" | ");
                sb.AppendFormat("#{0}: {1} (Tier={2}, Price={3}{4})",
                    i,
                    SafeName(d.diceName),
                    d.tier,
                    price,
                    (i == _freeIndex ? ", FREE SLOT" : string.Empty));
            }
            Debug.Log(sb.ToString());
        }

        // 同時建立本次商店要販售的遺物（Relic_Row）
        BuildRelicShop();
    }

    /// <summary>
    /// 生成本次商店要販售的遺物（從 RelicManager 的 GlobalRelicPool 中加權抽 4 個）。
    /// </summary>
    private void BuildRelicShop()
    {
        _relicChoices.Clear();

        if (_resourceManager == null)
        {
            Debug.LogWarning("[Shop/Relic] PlayerResourceManager is null. Cannot build relic shop.");
            return;
        }

        if (_resourceManager.RelicManager == null)
        {
            Debug.LogWarning("[Shop/Relic] RelicManager is null on PlayerResourceManager. Cannot build relic shop.");
            return;
        }

        var relicManager = _resourceManager.RelicManager;

        // 初始 debug：檢查 GlobalRelicPool 狀態
        var globalPool = relicManager.GlobalRelicPool;
        int initialCount = (globalPool != null) ? globalPool.Count : -1;
        Debug.Log($"[Shop/Relic] BuildRelicShop start. GlobalRelicPool initial count = {initialCount}");

        // 若 GlobalRelicPool 為空，嘗試重新初始化一次（防止其他地方忘記呼叫 InitializeGlobalRelicPool）
        if (globalPool == null || globalPool.Count == 0)
        {
            Debug.LogWarning("[Shop/Relic] GlobalRelicPool is empty. Attempting to re-initialize from code...");
            relicManager.InitializeGlobalRelicPool();

            globalPool = relicManager.GlobalRelicPool;
            int afterInitCount = (globalPool != null) ? globalPool.Count : -1;
            Debug.Log($"[Shop/Relic] After InitializeGlobalRelicPool, GlobalRelicPool count = {afterInitCount}");

            if (globalPool == null || globalPool.Count == 0)
            {
                Debug.LogWarning("[Shop/Relic] GlobalRelicPool still empty after re-init. No relics to sell.");
                return;
            }
        }

        // 過濾掉 null，並轉成 List
        var poolList = globalPool.Where(r => r != null).ToList();
        if (poolList.Count == 0)
        {
            Debug.LogWarning("[Shop/Relic] All relics in pool are null. Cannot build relic shop.");
            return;
        }

        // 過濾掉玩家已經擁有的遺物（根據遺物名稱判斷）
        var playerRelics = _resourceManager.GetPlayerRelics();
        if (playerRelics != null && playerRelics.Count > 0)
        {
            var ownedRelicNames = new HashSet<string>(
                playerRelics.Where(r => r != null).Select(r => r.relicName)
            );
            
            int beforeCount = poolList.Count;
            poolList = poolList.Where(r => !ownedRelicNames.Contains(r.relicName)).ToList();
            
            if (poolList.Count == 0)
            {
                Debug.LogWarning("[Shop/Relic] All available relics are already owned by player. Showing empty relic shop.");
            }
            else
            {
                Debug.Log($"[Shop/Relic] Filtered out {beforeCount - poolList.Count} owned relics from shop pool.");
            }
        }

        int relicCount = (relicSlots != null && relicSlots.Length > 0) ? relicSlots.Length : 4;
        _relicChoices = DrawWeightedUniqueRelics(poolList, relicCount);

        Debug.Log($"[Shop/Relic] Built relic shop with {_relicChoices.Count} offerings.");

        // Log this shop's relic lineup for debugging
        if (_relicChoices != null && _relicChoices.Count > 0)
        {
            var sb = new StringBuilder();
            sb.Append("[Shop/Relic] Relic offerings: ");
            for (int i = 0; i < _relicChoices.Count; i++)
            {
                var relic = _relicChoices[i];
                if (relic == null) continue;

                int relicPrice = relic.rarity == RelicRarity.Legendary
                    ? relicLegendaryPrice
                    : (relic.rarity == RelicRarity.Rare ? relicRarePrice : relicCommonPrice);

                if (i > 0) sb.Append(" | ");
                sb.AppendFormat("#{0}: {1} (Rarity={2}, Price={3})",
                    i,
                    relic.relicName,
                    relic.rarity,
                    relicPrice);
            }
            Debug.Log(sb.ToString());
        }
    }

    /// <summary>把商品綁到 UI。</summary>
    private void RenderShop()
    {
        // 綁定 5 個商品（若 choiceSlots 少於 5，則顯示可用數量）
        for (int i = 0; i < choiceSlots.Length; i++)
        {
            if (i < _choiceDice.Count)
            {
                var die = _choiceDice[i];
                int price;
                if (i == _freeIndex)
                {
                    price = 0; // 這一格免費
                }
                else
                {
                    // 其餘依稀有度決定價格
                    price = die.tier == DiceTier.Legendary ? legendaryPrice :
                            die.tier == DiceTier.Rare ? rarePrice :
                            normalPrice;
                }

                var slot = choiceSlots[i];
                slot.gameObject.SetActive(true);
                var preview = GetUiPreviewPrefab(die);
                slot.Bind(
                    preview,
                    SafeName(die.diceName),
                    price,
                    () => OnBuySpecificDie(die, price, slot)
                );
                slot.die = die;
            }
            else
            {
                choiceSlots[i].gameObject.SetActive(false);
            }
        }

        // ======= Render Relic Shop (Relic_Row) =======
        if (relicSlots != null && relicSlots.Length > 0)
        {
            for (int i = 0; i < relicSlots.Length; i++)
            {
                var slot = relicSlots[i];
                if (slot == null) continue;

                if (_relicChoices != null && i < _relicChoices.Count)
                {
                    var relic = _relicChoices[i];
                    if (relic == null)
                    {
                        slot.gameObject.SetActive(false);
                        continue;
                    }

                    int relicPrice = relic.rarity == RelicRarity.Legendary
                        ? relicLegendaryPrice
                        : (relic.rarity == RelicRarity.Rare ? relicRarePrice : relicCommonPrice);

                    // 先標記這張卡是「遺物卡片」，讓 ShopItemUI / SetPreview 可以讀到 relic
                    slot.die = null;
                    slot.relic = relic;

                    // 對於遺物欄位，目前不傳專屬 preview prefab（傳 null 讓 SetPreview 依稀有度上色）
                    slot.gameObject.SetActive(true);
                    slot.Bind(
                        null,
                        relic.relicName,
                        relicPrice,
                        () => OnBuyRelic(relic, relicPrice, slot)
                    );

                    Debug.Log($"[Shop/Relic] Rendered relic slot {i} -> {relic.relicName} ({relic.rarity}), price={relicPrice}");
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }
    }

    private void BuildBackpackSnapshot()
    {
        // 不再需要本地快照，直接從 PlayerResourceManager.DiceManager.PlayerDiceBackpack 讀取
        // 此方法保留以保持兼容性，但實際數據來源已改為 PlayerResourceManager
        int diceCount = _resourceManager?.DiceManager?.PlayerDiceBackpack.Count ?? 0;
        Debug.Log($"[Shop] Building backpack snapshot from PlayerResourceManager: {diceCount} dice");
    }

    // Helper to attach tooltip triggers to backpack entries and previews
    private void AttachBackpackTooltipTrigger(GameObject targetGo, BaseDice die)
    {
        if (!targetGo || die == null) return;

        // 確保這個物件上有 Graphic，可接收 Raycast
        var graphic = targetGo.GetComponent<Graphic>();
        if (graphic == null)
        {
            var img = targetGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            graphic = img;
        }
        else
        {
            graphic.raycastTarget = true;
        }

        var hover = targetGo.GetComponent<ShopBackpackHover>();
        if (!hover)
        {
            hover = targetGo.AddComponent<ShopBackpackHover>();
        }

        hover.die = die;
    }

    private void AttachRelicBackpackTooltipTrigger(GameObject targetGo, RelicBase relic)
    {
        if (!targetGo || relic == null) return;

        var graphic = targetGo.GetComponent<Graphic>();
        if (graphic == null)
        {
            var img = targetGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            graphic = img;
        }
        else
        {
            graphic.raycastTarget = true;
        }

        var hover = targetGo.GetComponent<ShopBackpackHover>();
        if (!hover)
        {
            hover = targetGo.AddComponent<ShopBackpackHover>();
        }

        hover.relic = relic;
    }

    private void RefreshBackpackUI()
    {
        Debug.Log("[Shop/Backpack] RefreshBackpackUI called");
        if (!backpackContentRoot || !backpackEntryPrefab) return;

        // 先清空現有項目
        foreach (Transform child in backpackContentRoot)
        {
            Destroy(child.gameObject);
        }

        if (_showingRelics)
        {
            RefreshRelicBackpackUI();
        }
        else
        {
            RefreshDiceBackpackUI();
        }
    }

    private void RefreshDiceBackpackUI()
    {
        // 檢查 PlayerResourceManager 是否已初始化
        if (_resourceManager == null || _resourceManager.DiceManager == null)
        {
            Debug.LogWarning("[Shop/Backpack] PlayerResourceManager or DiceManager is null. Cannot refresh dice backpack.");
            return;
        }

        // 從 PlayerResourceManager.DiceManager.PlayerDiceBackpack 讀取數據
        var backpackDice = _resourceManager.DiceManager.PlayerDiceBackpack;
        Debug.Log($"[Shop/Backpack] Displaying {backpackDice.Count} dice from PlayerResourceManager");

        // 重新建立列表
        foreach (var die in backpackDice)
        {
            if (die == null) continue;

            var entry = Instantiate(backpackEntryPrefab, backpackContentRoot);

            // 1) 產生視覺預覽：使用和商店一樣的骰子 UI prefab
            GameObject previewInstance = null;
            var previewPrefab = GetUiPreviewPrefab(die);
            if (previewPrefab != null)
            {
                previewInstance = Instantiate(previewPrefab, entry.transform);
                previewInstance.transform.localScale = Vector3.one;
                
                // 設置 DiceUI_Base 的 Image 大小為 80x80
                Transform diceUIBase = previewInstance.transform.Find("DiceUI_Base");
                if (diceUIBase != null)
                {
                    var rectTransform = diceUIBase.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.sizeDelta = new Vector2(80f, 80f);
                    }
                }
            }

            // 2) 若背包 entry 內有 TMP_Text，顯示骰子名稱
            var label = entry.GetComponentInChildren<TMP_Text>();
            if (label)
            {
                label.text = SafeName(die.diceName);
            }

            // 3) 在背包 entry 與實際顯示的骰子圖像上掛 Tooltip 事件
            AttachBackpackTooltipTrigger(entry.gameObject, die);
            if (previewInstance != null)
            {
                Transform hoverTarget = previewInstance.transform.Find("DiceUI_Base");
                if (hoverTarget != null)
                {
                    AttachBackpackTooltipTrigger(hoverTarget.gameObject, die);
                }
                else
                {
                    AttachBackpackTooltipTrigger(previewInstance, die);
                }
            }
        }
    }

    private void RefreshRelicBackpackUI()
    {
        if (_resourceManager == null)
        {
            Debug.LogWarning("[Shop/Backpack] PlayerResourceManager is null. Cannot display relic backpack.");
            return;
        }

        // 透過 PlayerResourceManager 取得玩家遺物背包
        var playerRelics = _resourceManager.GetPlayerRelics();
        if (playerRelics == null || playerRelics.Count == 0)
        {
            Debug.Log("[Shop/Backpack] No relics in player backpack (this does NOT affect shop offerings).");
            return;
        }

        Debug.Log($"[Shop/Backpack] Displaying {playerRelics.Count} relics from PlayerResourceManager");

        foreach (var relic in playerRelics)
        {
            if (relic == null) continue;

            var entry = Instantiate(backpackEntryPrefab, backpackContentRoot);

            // 這裡暫時用純文字顯示遺物名稱，未來可改為專用遺物 UI prefab
            var label = entry.GetComponentInChildren<TMP_Text>();
            if (label)
            {
                label.text = $"{relic.relicName}";
            }

            // 掛上遺物的 tooltip（DiceTooltipManager 有 overload ShowTooltip(RelicBase)）
            AttachRelicBackpackTooltipTrigger(entry.gameObject, relic);

            // 根據遺物稀有度改變背包色塊顏色（Common / Rare / Legendary）
            var bgImage = entry.GetComponentInChildren<Image>();
            if (bgImage == null)
            {
                // 若 prefab 上沒有 Image，就在根物件加一個當背景用色塊
                bgImage = entry.AddComponent<Image>();
                bgImage.raycastTarget = false;
            }

            Color rarityColor;
            switch (relic.rarity)
            {
                case RelicRarity.Common:
                    rarityColor = new Color32(143, 238, 143, 255);   // Light Green
                    break;
                case RelicRarity.Rare:
                    rarityColor = new Color32(147, 112, 219, 255);   // Purple
                    break;
                case RelicRarity.Legendary:
                    rarityColor = new Color32(255, 215, 0, 255);     // Gold
                    break;
                default:
                    rarityColor = Color.white;
                    break;
            }

            bgImage.color = rarityColor;
        }
    }

    private void SetupBackpackSwitchButton()
    {
        if (backpackSwitchButton != null)
        {
            backpackSwitchButton.onClick.RemoveAllListeners();
            backpackSwitchButton.onClick.AddListener(OnClickToggleBackpackMode);
        }

        UpdateBackpackSwitchLabel();
    }

    private void UpdateBackpackSwitchLabel()
    {
        if (backpackSwitchLabel == null) return;

        // 規則：
        //  - 當前顯示 Dice 時，按鈕顯示 "RELIC"（代表點擊會切換到遺物）
        //  - 當前顯示 Relic 時，按鈕顯示 "DICE"
        backpackSwitchLabel.text = _showingRelics ? "DICE" : "RELIC";
    }

    /// <summary>
    /// 點擊切換背包顯示模式（Dice / Relic）
    /// </summary>
    public void OnClickToggleBackpackMode()
    {
        _showingRelics = !_showingRelics;
        Debug.Log($"[Shop/Backpack] Toggle mode -> now showing: {(_showingRelics ? "Relics" : "Dice")}");
        UpdateBackpackSwitchLabel();
        RefreshBackpackUI();
    }

    private GameObject GetUiPreviewPrefab(BaseDice die)
    {
        if (die == null) return null;
        // 1) 依類型名稱找對應變體
        string key = die.GetType().Name;
        var map = diceUiMaps != null ? diceUiMaps.Find(m => m != null && m.diceNameKey == key) : null;
        if (map != null && map.uiPrefab != null) return map.uiPrefab;

        // 2) 若沒有專屬變體，根據稀有度回傳外框（若有設定）
        if (map != null)
        {
            switch (die.tier)
            {
                case DiceTier.Legendary: if (map.framePrefabLegendary) return map.framePrefabLegendary; break;
                case DiceTier.Rare: if (map.framePrefabRare) return map.framePrefabRare; break;
                default: if (map.framePrefabCommon) return map.framePrefabCommon; break;
            }
        }

        // 3) 全域找一個可用的通用外框（Common）
        if (diceUiMaps != null)
        {
            foreach (var m in diceUiMaps)
            {
                if (m != null && m.framePrefabCommon != null) return m.framePrefabCommon;
            }
        }
        return null;
    }

    // ================= 購買邏輯 =================


    private BuyResult OnBuySpecificDie(BaseDice die, int price, ShopItemUI ui)
    {
        if (die == null)
        {
            Debug.LogWarning("[Shop] OnBuySpecificDie called with null die.");
            return BuyResult.NotEnoughMoney; // fallback: treat as failed purchase
        }

        // 背包最多只能有 8 顆骰子（重複沒關係，只限制數量）
        if (_resourceManager != null && _resourceManager.DiceManager != null)
        {
            var backpackList = _resourceManager.DiceManager.PlayerDiceBackpack;
            int currentCount = backpackList != null ? backpackList.Count : 0;
            if (currentCount >= 8)
            {
                Debug.Log("[Shop] Backpack is full (8 dice). Cannot buy more.");
                return BuyResult.BackpackFull;
            }
        }
        else
        {
            Debug.LogWarning("[Shop] ResourceManager or DiceManager is null when buying specific die.");
            // 視為購買失敗（不區分原因），回傳 NotEnoughMoney 讓 UI 顯示通用錯誤
            return BuyResult.NotEnoughMoney;
        }

        // 處理金額：價格大於 0 才扣錢
        if (price > 0 && !Spend(price))
        {
            // Spend() 會自行印出金額不足的 log
            return BuyResult.NotEnoughMoney;
        }

        // 建立骰子實例
        var copy = CloneDice(die);

        // 使用 PlayerResourceManager 添加骰子到背包
        bool success = _resourceManager.AddDiceToBackpack(copy);
        if (!success)
        {
            Debug.LogWarning($"[Shop] Failed to add dice to backpack: {die.diceName} (may be duplicate)");
            // 即使添加失敗，錢已經扣了，維持購買成功的流程，避免卡住 UI
        }

        // 更新商店內背包顯示
        RefreshBackpackUI();

        // 更新錢包 UI
        UpdateWalletUI();

        Debug.Log($"[Shop] Bought: {die.diceName}");
        Debug.Log($"[Shop] Player purchased dice -> Name: {die.diceName}, Tier: {die.tier}, PricePaid: {price}");
        return BuyResult.Success;
    }

    private BuyResult OnBuyRelic(RelicBase relic, int price, ShopItemUI ui)
    {
        if (relic == null)
        {
            Debug.LogWarning("[Shop/Relic] OnBuyRelic called with null relic.");
            return BuyResult.NotEnoughMoney;
        }

        // 先確認金錢是否足夠
        if (price > 0 && !Spend(price))
        {
            // Spend() 會自行印出金額不足 log
            return BuyResult.NotEnoughMoney;
        }

        if (_resourceManager == null)
        {
            Debug.LogError("[Shop/Relic] PlayerResourceManager is null. Cannot add relic to backpack.");
            return BuyResult.NotEnoughMoney;
        }

        bool success = _resourceManager.AddRelicToBackpack(relic);
        if (!success)
        {
            Debug.LogWarning($"[Shop/Relic] Failed to add relic to backpack: {relic.relicName} (may be unique duplicate)");
            // 即使添加失敗，錢已經扣了，維持購買成功流程避免卡住 UI
        }

        // 更新背包顯示 & 金錢顯示
        RefreshBackpackUI();
        UpdateWalletUI();

        // Track analytics event for relic purchase
        UnityGameAnalytics.TrackRelicFrequency(relic.relicName);
        // Debug.Log($"[Shop/Relic/Analytics] Tracked relic purchase: {relic.relicName}");

        Debug.Log($"[Shop/Relic] Bought relic: {relic.relicName} ({relic.rarity}), PricePaid: {price}");
        return BuyResult.Success;
    }

    private bool OnBuyLegendary(int price)
    {
        if (_legendarySold || _legendaryOffering == null) return false;
        if (!Spend(price)) return false;

        var copy = CloneDice(_legendaryOffering);

        // 使用 PlayerResourceManager 添加骰子到背包
        bool success = _resourceManager?.AddDiceToBackpack(copy) ?? false;
        if (!success)
        {
            Debug.LogWarning($"[Shop] Failed to add legendary dice to backpack: {_legendaryOffering.diceName} (may be duplicate)");
        }

        // 更新商店內背包顯示
        RefreshBackpackUI();

        UpdateWalletUI();

        _legendarySold = true;
        Debug.Log($"[Shop] Bought Legendary: {_legendaryOffering.diceName}");
        Debug.Log($"[Shop] Player purchased Legendary -> Name: {_legendaryOffering.diceName}, Tier: {_legendaryOffering.tier}, PricePaid: {price}");
        return true;
    }

    // ================= 工具 =================

    private bool Spend(int price)
    {
        // 1) Debug 模式：只用本地 debugGold（方便單獨測商店）
        if (useDebugGold)
        {
            if (debugGold < price)
            {
                Debug.Log("[Shop] Not enough gold (debug).");
                return false;
            }

            debugGold -= price;
            UpdateWalletUI();
            return true;
        }

        // 2) PlayerResourceManager 模式：從跨場景資源管理器取得金錢
        if (_resourceManager == null)
        {
            Debug.LogError("[Shop] PlayerResourceManager missing. Cannot spend money.");
            return false;
        }

        // Use PlayerResourceManager.SpendMoney() to deduct money
        // This will handle validation and persistence automatically
        bool success = _resourceManager.SpendMoney(price);
        if (!success)
        {
            Debug.Log($"[Shop] Not enough gold (PlayerResourceManager). Current: {_resourceManager.GetMoney()}, Required: {price}");
            return false;
        }

        Debug.Log($"[Shop] Money spent: {price}, Remaining: {_resourceManager.GetMoney()}");
        UpdateWalletUI();
        return true;
    }

    /// <summary>
    /// 產生玩家持有的獨立實例：
    /// - 建立新物件並複製所有欄位
    /// </summary>
    private BaseDice CloneDice(BaseDice src)
    {
        if (src == null) return null;

        // Create a new instance of the same runtime type and copy over fields via reflection.
        var t = src.GetType();
        var instance = System.Activator.CreateInstance(t) as BaseDice;
        if (instance == null) return src; // fallback to returning the source (shared) to avoid nulls

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var f in t.GetFields(flags))
        {
            if (f.IsInitOnly) continue; // skip readonly init-only fields
            var value = f.GetValue(src);
            f.SetValue(instance, value);
        }

        return instance;
    }

    private List<BaseDice> DrawUnique(List<BaseDice> src, int n)
    {
        var list = new List<BaseDice>(src);
        var result = new List<BaseDice>(n);
        for (int i = 0; i < n && list.Count > 0; i++)
        {
            int k = Random.Range(0, list.Count);
            result.Add(list[k]);
            list.RemoveAt(k);
        }
        return result;
    }

    private void UpdateWalletUI()
    {
        if (!walletText) return;

        if (useDebugGold)
        {
            Debug.LogWarning("[Shop] ⚠️ DEBUG MODE ENABLED! Using debug gold instead of real money. Disable 'useDebugGold' in Inspector to use real money system.");
            walletText.text = "$ " + debugGold;
        }
        else if (_resourceManager != null)
        {
            int currentMoney = _resourceManager.GetMoney();
            Debug.Log($"[Shop] Displaying money from PlayerResourceManager: ${currentMoney}");
            walletText.text = "$ " + currentMoney;
        }
        else
        {
            Debug.LogWarning("[Shop] PlayerResourceManager not found. Displaying $0");
            walletText.text = "$ 0";
        }
    }


    public void OnClickToggleBackpack()
    {
        if (!backpackPanelRoot)
        {
            Debug.LogWarning("[Shop] OnClickToggleBackpack called, but backpackPanelRoot is NULL");
            return;
        }

        bool nowActive = backpackPanelRoot.activeSelf;
        bool nextActive = !nowActive;

        Debug.Log($"[Shop] ToggleBackpack -> wasActive={nowActive}, setActive={nextActive}, root={backpackPanelRoot.name}");
        backpackPanelRoot.SetActive(nextActive);
    }

    public void OnClickExitShop()
    {
        // 這邊改成你上一個場景的名稱
        SceneManager.LoadScene("BattleScene");
    }

    /// <summary>
    /// Continue to next level - loads BattleScene with next level state
    /// </summary>
    public void OnContinueToNextLevel()
    {
        var stateManager = GameStateManager.Instance;
        if (stateManager == null)
        {
            Debug.LogError("[Shop] GameStateManager.Instance is null! Cannot continue to next level.");
            return;
        }

        // Ensure ContinuingFromReward flag is set (should already be set by HandFlowController)
        if (!stateManager.State.ContinuingFromReward)
        {
            Debug.LogWarning("[Shop] ContinuingFromReward flag not set! Setting it now.");
            stateManager.State.ContinuingFromReward = true;
        }

        // Log next level info
        int nextLevel = stateManager.State.PendingLevel;
        int nextTarget = stateManager.State.PendingTargetScore;
        Debug.Log($"[Shop] Continuing to next level: Level {nextLevel}, Target Score: {nextTarget}");

        // BattleScene doesn't have a ScreenWipeFader, so we'll load it directly
        // This avoids the black screen issue
        Debug.Log("[Shop] Loading BattleScene directly (no fade animation needed)");
        SceneManager.LoadScene("BattleScene");
    }
}

// 專門用在商店背包區的 hover 腳本，避免跟隊友原本的 DiceUI 互相干擾
public class ShopBackpackHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public BaseDice die;
    [HideInInspector]
    public RelicBase relic;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DiceTooltipManager.Instance == null) return;

        if (die != null)
        {
            Debug.Log($"[Shop/BackpackHover] Enter on {name}, die={die.diceName}");
            DiceTooltipManager.Instance.ShowTooltip(die);
        }
        else if (relic != null)
        {
            Debug.Log($"[Shop/BackpackHover] Enter on {name}, relic={relic.relicName}");
            DiceTooltipManager.Instance.ShowTooltip(relic);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (DiceTooltipManager.Instance != null)
        {
            Debug.Log($"[Shop/BackpackHover] Exit on {name}");
            DiceTooltipManager.Instance.HideTooltip();
        }
    }
}