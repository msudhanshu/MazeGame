using System.Collections.Generic;
using Nixin.Grid.Core;

namespace Game.Core.Fue
{
    public static class TutorialSpec
    {
        public const string LessonId = "memory-path.tutorial";
        public const int LivesPerRun = 2;
        public const int RunsPerSession = 1;
        public const float RadarSweepSeconds = 5.5f;
        public const float RadarHoldSeconds = 1.5f;

        public static GridSize Size { get; } = new GridSize(4, 3);

        public static GridCoord Start { get; } = new GridCoord(0, 0);

        public static GridCoord Goal { get; } = new GridCoord(3, 2);

        public static IReadOnlyList<GridCoord> PathCells { get; } = new[]
        {
            new GridCoord(0, 0),
            new GridCoord(1, 0),
            new GridCoord(2, 0),
            new GridCoord(2, 1),
            new GridCoord(3, 1),
            new GridCoord(3, 2)
        };

        public static GridPath CreatePath() => new GridPath(Size, PathCells);
    }

    public enum TutorialBeat
    {
        None,
        Intro,
        Watching,
        PromptChoice,
        Advanced,
        SessionFailed,
        Completed
    }
}
