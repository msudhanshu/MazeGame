using Game.Core.State;

namespace Game.Core.Fue
{
    public static class LevelOneFueSpec
    {
        public const string LessonId = "memory-path.tile-level-1";
        public const int LevelNumber = 1;

        public static bool ShouldStart(GameModeId mode, int levelNumber, bool alreadySeen) =>
            !alreadySeen && mode == GameModeId.TileArena && levelNumber == LevelNumber;
    }

    public enum LevelOneFueBeat
    {
        PromptMove,
        HealthHint,
        Completed
    }
}
