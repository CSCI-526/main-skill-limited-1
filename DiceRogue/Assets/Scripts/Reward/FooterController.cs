using UnityEngine;
using UnityEngine.SceneManagement;

public class FooterController : MonoBehaviour
{
    public void GoToNextBattle()
    {
        // TODO：改成你的戰鬥場景名稱
        SceneManager.LoadScene("BattleScene");
    }
}