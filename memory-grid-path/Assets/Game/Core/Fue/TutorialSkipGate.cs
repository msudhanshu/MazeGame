namespace Game.Core.Fue
{
    /// <summary>
    /// Auto-play stops after the player skips the lesson twice. Settings replay is separate.
    /// </summary>
    public static class TutorialSkipGate
    {
        public const int SkipsBeforeDismiss = 2;

        public static bool ShouldAutoPlay(bool hasSeen, int skipCount, bool replayRequested)
        {
            if (replayRequested)
                return true;
            if (hasSeen)
                return false;
            return skipCount < SkipsBeforeDismiss;
        }

        public static bool ShouldMarkSeen(int skipCount) =>
            skipCount >= SkipsBeforeDismiss;
    }
}
