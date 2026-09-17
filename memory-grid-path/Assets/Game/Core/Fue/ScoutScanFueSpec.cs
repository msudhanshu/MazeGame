using Game.Core.State;

namespace Game.Core.Fue
{
    public static class ScoutScanFueSpec
    {
        public const string LessonId = "memory-path.scout-scan-hold.v2";
        public const int LevelNumber = 1;

        public static bool ShouldStart(GameModeId mode, int levelNumber, bool alreadySeen) =>
            !alreadySeen && mode == GameModeId.ScoutArena && levelNumber == LevelNumber;
    }

    public enum ScoutScanFueBeat
    {
        PromptHold,
        PromptRelease,
        Completed
    }
}
