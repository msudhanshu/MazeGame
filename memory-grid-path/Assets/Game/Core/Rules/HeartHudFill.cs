namespace Game.Core.Rules
{
    /// <summary>
    /// How much of a walk-heart is spent. Fill grows from the bottom of the heart.
    /// </summary>
    public static class HeartHudFill
    {
        public static float SpentAmount(int livesLeft, int livesPerRun)
        {
            if (livesPerRun < 1 || livesLeft <= 0)
                return 1f;
            if (livesLeft >= livesPerRun)
                return 0f;
            return (livesPerRun - livesLeft) / (float)livesPerRun;
        }

        public static float RemainingGlow(int livesLeft, int livesPerRun)
        {
            if (livesPerRun < 1 || livesLeft <= 0)
                return 0f;
            if (livesLeft >= livesPerRun)
                return 1f;
            return livesLeft / (float)livesPerRun;
        }
    }
}
