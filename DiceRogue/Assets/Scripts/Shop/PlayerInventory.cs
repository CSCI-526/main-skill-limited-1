using System.Collections.Generic;
using UnityEngine;
using DiceGame;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory I;
    public List<BaseDice> ownedDice = new();

    private void Awake()
    {
        if (I == null) I = this; else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
    }

    public void AddDie(BaseDice die) => ownedDice.Add(die);
}