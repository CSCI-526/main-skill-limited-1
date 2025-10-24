namespace DiceGame
{
    /// <summary>
    /// Simple static payload to carry final run results into EndScene.
    /// Avoids dependency issues across folders/namespaces for prototype speed.
    /// </summary>
    public static class EndSceneData
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
