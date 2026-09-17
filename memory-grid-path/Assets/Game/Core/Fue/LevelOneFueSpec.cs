using Game.Core.State;

namespace Game.Core.Fue
{
    public static class LevelOneFueSpec
    {
        public const string LessonId = "memory-path.tile-early";
        public const int LevelNumber = 1;
        public const int MaxLevel = OpeningCardSpec.MaxLevel;

        public static string LessonIdFor(int levelNumber) => LessonId + "." + levelNumber;

        public static bool ShouldStart(GameModeId mode, int levelNumber, bool alreadySeen) =>
            !alreadySeen && mode == GameModeId.TileArena && OpeningCardSpec.ShouldShow(levelNumber);
    }

    public enum LevelOneFueBeat
    {
        PromptMove,
        HealthHint,
        Completed
    }
}
