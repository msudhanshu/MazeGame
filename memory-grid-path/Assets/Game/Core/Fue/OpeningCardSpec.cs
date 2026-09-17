namespace Game.Core.Fue
{
    /// <summary>
    /// Big first-run reminder card. Auto-shows on early levels; Settings can replay it.
    /// </summary>
    public static class OpeningCardSpec
    {
        public const string LessonId = "memory-path.opening-card";
        public const int MaxLevel = 2;

        public static bool ShouldShow(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= MaxLevel;

        public static string LessonIdFor(int levelNumber) =>
            LessonId + "." + levelNumber;
    }
}
