using System;
using System.Collections.Generic;
using Nixin.Game.Core;
using Nixin.Maze.Core;

namespace Game.Core.Memory
{
    public static class MemoryWallPlanner
    {
        public static MemoryWallPlan Plan(
            MazeLayout layout,
            MemoryCatalogSnapshot catalog,
            MemoryLevelSpec level,
            IRandomSource random,
            IReadOnlyList<string> previousLevelGenreIds = null)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (level == null)
                throw new ArgumentNullException(nameof(level));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var validation = MemoryCatalogValidator.Validate(catalog, level);
            if (!validation.IsValid)
                throw new InvalidOperationException(string.Join("; ", validation.Errors));

            var enabled = new HashSet<string>(level.EnabledGenreIds, StringComparer.Ordinal);
            var previous = previousLevelGenreIds ?? Array.Empty<string>();
            var previousSet = new HashSet<string>(previous, StringComparer.Ordinal);

            var pool = BuildPool(catalog, level, enabled, previousSet);
            if (pool.Count == 0)
                throw new InvalidOperationException("No eligible wall entries for this level.");

            var slots = CollectSlots(layout, level, random);
            EnsureEnoughUniqueEntries(slots.Count, pool, level);

            var placements = new MemoryWallPlacement[slots.Count];
            var entryUse = new Dictionary<string, int>(StringComparer.Ordinal);
            var genreUse = new Dictionary<string, int>(StringComparer.Ordinal);
            var genresUsed = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < slots.Count; i++)
            {
                var entry = PickEntry(pool, entryUse, genreUse, level, random);
                placements[i] = new MemoryWallPlacement(
                    slots[i].Edge,
                    entry.Id,
                    entry.GenreId,
                    entry.Kind,
                    entry.FrameVariantId,
                    slots[i].Face);
                entryUse[entry.Id] = entryUse.TryGetValue(entry.Id, out var ec) ? ec + 1 : 1;
                genreUse[entry.GenreId] = genreUse.TryGetValue(entry.GenreId, out var gc) ? gc + 1 : 1;
                genresUsed.Add(entry.GenreId);
            }

            return new MemoryWallPlan(level.LevelId, placements, new List<string>(genresUsed));
        }

        /// <summary>
        /// Maximum display slots this level can fill (canonical face of every wall, plus
        /// opposite faces of interior walls when enabled). Chance &lt; 1 still counts the
        /// maximum so the catalog expander does not under-provision.
        /// </summary>
        public static int CountSlots(MazeLayout layout, MemoryLevelSpec level)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            var walls = layout.Grid.OccupiedWalls();
            var count = walls.Count;
            if (!level.DecorateOppositeFaces || level.OppositeFaceChance <= 0f)
                return count;

            for (var i = 0; i < walls.Count; i++)
            {
                if (MazeWallFaces.IsInterior(walls[i], layout.Size))
                    count++;
            }

            return count;
        }

        static List<WallSlot> CollectSlots(MazeLayout layout, MemoryLevelSpec level, IRandomSource random)
        {
            var walls = layout.Grid.OccupiedWalls();
            var slots = new List<WallSlot>(walls.Count * 2);
            for (var i = 0; i < walls.Count; i++)
            {
                var edge = walls[i];
                slots.Add(new WallSlot(edge, WallFace.Canonical));
                if (!level.DecorateOppositeFaces || !MazeWallFaces.IsInterior(edge, layout.Size))
                    continue;
                if (level.OppositeFaceChance < 1f && random.NextDouble() >= level.OppositeFaceChance)
                    continue;
                slots.Add(new WallSlot(edge, WallFace.Opposite));
            }

            return slots;
        }

        static List<MemoryEntrySnapshot> BuildPool(
            MemoryCatalogSnapshot catalog,
            MemoryLevelSpec level,
            HashSet<string> enabled,
            HashSet<string> previousGenres)
        {
            var pool = new List<MemoryEntrySnapshot>();
            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (!enabled.Contains(entry.GenreId))
                    continue;
                if (level.AvoidPreviousGenres && previousGenres.Contains(entry.GenreId))
                    continue;
                if (!MatchesKindWeight(entry.Kind, level))
                    continue;

                for (var w = 0; w < entry.SelectionWeight; w++)
                    pool.Add(entry);
            }

            return pool;
        }

        static bool MatchesKindWeight(MemoryDisplayKind kind, MemoryLevelSpec level)
        {
            switch (kind)
            {
                case MemoryDisplayKind.Photo: return level.PhotoWeight > 0;
                case MemoryDisplayKind.Relief: return level.ReliefWeight > 0;
                case MemoryDisplayKind.Object3d: return level.Object3dWeight > 0;
                default: return false;
            }
        }

        static void EnsureEnoughUniqueEntries(int slotCount, List<MemoryEntrySnapshot> pool, MemoryLevelSpec level)
        {
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < pool.Count; i++)
                unique.Add(pool[i].Id);

            var capacity = unique.Count * level.MaxSameEntryPerWalk;
            if (slotCount > capacity)
            {
                throw new InvalidOperationException(
                    "Need at least " + slotCount + " wall slots but catalog allows " + capacity
                    + " (" + unique.Count + " unique entries × " + level.MaxSameEntryPerWalk + " each).");
            }
        }

        static MemoryEntrySnapshot PickEntry(
            List<MemoryEntrySnapshot> pool,
            Dictionary<string, int> entryUse,
            Dictionary<string, int> genreUse,
            MemoryLevelSpec level,
            IRandomSource random)
        {
            var attempts = pool.Count * 4 + 8;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var index = random.NextInt(pool.Count);
                var candidate = pool[index];
                if (entryUse.TryGetValue(candidate.Id, out var entryCount) && entryCount >= level.MaxSameEntryPerWalk)
                    continue;
                if (level.MaxSameGenrePerWalk > 0
                    && genreUse.TryGetValue(candidate.GenreId, out var genreCount)
                    && genreCount >= level.MaxSameGenrePerWalk)
                    continue;
                return candidate;
            }

            for (var i = 0; i < pool.Count; i++)
            {
                var candidate = pool[i];
                if (!entryUse.TryGetValue(candidate.Id, out var entryCount) || entryCount < level.MaxSameEntryPerWalk)
                {
                    if (level.MaxSameGenrePerWalk <= 0
                        || !genreUse.TryGetValue(candidate.GenreId, out var genreCount)
                        || genreCount < level.MaxSameGenrePerWalk)
                        return candidate;
                }
            }

            throw new InvalidOperationException("Could not assign a wall entry without breaking placement limits.");
        }

        readonly struct WallSlot
        {
            public WallSlot(MazeEdge edge, WallFace face)
            {
                Edge = edge;
                Face = face;
            }

            public MazeEdge Edge { get; }
            public WallFace Face { get; }
        }
    }
}
