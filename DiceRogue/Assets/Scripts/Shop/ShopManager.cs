using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using DiceGame;           // BaseDice, DiceTier
using DiceGame.Core;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    [Header("UI References")]
    public ShopItemUI blindBoxSlot;     // 盲盒（便宜抽）
    public ShopItemUI[] choiceSlots;    // 自選 3 格（Normal/Rare）
    public ShopItemUI legendarySlot;    // 傳奇（偶發、限購 1）
    public Sprite blindBoxIcon;         // 盲盒圖示（?）
    public Sprite defaultDiceIcon;        // 當骰子沒有專屬 icon 時的後備圖

    [Header("UI Prefab Previews")]
    [Tooltip("可選：盲盒在卡片上的預覽 prefab（如果不設，仍會使用盲盒圖示）")] public GameObject blindBoxPreviewPrefab;
    [Tooltip("可選：傳奇項目的外框/預覽 prefab（若不設，仍會使用文字/圖示")] public GameObject legendaryPreviewFrame;

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

    // runtime fallback icon (generated if neither candidate nor default provided)
    private Sprite _runtimeFallbackIcon;

    private Sprite SafeIcon(Sprite candidate)
    {
        if (candidate != null) return candidate;
        if (defaultDiceIcon != null) return defaultDiceIcon;
        if (_runtimeFallbackIcon == null)
        {
            // create a tiny transparent sprite as a last-resort fallback
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { Color.clear, Color.clear, Color.clear, Color.clear });
            tex.Apply();
            _runtimeFallbackIcon = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        return _runtimeFallbackIcon;
    }

    private static string SafeText(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? string.Empty : s;
    }

    private static string SafeName(string s)
    {
        return string.IsNullOrWhiteSpace(s) ? "(Unnamed Dice)" : s;
    }

    [Header("Pricing")]
    public int blindBoxPrice = 5;
    public int normalPrice = 6;         // 自選 Common 價
    public int rarePrice = 12;          // 自選 Rare 價
    public int legendaryPrice = 30;     // 傳奇價

    [Header("Tier Weights (抽選機率)")]
    [Tooltip("Common 的權重（數值越大越常出現）")] public float weightCommon = 1.0f;
    [Tooltip("Normal 的權重（有的專案與 Common 等價，仍獨立提供以便調整）")] public float weightNormal = 1.0f;
    [Tooltip("Rare 的權重（建議 < 1）")] public float weightRare = 0.35f;
    [Tooltip("Legendary 的權重（建議很小，確保機率最低）")] public float weightLegendary = 0.08f;

    [Header("Probabilities")]
    [Range(0f, 1f)] public float legendaryAppearChance = 0.15f; // 傳奇出現機率
    [Range(0f, 1f)] public float blindBoxNormalRate = 0.7f;     // 盲盒 Common 機率（其餘為 Rare）

    [Header("Player Refs")]
    public PlayerInventory inventory;   // 背包

    [Header("Battle Money")]
    [Tooltip("從這裡讀取與扣除金錢（BattleController.GetMoney / SpendMoney）")]
    [SerializeField, HideInInspector]
    private BattleController battleController;

    [Header("Debug Gold (Temp for Testing)")]
    public bool useDebugGold = true;
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

    [Header("Feedback UI")]
    [Tooltip("Purchase Failed feedback Text")]
    public TMP_Text feedbackText;
    [Tooltip("Duration to show feedback message")]
    public float feedbackDuration = 1.5f;
    private Coroutine feedbackRoutine;

    // 狀態
    private List<BaseDice> _choiceDice;        // 這次商店的 3 個自選
    private BaseDice _legendaryOffering;       // 這次商店的傳奇（可能為 null）
    private bool _legendarySold;

    private int _freeIndex = -1; // 本次商店中免費的那一格（其餘需付費）

    // 在商店中用來顯示的背包快照（目前僅在本場景維護，未來可從真正的 PlayerInventory 讀取）
    private readonly List<BaseDice> _backpackSnapshot = new List<BaseDice>();

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
        if (!inventory) inventory = PlayerInventory.I;

        // 自動尋找 BattleController（若未在 Inspector 指定）
        if (!battleController)
        {
            battleController = FindObjectOfType<BattleController>();
            if (!battleController)
            {
                Debug.LogWarning("[Shop] BattleController not found in scene. Money will stay at 0 unless debugGold is used.");
            }
        }

        // Temp debug gold for shop testing; 若不想用，將 useDebugGold 設為 false
        if (useDebugGold)
        {
            debugGold = debugStartGold;
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

        BuildShop();
        BuildBackpackSnapshot();
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
        _choiceDice = DrawWeightedUnique(selectable, 5);

        // 指定其中一格為免費，但「Legendary 不能免費」
        _freeIndex = -1;
        if (_choiceDice.Count > 0)
        {
            // 蒐集可免費的索引（非 Legendary）
            var freeCandidates = new List<int>();
            for (int i = 0; i < _choiceDice.Count; i++)
            {
                if (_choiceDice[i].tier != DiceTier.Legendary)
                    freeCandidates.Add(i);
            }
            if (freeCandidates.Count > 0)
            {
                int k = Random.Range(0, freeCandidates.Count);
                _freeIndex = freeCandidates[k];
            }
        }
    }

    /// <summary>把商品綁到 UI。</summary>
    private void RenderShop()
    {
        // 盲盒、傳奇專區暫不顯示（基本商店）
        if (blindBoxSlot) blindBoxSlot.gameObject.SetActive(false);
        if (legendarySlot) legendarySlot.gameObject.SetActive(false);

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
    }

    private void BuildBackpackSnapshot()
    {
        _backpackSnapshot.Clear();

        // TODO: 將來若有真正的 PlayerInventory，可改成從 inventory 讀取，例如：
        // if (inventory != null)
        // {
        //     var owned = inventory.GetAllDice();
        //     if (owned != null) _backpackSnapshot.AddRange(owned);
        // }
        // 目前尚未與隊友的背包系統串接，因此預設為空，之後購買的骰子會動態加入。
    }

    private void AddToBackpackView(BaseDice die)
    {
        if (die == null) return;
        _backpackSnapshot.Add(die);
        RefreshBackpackUI();
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

    private void RefreshBackpackUI()
    {
        Debug.Log("[Shop/Backpack] RefreshBackpackUI called");
        if (!backpackContentRoot || !backpackEntryPrefab) return;

        // 清空現有項目
        foreach (Transform child in backpackContentRoot)
        {
            Destroy(child.gameObject);
        }

        // 重新建立列表
        foreach (var die in _backpackSnapshot)
        {
            if (die == null) continue;

            // 建立一個背包欄位（可是一個空的 Panel / 卡片容器）
            var entry = Instantiate(backpackEntryPrefab, backpackContentRoot);

            // 1) 產生視覺預覽：使用和商店一樣的骰子 UI prefab
            GameObject previewInstance = null;
            var previewPrefab = GetUiPreviewPrefab(die);
            if (previewPrefab != null)
            {
                previewInstance = Instantiate(previewPrefab, entry.transform);
                previewInstance.transform.localScale = Vector3.one;
            }

            // 2) 若背包 entry 內有 TMP_Text，顯示名稱 + 稀有度（可選）
            var label = entry.GetComponentInChildren<TMP_Text>();
            if (label)
            {
                label.text = SafeName(die.diceName);
            }

            // 3) 在背包 entry 與實際顯示的骰子圖像上掛 Tooltip 事件
            //    注意：隊友的骰子 prefab 結構通常是 Root -> DiceUI_Base(Image) -> Text 等，
            //    真正吃到 raycast 的往往是 DiceUI_Base，因此優先在該子物件上掛 EventTrigger。
            AttachBackpackTooltipTrigger(entry.gameObject, die);
            if (previewInstance != null)
            {
                // 優先尋找名為 "DiceUI_Base" 的子物件來做 hover 目標
                Transform hoverTarget = previewInstance.transform.Find("DiceUI_Base");
                if (hoverTarget != null)
                {
                    AttachBackpackTooltipTrigger(hoverTarget.gameObject, die);
                }
                else
                {
                    // 找不到就 fallback 到整個 previewInstance
                    AttachBackpackTooltipTrigger(previewInstance, die);
                }
            }
        }
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

    private bool OnBuyBlindBox()
    {
        if (!Spend(blindBoxPrice)) return false;

        BaseDice got = null;
        if (Random.value < blindBoxNormalRate)
        {
            var normals = DicePool.GetByTier(DiceTier.Common);
            if (normals.Count > 0) got = CloneDice(normals[Random.Range(0, normals.Count)]);
        }
        else
        {
            var rares = DicePool.GetByTier(DiceTier.Rare);
            if (rares.Count > 0) got = CloneDice(rares[Random.Range(0, rares.Count)]);
        }

        if (got != null)
        {
            if (inventory != null)
            {
                inventory.AddDie(got);
            }
            else
            {
                Debug.LogWarning("[Shop] inventory is null, skipping AddDie for blind box (test mode)");
            }

            // 更新商店內背包顯示
            AddToBackpackView(got);

            UpdateWalletUI();
            Debug.Log($"[Shop] BlindBox => {got.diceName} ({got.tier})");
            Debug.Log($"[Shop] Player purchased BlindBox result -> Name: {got.diceName}, Tier: {got.tier}, PricePaid: {blindBoxPrice}");
        }
        else
        {
            Debug.LogWarning("[Shop] BlindBox => pool empty?");
        }

        return true;
    }

    private bool OnBuySpecificDie(BaseDice die, int price, ShopItemUI ui)
    {
        if (price > 0 && !Spend(price)) return false;

        var copy = CloneDice(die);
        if (inventory != null)
        {
            inventory.AddDie(copy);
        }
        else
        {
            Debug.LogWarning("[Shop] inventory is null, skipping AddDie (test mode)");
        }

        // 更新商店內背包顯示
        AddToBackpackView(copy);

        UpdateWalletUI();

        Debug.Log($"[Shop] Bought: {die.diceName}");
        Debug.Log($"[Shop] Player purchased dice -> Name: {die.diceName}, Tier: {die.tier}, PricePaid: {price}");
        return true;
    }

    private bool OnBuyLegendary(int price)
    {
        if (_legendarySold || _legendaryOffering == null) return false;
        if (!Spend(price)) return false;

        var copy = CloneDice(_legendaryOffering);
        if (inventory != null)
        {
            inventory.AddDie(copy);
        }
        else
        {
            Debug.LogWarning("[Shop] inventory is null, skipping AddDie for legendary (test mode)");
        }

        // 更新商店內背包顯示
        AddToBackpackView(copy);

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
                ShowFeedback("Insufficient Funds!");
                return false;
            }

            debugGold -= price;
            UpdateWalletUI();
            return true;
        }

        // 2) BattleController 模式：從戰鬥流程取得金錢
        if (!battleController)
        {
            Debug.LogError("[Shop] BattleController missing. Cannot spend money.");
            ShowFeedback("Insufficient Funds!");
            return false;
        }

        int currentMoney = battleController.GetMoney();
        if (currentMoney < price)
        {
            Debug.Log("[Shop] Not enough gold (battleController).");
            ShowFeedback("Insufficient Funds!");
            return false;
        }

        // TODO: 根據你隊友實作的 API 名稱來扣錢：
        // 範例 1：若有 battleController.SpendMoney(int amount)
        // battleController.SpendMoney(price);
        //
        // 範例 2：若有 battleController.AddMoney(int delta)
        // battleController.AddMoney(-price);

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
            walletText.text = "$ " + debugGold;
        }
        else if (battleController)
        {
            int currentMoney = battleController.GetMoney();
            walletText.text = "$ " + currentMoney;
        }
        else
        {
            walletText.text = "$ 0";
        }
    }

    private void ShowFeedback(string msg)
    {
        if (!feedbackText) return;

        if (feedbackRoutine != null)
        {
            StopCoroutine(feedbackRoutine);
            feedbackRoutine = null;
        }

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = msg;
        feedbackRoutine = StartCoroutine(ClearFeedbackAfterDelay());
    }

    private IEnumerator ClearFeedbackAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDuration);
        if (feedbackText)
        {
            feedbackText.gameObject.SetActive(false);
        }
        feedbackRoutine = null;
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
}

// 專門用在商店背包區的 hover 腳本，避免跟隊友原本的 DiceUI 互相干擾
public class ShopBackpackHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public BaseDice die;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (die == null) return;
        Debug.Log($"[Shop/BackpackHover] Enter on {name}, die={die.diceName}");
        if (DiceTooltipManager.Instance != null)
        {
            DiceTooltipManager.Instance.ShowTooltip(die);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (DiceTooltipManager.Instance != null)
        {
            Debug.Log($"[Shop/BackpackHover] Exit on {name}, die={die?.diceName}");
            DiceTooltipManager.Instance.HideTooltip();
        }
    }
}