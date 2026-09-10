using System;
using System.Collections.Generic;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Fue
{
    /// <summary>
    /// Extends a discovery prefix into a self-avoiding path that ends at the tutorial home.
    /// Extra length is seed-stable so a replay of the same walk is the same route.
    /// </summary>
    public static class TutorialPathCompleter
    {
        const int NodeBudget = 8000;

        public static GridPath Complete(
            GridSize size,
            IReadOnlyList<GridCoord> prefix,
            IRandomSource random,
            int extraMin = TutorialSpec.ExtraMin,
            int extraMax = TutorialSpec.ExtraMax)
        {
            if (prefix == null || prefix.Count < 2)
                throw new ArgumentException("Prefix needs at least a start and one step.", nameof(prefix));
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (extraMin < 1)
                throw new ArgumentOutOfRangeException(nameof(extraMin));
            if (extraMax < extraMin)
                throw new ArgumentOutOfRangeException(nameof(extraMax));

            var goal = TutorialSpec.Goal;
            var cells = new List<GridCoord>(prefix.Count + extraMax);
            var visited = new HashSet<GridCoord>();
            for (var i = 0; i < prefix.Count; i++)
            {
                cells.Add(prefix[i]);
                visited.Add(prefix[i]);
            }

            if (cells[cells.Count - 1] == goal)
                return new GridPath(size, cells.ToArray());
            if (visited.Contains(goal))
                throw new InvalidOperationException("The discovery prefix visited home before the walk ended.");

            var prefixCount = cells.Count;
            var manhattan = cells[cells.Count - 1].ManhattanDistanceTo(goal);
            var preferredMin = Math.Max(extraMin, manhattan);
            if ((preferredMin - manhattan) % 2 != 0)
                preferredMin++;
            var preferredMax = Math.Max(extraMax, preferredMin);
            var fallbackMax = size.Width * size.Height - prefixCount;

            var budget = NodeBudget;
            if (!Search(size, cells, visited, random, goal, prefixCount, preferredMin, preferredMax, ref budget))
            {
                budget = NodeBudget;
                if (!Search(size, cells, visited, random, goal, prefixCount, manhattan, fallbackMax, ref budget))
                    throw new InvalidOperationException("Could not complete a tutorial path from the discovery prefix.");
            }

            return new GridPath(size, cells.ToArray());
        }

        static bool Search(
            GridSize size,
            List<GridCoord> cells,
            HashSet<GridCoord> visited,
            IRandomSource random,
            GridCoord goal,
            int prefixCount,
            int extraMin,
            int extraMax,
            ref int budget)
        {
            if (budget-- <= 0)
                return false;

            var extra = cells.Count - prefixCount;
            var current = cells[cells.Count - 1];
            if (current == goal)
                return extra >= extraMin && extra <= extraMax;
            if (extra >= extraMax)
                return false;
            if (current.ManhattanDistanceTo(goal) > extraMax - extra)
                return false;

            var neighbours = Shuffle(GridNeighbors.Of(size, current), random);
            for (var i = 0; i < neighbours.Count; i++)
            {
                var next = neighbours[i];
                if (!visited.Add(next))
                    continue;

                cells.Add(next);
                if (Search(size, cells, visited, random, goal, prefixCount, extraMin, extraMax, ref budget))
                    return true;

                cells.RemoveAt(cells.Count - 1);
                visited.Remove(next);
            }

            return false;
        }

        static List<GridCoord> Shuffle(IReadOnlyList<GridCoord> source, IRandomSource random)
        {
            var list = new List<GridCoord>(source.Count);
            for (var i = 0; i < source.Count; i++)
                list.Add(source[i]);

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = random.NextInt(i + 1);
                var swap = list[i];
                list[i] = list[j];
                list[j] = swap;
            }

            return list;
        }
    }
}
