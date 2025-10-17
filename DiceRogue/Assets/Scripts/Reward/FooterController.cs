using UnityEngine;
using UnityEngine.SceneManagement;

public class FooterController : MonoBehaviour
{
    public void GoToNextBattle()
    {
        if (RewardSceneManager._selectedDice == null)
        {
            Debug.LogWarning("[Footer] No dice selected.");
            return;
        }

        var dice = RewardSceneManager._selectedDice;
        var typeId = dice.GetType().Name;
        Debug.Log($"[Footer] Player selected dice: {dice.diceName} ({dice.tier}) TypeId={typeId}");

        // 你可以選擇：
        // 1) 存進 PendingDiceTypeIds
        RewardSceneManager.PendingDiceTypeIds.Add(typeId);

        // 2) 直接進入戰鬥場景
        SceneManager.LoadScene("BattleScene");
    }
}