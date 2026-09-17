using Game.Core.State;

namespace Game.Core.Fue
{
    public static class GraphLevelOneFueSpec
    {
        public const string LessonId = "memory-path.graph-level-1.tap.v1";
        public const int LevelNumber = 1;

        public static bool ShouldStart(GameModeId mode, int levelNumber, bool alreadySeen) =>
            !alreadySeen && mode == GameModeId.GraphArena && levelNumber == LevelNumber;
    }

    public enum GraphLevelOneFueBeat
    {
        PromptTap,
        HealthHint,
        Completed
    }
}
