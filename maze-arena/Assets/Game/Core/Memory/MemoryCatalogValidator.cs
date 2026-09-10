using System;
using System.Collections.Generic;

namespace Game.Core.Memory
{
    public sealed class MemoryCatalogValidationResult
    {
        public MemoryCatalogValidationResult(IReadOnlyList<string> errors)
        {
            Errors = errors ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
    }

    public static class MemoryCatalogValidator
    {
        public static MemoryCatalogValidationResult Validate(MemoryCatalogSnapshot catalog, MemoryLevelSpec level)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errors.Add("Catalog is required.");
                return new MemoryCatalogValidationResult(errors);
            }

            if (level == null)
            {
                errors.Add("Level spec is required.");
                return new MemoryCatalogValidationResult(errors);
            }

            var genreIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.Genres.Count; i++)
            {
                var genre = catalog.Genres[i];
                if (!genreIds.Add(genre.Id))
                    errors.Add("Duplicate genre id '" + genre.Id + "'.");
            }

            var entryIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                var entry = catalog.Entries[i];
                if (!entryIds.Add(entry.Id))
                    errors.Add("Duplicate entry id '" + entry.Id + "'.");
                if (!genreIds.Contains(entry.GenreId))
                    errors.Add("Entry '" + entry.Id + "' references unknown genre '" + entry.GenreId + "'.");
            }

            if (catalog.Entries.Count == 0)
                errors.Add("Catalog has no wall entries.");

            if (level.EnabledGenreIds.Count == 0)
                errors.Add("Level '" + level.LevelId + "' enables no genres.");

            var enabled = new HashSet<string>(level.EnabledGenreIds, StringComparer.Ordinal);
            var hasEnabledEntry = false;
            for (var i = 0; i < catalog.Entries.Count; i++)
            {
                if (enabled.Contains(catalog.Entries[i].GenreId))
                {
                    hasEnabledEntry = true;
                    break;
                }
            }

            if (!hasEnabledEntry)
                errors.Add("No catalog entries match enabled genres for level '" + level.LevelId + "'.");

            return new MemoryCatalogValidationResult(errors);
        }
    }
}
