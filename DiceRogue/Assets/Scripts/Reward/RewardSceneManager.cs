using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DiceGame;   // ← 確保有這行
using TMPro;
using UnityEngine.SceneManagement;

public class RewardSceneManager : MonoBehaviour
{
    private GameStateManager _stateManager;
    private DiceManager _diceManager;  // Dice manager for accessing global dice pool
    
    [Header("Reference")]
    public List<BaseDice> allDicePool;
    [SerializeField] private Transform rewardParent;      // UI container (e.g., RewardPanel under Canvas)
    [SerializeField] private GameObject rewardCardPrefab; // Prefab: RewardDiceCard.prefab

    [Header("Config")]
    public int rewardCount = 3;

    private List<BaseDice> rewardResult = new();
    public static BaseDice _selectedDice;

    // Call this from the Next/Confirm button
    public void OnClickNext()
    {
        if (_selectedDice == null)
        {
            // If nothing selected, auto-pick the first option (quality of life)
            if (rewardResult != null && rewardResult.Count > 0)
            {
                _selectedDice = rewardResult[0];
                Debug.LogWarning($"[Reward] No selection made. Auto-picking: {_selectedDice.diceName} ({_selectedDice.tier})");
            }
            else
            {
                Debug.LogWarning("[Reward] No dice to confirm. Aborting.");
                return;
            }
        }

        // Convert to a type identifier (e.g., "D8", "HeavyDice") to recreate later
        var typeId = _selectedDice.GetType().Name;
        _stateManager.State.PendingDiceTypeIds.Add(typeId);
        Debug.Log($"[RewardScene] ========================================");
        Debug.Log($"[RewardScene] Reward confirmed!");
        Debug.Log($"[RewardScene] Selected: {_selectedDice.diceName} ({_selectedDice.tier})");
        Debug.Log($"[RewardScene] TypeId: '{typeId}'");
        Debug.Log($"[RewardScene] Returning to Battle Scene for next level...");
        Debug.Log($"[RewardScene] ========================================");

        // Go back to battle scene (BattleController will handle level progression)
        SceneManager.LoadScene("BattleScene");
    }

    void Start()
    {
        _stateManager = GameStateManager.Instance;
        
        // Initialize DiceManager to access global dice pool
        _diceManager = new DiceManager();
        _diceManager.InitializeGlobalDicePool();
        
        // 1) Build a pool from DiceManager's global pool (or fallback to DicePool.GetNonFiller())
        if (_diceManager != null && _diceManager.GlobalDicePool != null && _diceManager.GlobalDicePool.Count > 0)
        {
            allDicePool = _diceManager.GlobalDicePool.ToList();
            Debug.Log($"[RewardSceneManager] Using DiceManager.GlobalDicePool: {allDicePool.Count} dice types");
        }
        else
        {
            // Fallback to DicePool.GetNonFiller() to avoid Filler dice
            allDicePool = DicePool.GetNonFiller();
            Debug.Log($"[RewardSceneManager] Fallback to DicePool.GetNonFiller(): {allDicePool.Count} dice types");
        }
        
        GenerateRewardOptions();   // fills rewardResult with 3 random non-filler dice

        // 2) Render UI cards
        RenderRewards();

        // 3) Log final picks
        var resultNames = string.Join(", ", rewardResult.Select(d => $"{d.diceName} ({d.tier})"));
        Debug.Log($"[Reward] Final selected dice: {resultNames}");
    }

    void GenerateRewardOptions()
    {
        if (allDicePool == null || allDicePool.Count == 0)
        {
            Debug.LogError("[Reward] allDicePool is empty. Did you forget to assign or build it?");
            return;
        }

        // Filter out filler and null
        var source = allDicePool
            .Where(d => d != null && d.tier != DiceTier.Filler)
            .ToList();

        Debug.Log($"[Reward] Total non-filler dice: {source.Count}");
        if (source.Count == 0)
        {
            Debug.LogError("[Reward] No available dice after filtering out Filler!");
            return;
        }

        // Pick unique 3 (or rewardCount) at random
        rewardResult.Clear();
        for (int i = 0; i < rewardCount && source.Count > 0; i++)
        {
            int index = Random.Range(0, source.Count);
            var selected = source[index];
            rewardResult.Add(selected);
            Debug.Log($"[Reward] Selected dice: {selected.diceName} ({selected.tier})");
            source.RemoveAt(index);
        }
    }

    private void RenderRewards()
    {
        if (rewardParent == null)
        {
            Debug.LogError("[Reward] rewardParent is NULL. Drag your RewardPanel (under Canvas) here in the inspector.");
            return;
        }
        if (rewardCardPrefab == null)
        {
            Debug.LogError("[Reward] rewardCardPrefab is NULL. Drag RewardDiceCard.prefab here in the inspector.");
            return;
        }
        // Clear old children
        for (int i = rewardParent.childCount - 1; i >= 0; i--)
        {
            Destroy(rewardParent.GetChild(i).gameObject);
        }
        if (rewardResult == null || rewardResult.Count == 0)
        {
            Debug.LogWarning("[Reward] No rewardResult to render.");
            return;
        }

        foreach (var dice in rewardResult)
        {
            var go = Instantiate(rewardCardPrefab, rewardParent);
            var ui = go.GetComponent<RewardDiceUI>();
            if (ui == null)
            {
                Debug.LogError("[Reward] RewardDiceCard prefab is missing RewardDiceUI component.");
                continue;
            }
            // Bind card and selection callback
            ui.Bind(dice, () => OnSelectDice(dice, ui));
        }
    }

    private void OnSelectDice(BaseDice dice, RewardDiceUI ui)
    {
        _selectedDice = dice;
        Debug.Log($"[Reward] Selected by user: {_selectedDice.diceName} ({_selectedDice.tier}) | TypeId={_selectedDice.GetType().Name}");

        // Optional: highlight only the picked one
        for (int i = 0; i < rewardParent.childCount; i++)
        {
            var child = rewardParent.GetChild(i);
            var card = child.GetComponent<RewardDiceUI>();
            if (card != null)
            {
                card.SetHighlight(card == ui);
            }
        }
    }
}