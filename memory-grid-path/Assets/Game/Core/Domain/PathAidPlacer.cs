using System;
using System.Collections.Generic;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Drops lighthouses and pickups onto a generated path. Whites split the snake into memory
    /// chunks; pickups sit on remaining interior cells so stepping on them is always a correct move.
    /// </summary>
    public static class PathAidPlacer
    {
        public static PathAids Place(GridPath path, LevelDefinition level, IRandomSource random)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var lighthouseIndices = PickLighthouseIndices(path.Cells.Count, level.LighthouseCount);
            var taken = new HashSet<int>(lighthouseIndices);
            var pickups = PlacePickups(path, taken, level.GlimpseCount, level.BeaconCount, random);

            var lighthouses = new GridCoord[lighthouseIndices.Count];
            for (var i = 0; i < lighthouseIndices.Count; i++)
                lighthouses[i] = path.Cells[lighthouseIndices[i]];

            return new PathAids(lighthouses, pickups);
        }

        static List<int> PickLighthouseIndices(int cellCount, int requested)
        {
            var placed = new List<int>();
            var maxPlaceable = MaxLighthouses(cellCount);
            var count = Math.Min(Math.Min(requested, maxPlaceable), 2);
            if (count <= 0)
                return placed;

            var last = cellCount - 1;
            for (var i = 1; i <= count; i++)
            {
                var index = (i * last) / (count + 1);
                index = ClampInterior(index, last);
                if (!TryTake(placed, index, last))
                    break;
            }

            placed.Sort();
            return placed;
        }

        static bool TryTake(List<int> placed, int index, int last)
        {
            if (IsFreeInterior(placed, index, last))
            {
                placed.Add(index);
                return true;
            }

            for (var bump = index + 1; bump < last; bump++)
            {
                if (!IsFreeInterior(placed, bump, last))
                    continue;
                placed.Add(bump);
                return true;
            }

            for (var bump = index - 1; bump >= 1; bump--)
            {
                if (!IsFreeInterior(placed, bump, last))
                    continue;
                placed.Add(bump);
                return true;
            }

            return false;
        }

        static bool IsFreeInterior(List<int> placed, int index, int last)
        {
            if (index < 1 || index >= last)
                return false;

            for (var i = 0; i < placed.Count; i++)
            {
                if (placed[i] == index)
                    return false;
                if (Math.Abs(placed[i] - index) == 1)
                    return false;
            }

            return true;
        }

        static int ClampInterior(int index, int last)
        {
            if (index < 1)
                return 1;
            if (index >= last)
                return last - 1;
            return index;
        }

        static int MaxLighthouses(int cellCount)
        {
            var interior = cellCount - 2;
            if (interior < 1)
                return 0;
            if (interior < 3)
                return 1;
            return 2;
        }

        static IReadOnlyList<PathPickup> PlacePickups(
            GridPath path,
            HashSet<int> taken,
            int glimpseCount,
            int beaconCount,
            IRandomSource random)
        {
            var last = path.Cells.Count - 1;
            var free = new List<int>();
            for (var i = 1; i < last; i++)
            {
                if (!taken.Contains(i))
                    free.Add(i);
            }

            Shuffle(free, random);

            var pickups = new List<PathPickup>();
            if (glimpseCount > 0 && free.Count > 0)
            {
                var index = free[0];
                free.RemoveAt(0);
                pickups.Add(new PathPickup(path.Cells[index], PathPickupKind.Glimpse));
            }

            if (beaconCount > 0 && free.Count > 0)
            {
                var index = free[0];
                free.RemoveAt(0);
                pickups.Add(new PathPickup(path.Cells[index], PathPickupKind.Beacon));
            }

            return pickups;
        }

        static void Shuffle(List<int> values, IRandomSource random)
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
