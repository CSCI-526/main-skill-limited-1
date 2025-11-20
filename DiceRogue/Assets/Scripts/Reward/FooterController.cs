using UnityEngine;
using UnityEngine.SceneManagement;
using DiceGame;

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

        // Add to pending dice list
        GameStateManager.Instance.State.PendingDiceTypeIds.Add(typeId);

        // Go to battle scene
        SceneManager.LoadScene("BattleScene");
    }
}