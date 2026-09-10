using System;
using System.Collections.Generic;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// One catalog row: board side, turn floor, walk budget, and how much help the path may show.
    /// Length slack and the turn band are derived so the generator still has room to wander.
    /// </summary>
    public readonly struct LevelSpec
    {
        public LevelSpec(
            int side,
            int minTurns,
            int lives,
            int lighthouses = 0,
            int glimpse = 0,
            int beacon = 0,
            int lengthSlack = 4,
            int runs = 5,
            int width = 0,
            int height = 0,
            int blockedHints = 0)
        {
            Side = side;
            MinTurns = minTurns;
            Lives = lives;
            Lighthouses = lighthouses;
            Glimpse = glimpse;
            Beacon = beacon;
            LengthSlack = lengthSlack;
            Runs = runs;
            Width = width > 0 ? width : side;
            Height = height > 0 ? height : side;
            BlockedHints = blockedHints;
        }

        public int Side { get; }
        public int MinTurns { get; }
        public int Lives { get; }
        public int Lighthouses { get; }
        public int Glimpse { get; }
        public int Beacon { get; }
        public int LengthSlack { get; }
        public int Runs { get; }
        public int Width { get; }
        public int Height { get; }
        public int BlockedHints { get; }
    }

    /// <summary>
    /// The level ladder, expressed as data. Each row widens the board or asks for more unmarked
    /// walking; nothing here fixes an actual route.
    /// </summary>
    public sealed class LevelCatalog
    {
        // Early boards are short, so a 5×3 luck budget almost never fails. Keep the memory
        // restart, but shrink hearts and walks until the path is long enough that luck is
        // no longer enough on its own.
        static readonly LevelSpec[] Specs =
        {
            new LevelSpec(3, 1, 2, lengthSlack: 2, runs: 2, width: 3, height: 3),
            new LevelSpec(3, 2, 2, lengthSlack: 2, runs: 2, width: 3, height: 4),
            new LevelSpec(4, 2, 2, lengthSlack: 3, runs: 3, width: 3, height: 5),
            new LevelSpec(4, 3, 2, lengthSlack: 3, runs: 3, width: 4, height: 4),
            new LevelSpec(4, 3, 2, lengthSlack: 3, runs: 3, width: 4, height: 5),
            new LevelSpec(4, 4, 2, lengthSlack: 3, runs: 3, width: 4, height: 5),
            new LevelSpec(5, 4, 3, blockedHints: 1, lengthSlack: 4, runs: 4, width: 5, height: 5),
            new LevelSpec(5, 8, 3, lighthouses: 1, blockedHints: 1, lengthSlack: 4, runs: 4, width: 5, height: 6),
            new LevelSpec(5, 7, 3, blockedHints: 2, lengthSlack: 4, runs: 4, width: 5, height: 6),
            new LevelSpec(5, 8, 3, blockedHints: 2, lengthSlack: 4, runs: 4, width: 5, height: 6),
            new LevelSpec(5, 8, 3, blockedHints: 2, lengthSlack: 4, runs: 4, width: 5, height: 6),
            new LevelSpec(5, 9, 3, blockedHints: 3, lengthSlack: 4, runs: 4, width: 5, height: 6),
            new LevelSpec(6, 8, 3, blockedHints: 3, lengthSlack: 5, width: 6, height: 7),
            new LevelSpec(6, 9, 3, blockedHints: 3, lengthSlack: 5, width: 6, height: 7),
            new LevelSpec(6, 12, 3, glimpse: 1, blockedHints: 4, lengthSlack: 5, width: 6, height: 8),
            new LevelSpec(6, 11, 3, blockedHints: 4, lengthSlack: 5, width: 6, height: 8),
            new LevelSpec(6, 12, 3, blockedHints: 4, lengthSlack: 5, width: 6, height: 8),
            new LevelSpec(6, 16, 3, lighthouses: 1, blockedHints: 5, lengthSlack: 5, width: 6, height: 9),
            new LevelSpec(6, 13, 3, blockedHints: 5, lengthSlack: 5, width: 6, height: 9),
            new LevelSpec(6, 17, 3, lighthouses: 1, blockedHints: 5, lengthSlack: 6, width: 6, height: 10),
            new LevelSpec(7, 13, 3, blockedHints: 5, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 18, 3, lighthouses: 1, blockedHints: 6, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 18, 3, lighthouses: 1, blockedHints: 6, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 18, 3, lighthouses: 1, blockedHints: 6, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 18, 3, lighthouses: 1, blockedHints: 6, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 19, 3, lighthouses: 1, beacon: 1, blockedHints: 5, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(7, 19, 3, lighthouses: 1, blockedHints: 7, lengthSlack: 6, width: 7, height: 9),
            new LevelSpec(8, 17, 3, lighthouses: 1, blockedHints: 7, lengthSlack: 7, width: 8, height: 10),
            new LevelSpec(8, 17, 3, lighthouses: 1, blockedHints: 7, lengthSlack: 7, width: 8, height: 10),
            new LevelSpec(8, 19, 3, lighthouses: 2, blockedHints: 5, lengthSlack: 7, width: 8, height: 10),
            new LevelSpec(8, 21, 3, lighthouses: 2, blockedHints: 8, lengthSlack: 7, width: 8, height: 11),
            new LevelSpec(8, 21, 3, lighthouses: 2, blockedHints: 8, lengthSlack: 7, width: 8, height: 11),
            new LevelSpec(8, 23, 3, lighthouses: 2, glimpse: 1, blockedHints: 8, lengthSlack: 8, width: 8, height: 12),
            new LevelSpec(9, 16, 3, lighthouses: 1, blockedHints: 8, lengthSlack: 7, width: 9, height: 11),
            new LevelSpec(9, 20, 3, lighthouses: 2, blockedHints: 9, lengthSlack: 7, width: 9, height: 12),
            new LevelSpec(9, 20, 3, lighthouses: 2, blockedHints: 9, lengthSlack: 7, width: 9, height: 12),
            new LevelSpec(9, 22, 3, lighthouses: 2, beacon: 1, blockedHints: 9, lengthSlack: 7, width: 9, height: 12),
            new LevelSpec(9, 20, 3, lighthouses: 2, blockedHints: 9, lengthSlack: 7, width: 9, height: 12),
            new LevelSpec(10, 19, 3, lighthouses: 2, blockedHints: 9, lengthSlack: 8, width: 10, height: 12),
            new LevelSpec(10, 20, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 12),
            new LevelSpec(10, 23, 3, lighthouses: 2, glimpse: 1, blockedHints: 10, lengthSlack: 8, width: 10, height: 12),
            new LevelSpec(10, 21, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 13),
            new LevelSpec(10, 23, 3, lighthouses: 2, beacon: 1, blockedHints: 10, lengthSlack: 8, width: 10, height: 13),
            new LevelSpec(10, 22, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 13),
            new LevelSpec(10, 25, 3, lighthouses: 2, glimpse: 1, blockedHints: 10, lengthSlack: 8, width: 10, height: 14),
            new LevelSpec(10, 24, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 14),
            new LevelSpec(10, 25, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 14),
            new LevelSpec(10, 25, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 15),
            new LevelSpec(10, 25, 3, lighthouses: 2, blockedHints: 10, lengthSlack: 8, width: 10, height: 15),
            new LevelSpec(10, 27, 3, lighthouses: 2, beacon: 1, blockedHints: 10, lengthSlack: 8, width: 10, height: 15)
        };

        const int TurnBandWidth = 4;

        public const string DefaultThemeId = "dance_floor";

        public static IReadOnlyList<LevelSpec> NixinDefaultSpecs => Specs;

        public LevelCatalog() : this(FromSpecs(Specs))
        {
        }

        public static IReadOnlyList<LevelDefinition> FromSpecs(IReadOnlyList<LevelSpec> specs)
        {
            if (specs == null || specs.Count == 0)
                throw new ArgumentException("A catalog needs at least one level spec.", nameof(specs));

            var levels = new List<LevelDefinition>(specs.Count);
            for (var i = 0; i < specs.Count; i++)
                levels.Add(ToDefinition(i + 1, specs[i]));

            return levels;
        }

        public LevelCatalog(IReadOnlyList<LevelDefinition> levels)
        {
            if (levels == null || levels.Count == 0)
                throw new ArgumentException("A catalog needs at least one level.", nameof(levels));

            for (var i = 0; i < levels.Count; i++)
            {
                if (levels[i].Number != i + 1)
                    throw new ArgumentException($"Level at index {i} is numbered {levels[i].Number}; levels must be numbered 1..N in order.", nameof(levels));
            }

            Levels = levels;
        }

        public IReadOnlyList<LevelDefinition> Levels { get; }

        public int Count => Levels.Count;

        public bool Contains(int levelNumber) => levelNumber >= 1 && levelNumber <= Levels.Count;

        public LevelDefinition Get(int levelNumber)
        {
            if (!Contains(levelNumber))
                throw new ArgumentOutOfRangeException(nameof(levelNumber), $"Level {levelNumber} is not in a catalog of {Levels.Count} levels.");

            return Levels[levelNumber - 1];
        }

        static LevelDefinition ToDefinition(int number, LevelSpec spec)
        {
            var size = new GridSize(spec.Width, spec.Height);
            var start = new GridCoord(0, 0);
            var goal = new GridCoord(spec.Width - 1, spec.Height - 1);

            var shortestLength = start.ManhattanDistanceTo(goal) + 1;
            var maxLength = Math.Min(size.CellCount, shortestLength + spec.LengthSlack);
            var turnCeiling = Math.Max(0, maxLength - 2);
            var minTurns = Math.Min(spec.MinTurns, turnCeiling);
            var maxTurns = Math.Min(minTurns + TurnBandWidth, turnCeiling);

            return new LevelDefinition(
                number: number,
                size: size,
                start: start,
                goal: goal,
                shape: new PathShapeSpec(shortestLength, maxLength, minTurns, maxTurns),
                themeId: DefaultThemeId,
                livesPerRun: spec.Lives,
                runsPerSession: spec.Runs,
                lighthouseCount: spec.Lighthouses,
                glimpseCount: spec.Glimpse,
                beaconCount: spec.Beacon,
                blockedHintCount: spec.BlockedHints);
        }
    }
}
