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
            int blockedHints = 0,
            int turnBand = -1)
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
            TurnBand = turnBand;
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

        /// <summary>0 pins an exact turn count. Negative uses the catalog default band.</summary>
        public int TurnBand { get; }
    }

    /// <summary>
    /// The level ladder, expressed as data. Each row widens the board or asks for more unmarked
    /// walking; nothing here fixes an actual route. Economy rules live in README.md.
    /// </summary>
    public sealed class LevelCatalog
    {
        // Exact early turn counts so a new player sees one corner, then two, then three.
        static readonly LevelSpec[] Specs =
        {
            new LevelSpec(4, 1, 2, lengthSlack: 0, runs: 3, width: 4, height: 4, turnBand: 0),
            new LevelSpec(4, 1, 2, lengthSlack: 0, runs: 3, width: 4, height: 5, turnBand: 0),
            new LevelSpec(4, 2, 2, lengthSlack: 2, runs: 3, width: 4, height: 5, turnBand: 0),
            new LevelSpec(5, 2, 2, lengthSlack: 2, runs: 3, width: 5, height: 5, turnBand: 0),
            new LevelSpec(5, 2, 2, lengthSlack: 3, runs: 3, width: 5, height: 5, turnBand: 0),
            new LevelSpec(5, 3, 2, lengthSlack: 3, runs: 3, width: 5, height: 6, turnBand: 0),
            new LevelSpec(5, 3, 2, lengthSlack: 3, runs: 3, width: 5, height: 6, turnBand: 0),
            new LevelSpec(5, 3, 2, lengthSlack: 4, runs: 3, width: 5, height: 7, turnBand: 0),
            new LevelSpec(6, 4, 2, lengthSlack: 4, runs: 3, width: 6, height: 6, turnBand: 0),
            new LevelSpec(6, 4, 2, lengthSlack: 4, runs: 3, width: 6, height: 6, turnBand: 0),
            new LevelSpec(6, 4, 2, lengthSlack: 5, runs: 3, width: 6, height: 7, turnBand: 0),
            new LevelSpec(6, 5, 2, lengthSlack: 5, runs: 3, width: 6, height: 7, turnBand: 1),
            new LevelSpec(6, 5, 2, lengthSlack: 6, runs: 3, width: 6, height: 8, turnBand: 1),
            new LevelSpec(6, 6, 2, lengthSlack: 6, runs: 3, width: 6, height: 8, turnBand: 1),
            new LevelSpec(7, 7, 2, lengthSlack: 6, runs: 3, width: 7, height: 7, turnBand: 1),
            new LevelSpec(7, 9, 2, lengthSlack: 7, runs: 3, width: 7, height: 7, turnBand: 2),
            new LevelSpec(7, 11, 2, lengthSlack: 7, runs: 3, width: 7, height: 8, turnBand: 2),
            new LevelSpec(7, 13, 2, lengthSlack: 8, runs: 3, width: 7, height: 8, turnBand: 2),
            new LevelSpec(7, 14, 2, lengthSlack: 8, runs: 3, width: 7, height: 9, turnBand: 2),
            new LevelSpec(7, 16, 2, lengthSlack: 8, runs: 3, width: 7, height: 9, turnBand: 2),
            new LevelSpec(8, 16, 2, lengthSlack: 8, runs: 3, width: 8, height: 8, turnBand: 2),
            new LevelSpec(8, 17, 2, lengthSlack: 8, runs: 3, width: 8, height: 8, turnBand: 2),
            new LevelSpec(8, 18, 2, lengthSlack: 9, runs: 3, width: 8, height: 8, turnBand: 2),
            new LevelSpec(8, 19, 2, lengthSlack: 9, runs: 3, width: 8, height: 9, turnBand: 2),
            new LevelSpec(8, 20, 2, lengthSlack: 9, runs: 3, width: 8, height: 9, turnBand: 2)
        };

        const int TurnBandWidth = 2;

        public const string DefaultThemeId = "dance_floor";

        public static IReadOnlyList<LevelSpec> NixinDefaultSpecs => Specs;

        /// <summary>
        /// Scout Arena starts on tiny boards and grows slowly. Width x height in tiles.
        /// Levels 1–4 stay the teaching boards; later rows keep the city pack and get tougher.
        /// </summary>
        static readonly LevelSpec[] ScoutLadder =
        {
            new LevelSpec(2, 1, 2, lengthSlack: 0, runs: 3, width: 2, height: 2),
            new LevelSpec(2, 1, 2, lengthSlack: 1, runs: 3, width: 2, height: 2),
            new LevelSpec(2, 1, 2, lengthSlack: 1, runs: 3, width: 2, height: 3),
            new LevelSpec(2, 2, 2, lengthSlack: 2, runs: 3, width: 2, height: 3),
            new LevelSpec(3, 2, 2, lengthSlack: 2, runs: 3, width: 3, height: 3, turnBand: 0),
            new LevelSpec(3, 3, 2, lengthSlack: 2, runs: 3, width: 3, height: 3, turnBand: 0),
            new LevelSpec(3, 3, 2, lengthSlack: 3, runs: 3, width: 3, height: 4, turnBand: 0),
            new LevelSpec(3, 4, 2, lengthSlack: 3, runs: 3, width: 3, height: 4, turnBand: 0),
            new LevelSpec(4, 4, 2, lengthSlack: 3, runs: 3, width: 4, height: 4, turnBand: 1),
            new LevelSpec(4, 5, 2, lengthSlack: 4, runs: 3, width: 4, height: 4, turnBand: 1),
            new LevelSpec(4, 5, 2, lengthSlack: 4, runs: 3, width: 4, height: 5, turnBand: 1),
            new LevelSpec(4, 6, 2, lengthSlack: 5, runs: 3, width: 4, height: 5, turnBand: 1)
        };

        public static IReadOnlyList<LevelSpec> ScoutSpecs => ScoutLadder;

        public static bool TryScoutSpec(int zeroBasedIndex, out LevelSpec spec)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= ScoutLadder.Length)
            {
                spec = default;
                return false;
            }

            spec = ScoutLadder[zeroBasedIndex];
            return true;
        }

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
            var band = spec.TurnBand >= 0 ? spec.TurnBand : TurnBandWidth;
            var maxTurns = Math.Min(minTurns + band, turnCeiling);

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
