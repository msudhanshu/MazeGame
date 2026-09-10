using System;
using System.Collections.Generic;

namespace Game.Core.Memory
{
    public sealed class MemoryGenreSnapshot
    {
        public MemoryGenreSnapshot(string id, string name)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Genre id is required.", nameof(id));
            Id = id;
            Name = name ?? id;
        }

        public string Id { get; }
        public string Name { get; }
    }

    public sealed class MemoryEntrySnapshot
    {
        public MemoryEntrySnapshot(
            string id,
            string genreId,
            MemoryDisplayKind kind,
            string frameVariantId,
            int selectionWeight = 1)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Entry id is required.", nameof(id));
            if (string.IsNullOrEmpty(genreId))
                throw new ArgumentException("Genre id is required.", nameof(genreId));
            if (selectionWeight < 1)
                throw new ArgumentOutOfRangeException(nameof(selectionWeight));

            Id = id;
            GenreId = genreId;
            Kind = kind;
            FrameVariantId = frameVariantId ?? string.Empty;
            SelectionWeight = selectionWeight;
        }

        public string Id { get; }
        public string GenreId { get; }
        public MemoryDisplayKind Kind { get; }
        public string FrameVariantId { get; }
        public int SelectionWeight { get; }
    }

    public sealed class MemoryCatalogSnapshot
    {
        public MemoryCatalogSnapshot(
            IReadOnlyList<MemoryGenreSnapshot> genres,
            IReadOnlyList<MemoryEntrySnapshot> entries)
        {
            Genres = genres ?? Array.Empty<MemoryGenreSnapshot>();
            Entries = entries ?? Array.Empty<MemoryEntrySnapshot>();
        }

        public IReadOnlyList<MemoryGenreSnapshot> Genres { get; }
        public IReadOnlyList<MemoryEntrySnapshot> Entries { get; }
    }
}
