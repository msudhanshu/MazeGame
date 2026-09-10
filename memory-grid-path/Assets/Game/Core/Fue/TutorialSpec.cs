using Nixin.Grid.Core;

namespace Game.Core.Fue
{
    public static class TutorialSpec
    {
        public const string LessonId = "memory-path.tutorial";
        public const int LivesPerRun = 2;
        public const int RunsPerSession = 2;
        public const int ExtraMin = 3;
        public const int ExtraMax = 5;

        public static GridSize Size { get; } = new GridSize(4, 3);

        public static GridCoord Start { get; } = new GridCoord(0, 0);

        public static GridCoord Goal { get; } = new GridCoord(3, 2);
    }

    public enum TutorialBeat
    {
        None,
        Intro,
        PromptChoice,
        Lucky,
        UnluckyPartial,
        UnluckyRunOver,
        Remembered,
        RepeatedMistake,
        Advanced,
        SessionFailed,
        Completed
    }
}
