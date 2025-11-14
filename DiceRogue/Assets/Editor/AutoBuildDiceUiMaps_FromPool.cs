#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DiceGame;                  // BaseDice
using UnityEditor.Experimental.SceneManagement;

public static class AutoBuildDiceUiMaps_FromPool
{
    // 你可以改這個搜尋範圍；支援多個資料夾
    private static readonly string[] SearchFolders = new[]
    {
        "Assets/Prefabs",
        "Assets/**/DiceUI",     // 可選：子資料夾萬用
    };

    [MenuItem("Tools/Shop/Auto-Build Dice UI Map (from DicePool + Prefab Variants)")]
    public static void BuildFromDicePool()
    {
        var mgr = UnityEngine.Object.FindObjectOfType<ShopManager>();
        if (mgr == null)
        {
            Debug.LogError("[Shop] 找不到 ShopManager，請先在場景放一個。");
            return;
        }

        // 1) 來源：DicePool 已定義所有 dice 類別
        var allDice = DicePool.GetAll();
        if (allDice == null || allDice.Count == 0)
        {
            Debug.LogWarning("[Shop] DicePool.GetAll() 沒有回傳任何骰子。");
            return;
        }

        // 2) 掃描 Prefab / Prefab Variant
        var guids = AssetDatabase.FindAssets("t:Prefab", SearchFolders);
        var allPrefabs = guids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
            .Where(p => p != null)
            .ToList();

        // 3) 可選：外框（Common/Rare/Legendary）——若專案有就會自動填
        GameObject frameCommon = FindByExactName(allPrefabs, "Common");
        GameObject frameRare = FindByExactName(allPrefabs, "Rare");
        GameObject frameLegendary = FindByExactName(allPrefabs, "Legendary");

        // 4) 依「dice 類名」→ 嘗試各種常見命名去匹配 prefab / variant
        var newMaps = new List<ShopManager.DiceUiMap>();
        foreach (var die in allDice)
        {
            var key = die.GetType().Name;     // 例：CounterDice、PlusOne、D8、...
            var prefab = ResolveVariantPrefab(allPrefabs, key);

            var map = new ShopManager.DiceUiMap
            {
                diceNameKey = key,
                uiPrefab = prefab,                     // 可能是 Variant 或普通 Prefab，皆可
                framePrefabCommon = frameCommon,
                framePrefabRare = frameRare,
                framePrefabLegendary = frameLegendary
            };
            newMaps.Add(map);
        }

        // 5) 寫回 ShopManager
        Undo.RecordObject(mgr, "Auto Build Dice UI Maps (from DicePool)");
        mgr.diceUiMaps = newMaps;
        EditorUtility.SetDirty(mgr);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Shop] diceUiMaps 已自動建立，共 {newMaps.Count} 筆（來源：DicePool + Prefab/Variant 掃描）。");
    }

    // ---- Helpers ----

    private static GameObject FindByExactName(List<GameObject> prefabs, string name)
        => prefabs.FirstOrDefault(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));

    private static string[] NameCandidates(string key)
    {
        // 針對 Prefab Variant 常見命名做多種嘗試
        // 例如：CounterDice、CounterDiceUI、CounterDice_Variant、CounterDice Variant、CounterDice(1)
        return new[]
        {
            key,
            $"{key}UI",
            $"{key}_UI",
            $"{key}Variant",
            $"{key}_Variant",
            $"{key} Variant",
            $"{key}(Variant)",
            $"{key}(1)",
            $"{key}(2)"
        };
    }

    private static bool NameLike(string prefabName, string key)
    {
        // 寬鬆匹配：去掉空白與底線，忽略大小寫，允許包含關鍵字
        string norm(string s) => new string(s.Where(ch => !char.IsWhiteSpace(ch) && ch != '_' && ch != '-').ToArray());
        var a = norm(prefabName).ToLowerInvariant();
        var b = norm(key).ToLowerInvariant();
        return a == b || a.Contains(b);
    }

    private static GameObject ResolveVariantPrefab(List<GameObject> prefabs, string key)
    {
        // 1) 先試精準候選名
        foreach (var cand in NameCandidates(key))
        {
            var hit = prefabs.FirstOrDefault(p => string.Equals(p.name, cand, StringComparison.OrdinalIgnoreCase));
            if (hit) return hit;
        }

        // 2) 寬鬆包含（處理帶空格/底線/後綴的變體）
        var loose = prefabs.FirstOrDefault(p => NameLike(p.name, key));
        if (loose) return loose;

        // 3) 若是 Variant，可嘗試回源檢查（通常沒必要；這裡保留作範例）
        // var variant = prefabs.FirstOrDefault(p => PrefabUtility.GetPrefabAssetType(p) == PrefabAssetType.Variant);
        // if (variant)
        // {
        //     var source = PrefabUtility.GetCorrespondingObjectFromSource(variant);
        //     if (source && string.Equals(source.name, key, StringComparison.OrdinalIgnoreCase))
        //         return variant;
        // }

        return null; // 找不到就交給 ShopItemCard 只顯示名稱與價格
    }
}
#endif