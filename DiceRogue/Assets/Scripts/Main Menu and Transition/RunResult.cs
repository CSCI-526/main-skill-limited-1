namespace DiceGame
{
    public static class RunResult
    {
        public static int LastScore { get; private set; }
        public static int TargetScore { get; private set; }
        public static bool DidWin { get; private set; }

        public static void Set(int lastScore, int targetScore, bool didWin)
        {
            LastScore = lastScore;
            TargetScore = targetScore;
            DidWin = didWin;
        }
    }
}
