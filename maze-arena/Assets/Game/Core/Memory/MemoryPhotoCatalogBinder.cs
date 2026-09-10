using System;
using System.Collections.Generic;

namespace Game.Core.Memory
{
    /// <summary>
    /// Merges dropped photo file ids into a catalog snapshot. Existing entries stay
    /// unchanged; leftover filenames become extra photo entries.
    /// </summary>
    public static class MemoryPhotoCatalogBinder
    {
        public const string ExtraGenreId = "art";
        public const string ExtraGenreName = "Art";
        public const string ExtraFrameVariantId = "plain";

        public static MemoryCatalogSnapshot Bind(MemoryCatalogSnapshot catalog, IReadOnlyList<string> photoFileIds)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (photoFileIds == null || photoFileIds.Count == 0)
                return catalog;

            var existing = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < catalog.Entries.Count; i++)
                existing.Add(catalog.Entries[i].Id);

            List<MemoryEntrySnapshot> extras = null;
            for (var i = 0; i < photoFileIds.Count; i++)
            {
                var id = photoFileIds[i];
                if (string.IsNullOrEmpty(id) || !existing.Add(id))
                    continue;

                extras ??= new List<MemoryEntrySnapshot>();
                extras.Add(new MemoryEntrySnapshot(
                    id,
                    GenreIdForExtras(catalog),
                    MemoryDisplayKind.Photo,
                    ExtraFrameVariantId));
            }

            if (extras == null)
                return catalog;

            var genres = EnsureExtraGenre(catalog);
            var entries = new List<MemoryEntrySnapshot>(catalog.Entries.Count + extras.Count);
            for (var i = 0; i < catalog.Entries.Count; i++)
                entries.Add(catalog.Entries[i]);
            for (var i = 0; i < extras.Count; i++)
                entries.Add(extras[i]);

            return new MemoryCatalogSnapshot(genres, entries);
        }

        static string GenreIdForExtras(MemoryCatalogSnapshot catalog)
        {
            for (var i = 0; i < catalog.Genres.Count; i++)
            {
                if (catalog.Genres[i].Id == ExtraGenreId)
                    return ExtraGenreId;
            }

            return catalog.Genres.Count > 0 ? catalog.Genres[0].Id : ExtraGenreId;
        }

        static IReadOnlyList<MemoryGenreSnapshot> EnsureExtraGenre(MemoryCatalogSnapshot catalog)
        {
            var genreId = GenreIdForExtras(catalog);
            for (var i = 0; i < catalog.Genres.Count; i++)
            {
                if (catalog.Genres[i].Id == genreId)
                    return catalog.Genres;
            }

            var genres = new MemoryGenreSnapshot[catalog.Genres.Count + 1];
            for (var i = 0; i < catalog.Genres.Count; i++)
                genres[i] = catalog.Genres[i];
            genres[catalog.Genres.Count] = new MemoryGenreSnapshot(ExtraGenreId, ExtraGenreName);
            return genres;
        }
    }
}
