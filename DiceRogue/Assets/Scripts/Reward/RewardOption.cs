public class RewardOption
{
    public DiceType type;
    public string displayName; // e.g., "Common Die"
    public string effectText;  // e.g., "+Stable rolls" / "Higher variance"
    public int cost;           // 可顯示用(選填)

    public RewardOption(DiceType t, string name, string effect, int c = 1)
    {
        type = t; displayName = name; effectText = effect; cost = c;
    }
}