using System;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// One level's difficulty envelope. It pins the board, complexity band, walk budget, and
    /// how much help the path may show — not the route itself.
    /// </summary>
    public sealed class LevelDefinition
    {
        public LevelDefinition(
            int number,
            GridSize size,
            GridCoord start,
            GridCoord goal,
            PathShapeSpec shape,
            string themeId,
            int livesPerRun = 3,
            int runsPerSession = 5,
            int lighthouseCount = 0,
            int glimpseCount = 0,
            int beaconCount = 0,
            int blockedHintCount = 0)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number), "Levels are numbered from 1.");
            if (shape == null)
                throw new ArgumentNullException(nameof(shape));
            if (!size.Contains(start))
                throw new ArgumentException($"Start {start} is outside the {size} board.", nameof(start));
            if (!size.Contains(goal))
                throw new ArgumentException($"Goal {goal} is outside the {size} board.", nameof(goal));
            if (start == goal)
                throw new ArgumentException("Start and goal must differ.", nameof(goal));
            if (string.IsNullOrEmpty(themeId))
                throw new ArgumentException("A level needs a theme id.", nameof(themeId));
            if (livesPerRun < 1)
                throw new ArgumentOutOfRangeException(nameof(livesPerRun));
            if (runsPerSession < 1)
                throw new ArgumentOutOfRangeException(nameof(runsPerSession));
            if (lighthouseCount < 0 || lighthouseCount > 2)
                throw new ArgumentOutOfRangeException(nameof(lighthouseCount), "At most two starting lighthouses.");
            if (glimpseCount < 0 || glimpseCount > 1)
                throw new ArgumentOutOfRangeException(nameof(glimpseCount));
            if (beaconCount < 0 || beaconCount > 1)
                throw new ArgumentOutOfRangeException(nameof(beaconCount));
            if (blockedHintCount < 0)
                throw new ArgumentOutOfRangeException(nameof(blockedHintCount));

            Number = number;
            Size = size;
            Start = start;
            Goal = goal;
            Shape = shape;
            ThemeId = themeId;
            LivesPerRun = livesPerRun;
            RunsPerSession = runsPerSession;
            LighthouseCount = lighthouseCount;
            GlimpseCount = glimpseCount;
            BeaconCount = beaconCount;
            BlockedHintCount = blockedHintCount;
        }

        public int Number { get; }
        public GridSize Size { get; }
        public GridCoord Start { get; }
        public GridCoord Goal { get; }
        public PathShapeSpec Shape { get; }
        public string ThemeId { get; }
        public int LivesPerRun { get; }
        public int RunsPerSession { get; }
        public int LighthouseCount { get; }
        public int GlimpseCount { get; }
        public int BeaconCount { get; }

        /// <summary>Off-path tiles shown grey from the start so they are never choices.</summary>
        public int BlockedHintCount { get; }

        /// <summary>
        /// How much hidden walking this level still asks for after lighthouses and pickups.
        /// Used to keep the ladder from getting easier when help is added.
        /// </summary>
        public int UnmarkedDifficulty =>
            Shape.MinLength + Shape.MinTurns
            - LighthouseCount * 4
            - GlimpseCount * 3
            - BeaconCount * 2
            - BlockedHintCount;
    }
}
