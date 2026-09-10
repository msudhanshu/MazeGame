using System;
using System.Collections.Generic;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Greys out off-path tiles so they never appear as choices. Prefers cells that would
    /// otherwise distract the player beside the hidden route.
    /// </summary>
    public static class OffPathHintPlacer
    {
        public static IReadOnlyList<GridCoord> Place(GridPath path, LevelDefinition level, IRandomSource random)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var requested = level.BlockedHintCount;
            if (requested <= 0)
                return Array.Empty<GridCoord>();

            var pathSet = new HashSet<GridCoord>(path.Cells);
            var candidates = new List<(GridCoord cell, int score)>();
            for (var y = 0; y < path.Size.Height; y++)
            {
                for (var x = 0; x < path.Size.Width; x++)
                {
                    var cell = new GridCoord(x, y);
                    if (pathSet.Contains(cell))
                        continue;

                    candidates.Add((cell, Score(cell, pathSet, path.Size)));
                }
            }

            if (candidates.Count == 0)
                return Array.Empty<GridCoord>();

            Shuffle(candidates, random);
            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            var placed = new HashSet<GridCoord>();
            var result = new List<GridCoord>();
            for (var i = 0; i < candidates.Count && result.Count < requested; i++)
            {
                var cell = candidates[i].cell;
                placed.Add(cell);
                if (!IsPlayable(path, placed))
                {
                    placed.Remove(cell);
                    continue;
                }

                result.Add(cell);
            }

            return result;
        }

        static int Score(GridCoord cell, HashSet<GridCoord> pathSet, GridSize size)
        {
            var score = 0;
            var neighbours = GridNeighbors.Of(size, cell, includeDiagonal: false);
            for (var i = 0; i < neighbours.Count; i++)
            {
                if (pathSet.Contains(neighbours[i]))
                    score += 3;
            }

            if (cell.X == 0 || cell.Y == 0 || cell.X == size.Width - 1 || cell.Y == size.Height - 1)
                score += 1;

            return score;
        }

        static bool IsPlayable(GridPath path, HashSet<GridCoord> blocked)
        {
            try
            {
                var prompts = GridPrompts.FromPath(path, allowStepBack: true, blocked);
                for (var i = 0; i < prompts.Count; i++)
                {
                    if (prompts[i].Choices.Count < 2)
                        return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        static void Shuffle(List<(GridCoord cell, int score)> values, IRandomSource random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(i + 1);
                var swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }
        }
    }
}
