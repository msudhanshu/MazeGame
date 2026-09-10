using System;
using System.Collections.Generic;

namespace Game.Core.Memory
{
    public static class MemoryCatalogExpander
    {
        public static MemoryCatalogSnapshot EnsureCapacity(
            MemoryCatalogSnapshot catalog,
            int wallCount,
            int maxSameEntryPerWalk)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (wallCount < 0)
                throw new ArgumentOutOfRangeException(nameof(wallCount));
            if (maxSameEntryPerWalk < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSameEntryPerWalk));

            var requiredUnique = (wallCount + maxSameEntryPerWalk - 1) / maxSameEntryPerWalk;
            if (catalog.Entries.Count >= requiredUnique)
                return catalog;

            var genres = new List<MemoryGenreSnapshot>(catalog.Genres);
            if (genres.Count == 0)
                genres.Add(new MemoryGenreSnapshot("generated", "Generated"));

            var genreId = genres[0].Id;
            var entries = new List<MemoryEntrySnapshot>(catalog.Entries);
            var existing = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Count; i++)
                existing.Add(entries[i].Id);

            for (var i = entries.Count; i < requiredUnique; i++)
            {
                var id = "generated-" + i;
                while (!existing.Add(id))
                    id = "generated-" + Guid.NewGuid().ToString("N");
                entries.Add(new MemoryEntrySnapshot(id, genreId, MemoryDisplayKind.Photo, "plain"));
            }

            return new MemoryCatalogSnapshot(genres, entries);
        }
    }
}
