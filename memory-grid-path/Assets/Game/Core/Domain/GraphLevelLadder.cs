using System;
using System.Collections.Generic;

namespace Game.Core.Domain
{
    /// <summary>
    /// One Graph Arena row: route size (node count, including start and goal), turn band,
    /// walk budget, and how the path is shown. Topology stays on the authored arena.
    /// </summary>
    public readonly struct GraphLevelSpec
    {
        public GraphLevelSpec(
            int minPath,
            int maxPath,
            int minTurns,
            int maxTurns,
            int lives,
            int runs,
            PathPreviewKind previewKind,
            float previewSeconds,
            float holdSeconds)
        {
            MinPath = minPath;
            MaxPath = maxPath;
            MinTurns = minTurns;
            MaxTurns = maxTurns;
            Lives = lives;
            Runs = runs;
            PreviewKind = previewKind;
            PreviewSeconds = previewSeconds;
            HoldSeconds = holdSeconds;
        }

        public int MinPath { get; }
        public int MaxPath { get; }
        public int MinTurns { get; }
        public int MaxTurns { get; }
        public int Lives { get; }
        public int Runs { get; }
        public PathPreviewKind PreviewKind { get; }
        public float PreviewSeconds { get; }
        public float HoldSeconds { get; }

        public int IntermediateNodes => Math.Max(0, MinPath - 2);

        public bool IsFlash => PreviewKind == PathPreviewKind.CameraFlash;
    }

    /// <summary>
    /// Graph Arena teaching ladder for GraphLevel-1 0 (n41→n4). Shortest real route is
    /// 8 nodes / 4 turns. Hub play clones that arena and only changes path / scan / lives.
    /// </summary>
    public static class GraphLevelLadder
    {
        public const int Count = 8;
        public const int LastRadarLevel = 7;

        static readonly GraphLevelSpec[] Specs =
        {
            // L1 FUE: shortest band on the real map, very slow radar + finger.
            new GraphLevelSpec(8, 9, 0, 4, 2, 3, PathPreviewKind.Radar, 3.4f, 1.6f),
            new GraphLevelSpec(9, 11, 0, 5, 2, 3, PathPreviewKind.Radar, 3.1f, 1.4f),
            new GraphLevelSpec(10, 12, 2, 6, 2, 3, PathPreviewKind.Radar, 2.8f, 1.2f),
            new GraphLevelSpec(11, 13, 3, 7, 2, 3, PathPreviewKind.Radar, 2.5f, 1.0f),
            new GraphLevelSpec(12, 15, 4, 8, 2, 3, PathPreviewKind.Radar, 2.2f, 0.9f),
            new GraphLevelSpec(14, 18, 5, 10, 2, 3, PathPreviewKind.Radar, 1.5f, 0.45f),
            new GraphLevelSpec(16, 22, 6, 12, 2, 3, PathPreviewKind.Radar, 1.2f, 0.3f),
            new GraphLevelSpec(18, 26, 8, 16, 2, 2, PathPreviewKind.CameraFlash, 0.55f, 0f)
        };

        public static IReadOnlyList<GraphLevelSpec> All => Specs;

        public static GraphLevelSpec For(int levelNumber)
        {
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            if (levelNumber > Specs.Length)
                return Specs[Specs.Length - 1];

            return Specs[levelNumber - 1];
        }

        public static bool UsesTapFue(int levelNumber) => levelNumber == 1;

        public static bool UsesRadar(int levelNumber) =>
            levelNumber >= 1 && levelNumber <= LastRadarLevel;
    }
}
