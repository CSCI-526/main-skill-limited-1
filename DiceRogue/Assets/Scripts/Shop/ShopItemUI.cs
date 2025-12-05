using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame;           // BaseDice
using DiceGame.Core;      // DiceHoverTooltip
using DiceGame.Relics;    // RelicBase
using UnityEngine.EventSystems;

public class ShopItemUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text priceText;
    public Button buyBtn;
    public Transform previewAnchor;
    public TMP_Text feedbackText;

    public BaseDice die;
    public RelicBase relic;   // 若此卡片代表遺物，會由 ShopManager 指定

    [Header("Sold Overlay")]
    public GameObject soldOverlay;

    // 資料
    private System.Func<BuyResult> _onBuy;  // 回呼：點「購買」時由 ShopManager 注入邏輯
    private bool _sold;
    private GameObject _currentPreview;

    public void Bind(GameObject previewPrefab, string name, int price, System.Func<BuyResult> onBuy)
    {
        SetPreview(previewPrefab);
        if (priceText)
        {
            if (price <= 0)
                priceText.text = "FREE";
            else
                priceText.text = $"$ {price}";
        }
        if (feedbackText)
        {
            feedbackText.text = string.Empty;
        }
        _onBuy = onBuy;
        _sold = false;
        if (soldOverlay)
            soldOverlay.SetActive(false);
        if (buyBtn)
        {
            buyBtn.onClick.RemoveAllListeners();
            buyBtn.onClick.AddListener(TryBuy);
            buyBtn.interactable = true;
        }
    }


    private void SetPreview(GameObject prefab)
    {
        // 先清掉舊的 preview（不管是骰子 prefab 還是遺物色塊）
        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
            _currentPreview = null;
        }

        if (previewAnchor == null)
            return;

        // 有 prefab（例如骰子 UI），照舊實例化
        if (prefab != null)
        {
            _currentPreview = Instantiate(prefab, previewAnchor);

            var diceUis = _currentPreview.GetComponentsInChildren<DiceGame.Core.DiceUI>(true);
            foreach (var u in diceUis) u.enabled = false;

            var oldTips = _currentPreview.GetComponentsInChildren<DiceGame.Core.DiceHoverTooltip>(true);
            foreach (var t in oldTips) t.enabled = false;

            _currentPreview.transform.localPosition = Vector3.zero;
            _currentPreview.transform.localRotation = Quaternion.identity;
            _currentPreview.transform.localScale = Vector3.one;
            return;
        }

        // 沒有 prefab，而且這張卡是「遺物」→ 用顏色方塊代表不同稀有度
        if (relic != null)
        {
            // 先嘗試直接使用 previewAnchor 本身的 Image（你的 ShopItemCard 可能已經放了一個 Image）
            var anchorImage = previewAnchor.GetComponent<UnityEngine.UI.Image>();
            if (anchorImage == null || anchorImage.sprite == null)
            {
                // 有可能 Image 是掛在 previewAnchor 的子物件上，所以再從子物件裡找一次
                anchorImage = previewAnchor.GetComponentInChildren<UnityEngine.UI.Image>(true);
                // 如果子物件上的 Image 也沒有 sprite，就等同於沒有可用的圖，稍後會走色塊邏輯
                if (anchorImage != null && anchorImage.sprite == null)
                {
                    anchorImage = null;
                }
            }
            Color c;

            switch (relic.rarity)
            {
                case RelicRarity.Common:
                    c = new Color32(143, 238, 143, 255); // 淺綠 Common
                    break;
                case RelicRarity.Rare:
                    c = new Color32(147, 112, 219, 255); // 紫色 Rare
                    break;
                case RelicRarity.Legendary:
                    c = new Color32(255, 215, 0, 255);   // 金色 Legendary
                    break;
                default:
                    c = Color.gray;
                    break;
            }

            if (anchorImage != null)
            {
                Debug.Log($"[Shop/RelicPreview] Using existing Image on {anchorImage.gameObject.name} for relic {relic.relicName}, rarity={relic.rarity}");
                // 直接把 previewAnchor 的 Image 染色，這樣就算沒有子物件也會有顏色
                anchorImage.enabled = true;
                anchorImage.color = c;
                // 不要把 _currentPreview 指向 previewAnchor，避免之後被 Destroy
            }
            else
            {
                // 如果 previewAnchor 沒有 Image，就退而求其次，建立一個色塊子物件
                Debug.Log($"[Shop/RelicPreview] No Image found under previewAnchor ({previewAnchor.name}), creating RelicColorBlock for relic {relic.relicName}, rarity={relic.rarity}");
                var go = new GameObject("RelicColorBlock", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(previewAnchor, false);

                // 固定色塊大小 80x80，並避免被上層 Layout Group 改變尺寸
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(80f, 80f);
                rt.anchoredPosition = Vector2.zero;

                // 告訴任何父物件上的 Layout Group：不要對這個色塊套用自動排版尺寸
                var layout = go.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.ignoreLayout = true;
                layout.preferredWidth = 80f;
                layout.preferredHeight = 80f;
                layout.minWidth = 80f;
                layout.minHeight = 80f;

                var img = go.GetComponent<UnityEngine.UI.Image>();
                img.color = c;
                _currentPreview = go;
            }
        }
    }

    private void TryBuy()
    {
        if (_sold) return;
        if (_onBuy == null) return;

        // 呼叫 ShopManager 決定購買結果
        BuyResult result = _onBuy.Invoke();

        switch (result)
        {
            case BuyResult.Success:
                // 購買成功：清除提示、標記為已售出
                if (feedbackText)
                {
                    feedbackText.text = string.Empty;
                }

                _sold = true;
                if (buyBtn)
                {
                    buyBtn.interactable = false;
                    // 改變按鈕文字成 SOLD
                    var txt = buyBtn.GetComponentInChildren<TMP_Text>();
                    if (txt)
                    {
                        txt.text = "SOLD";
                    }
                }
                if (soldOverlay)
                    soldOverlay.SetActive(true);
                break;

            case BuyResult.NotEnoughMoney:
                {
                    string itemName =
                        die != null ? die.diceName :
                        (relic != null ? relic.relicName : name);
                    Debug.Log("[ShopItemUI] Not enough money to buy " + itemName);
                    if (feedbackText)
                    {
                        feedbackText.text = "Not enough money!";
                    }
                    break;
                }

            case BuyResult.BackpackFull:
                {
                    string itemName =
                        die != null ? die.diceName :
                        (relic != null ? relic.relicName : name);
                    Debug.Log("[ShopItemUI] Backpack is full, cannot buy " + itemName);
                    if (feedbackText)
                    {
                        feedbackText.text = "Backpack is full!";
                    }
                    break;
                }
        }
    }

    public void ShowTooltipForThisCard()
    {
        // 這行是「被叫到」的證明，不要刪
        Debug.Log($"[ShopItemUI] ShowTooltipForThisCard CALLED on {name}");

        if (DiceTooltipManager.Instance == null)
        {
            Debug.LogWarning("[ShopItemUI] DiceTooltipManager.Instance is NULL，這個 Scene 有沒有放 TooltipRoot？");
            return;
        }

        // 若此卡片代表骰子（choiceSlots）
        if (die != null)
        {
            Debug.Log($"[ShopItemUI] Hover SUCCESS → Showing tooltip for dice: {die.diceName}");
            DiceTooltipManager.Instance.ShowTooltip(die);
            return;
        }

        // 若此卡片代表遺物（relicSlots）
        if (relic != null)
        {
            Debug.Log($"[ShopItemUI] Hover SUCCESS → Showing tooltip for relic: {relic.relicName}");
            DiceTooltipManager.Instance.ShowTooltip(relic);
            return;
        }

        // 既不是骰子也不是遺物，只做 debug 紀錄
        Debug.Log($"[ShopItemUI] No die/relic assigned on {name} for tooltip.");
    }

    public void HideTooltipForThisCard()
    {
        string itemName =
            die != null ? die.diceName :
            (relic != null ? relic.relicName : "NULL");
        Debug.Log($"[ShopItemUI] HideTooltipForThisCard on {name}, item = {itemName}");
        DiceTooltipManager.Instance?.HideTooltip();
    }
}