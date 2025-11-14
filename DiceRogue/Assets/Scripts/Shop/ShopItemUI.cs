using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame;           // BaseDice
using DiceGame.Core;      // DiceHoverTooltip
using UnityEngine.EventSystems;

public class ShopItemUI : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text priceText;
    public Button buyBtn;
    public Transform previewAnchor;

    public BaseDice die;

    [Header("Sold Overlay")]
    public GameObject soldOverlay;

    // 資料
    private System.Func<bool> _onBuy;  // 回呼：點「購買」時由 ShopManager 注入邏輯
    private bool _sold;
    private GameObject _currentPreview;

    public void Bind(GameObject previewPrefab, string name, int price, System.Func<bool> onBuy)
    {
        SetPreview(previewPrefab);
        if (priceText)
        {
            if (price <= 0)
                priceText.text = "FREE";
            else
                priceText.text = $"$ {price}";
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
        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
        }
        if (prefab != null && previewAnchor != null)
        {
            _currentPreview = Instantiate(prefab, previewAnchor);

            // _currentPreview = Instantiate(...); 之後加入
            var diceUis = _currentPreview.GetComponentsInChildren<DiceGame.Core.DiceUI>(true);
            foreach (var u in diceUis) u.enabled = false;

            var oldTips = _currentPreview.GetComponentsInChildren<DiceGame.Core.DiceHoverTooltip>(true);
            foreach (var t in oldTips) t.enabled = false;

            _currentPreview.transform.localPosition = Vector3.zero;
            _currentPreview.transform.localRotation = Quaternion.identity;
            _currentPreview.transform.localScale = Vector3.one;
        }
    }

    private void TryBuy()
    {
        if (_sold) return;
        if (_onBuy != null && _onBuy.Invoke())
        {
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
        }
    }

    public void ShowTooltipForThisCard()
    {
        // 這行是「被叫到」的證明，不要刪
        Debug.Log($"[ShopItemUI] ShowTooltipForThisCard CALLED on {name}");

        if (die == null)
        {
            Debug.LogWarning($"[ShopItemUI] die is NULL on {name}，請在 ShopManager 設定 choiceSlots[i].die = die;");
            return;
        }

        if (DiceTooltipManager.Instance == null)
        {
            Debug.LogWarning("[ShopItemUI] DiceTooltipManager.Instance is NULL，這個 Scene 有沒有放 TooltipRoot？");
            return;
        }

        // 真正成功顯示時的 log
        Debug.Log($"[ShopItemUI] Hover SUCCESS → Showing tooltip for dice: {die.diceName}");
        DiceTooltipManager.Instance.ShowTooltip(die);
    }

    public void HideTooltipForThisCard()
    {
        Debug.Log($"[ShopItemUI] HideTooltipForThisCard on {name}, die = {(die != null ? die.diceName : "NULL")}");
        DiceTooltipManager.Instance?.HideTooltip();
    }
}