using System;
using System.Collections.Generic;
using Game.Core.Memory;
using UnityEngine;

namespace Game.Unity.Memory
{
    [CreateAssetMenu(fileName = "MemoryWallCatalog", menuName = "Nixin Studio/Maze Arena/Memory Wall Catalog")]
    public sealed class MemoryWallCatalog : ScriptableObject
    {
        [SerializeField] MemoryGenreDef[] genres = Array.Empty<MemoryGenreDef>();
        [SerializeField] MemoryWallEntryDef[] entries = Array.Empty<MemoryWallEntryDef>();

        public IReadOnlyList<MemoryWallEntryDef> Entries => entries;

        public void Configure(MemoryGenreDef[] genreDefs, MemoryWallEntryDef[] entryDefs)
        {
            genres = genreDefs ?? Array.Empty<MemoryGenreDef>();
            entries = entryDefs ?? Array.Empty<MemoryWallEntryDef>();
        }

        public MemoryCatalogSnapshot ToSnapshot()
        {
            var genreList = new MemoryGenreSnapshot[genres.Length];
            for (var i = 0; i < genres.Length; i++)
                genreList[i] = genres[i].ToSnapshot();

            var entryList = new MemoryEntrySnapshot[entries.Length];
            for (var i = 0; i < entries.Length; i++)
                entryList[i] = entries[i].ToSnapshot();

            return new MemoryCatalogSnapshot(genreList, entryList);
        }

        public Texture2D FindImage(string entryId)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].Id == entryId)
                    return entries[i].Image;
            }

            return null;
        }

        public Texture2D ResolveImage(string entryId)
        {
            var image = FindImage(entryId);
            if (image != null)
                return image;
            return ProceduralSwatch.ForId(entryId);
        }

        /// <summary>
        /// Assigns dropped textures by filename = entry id. Extra files become new photo entries.
        /// </summary>
        public void BindDroppedPhotos(Texture2D[] textures)
        {
            if (textures == null || textures.Length == 0)
                return;

            var byId = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
            var fileIds = new List<string>(textures.Length);
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (texture == null || string.IsNullOrEmpty(texture.name))
                    continue;
                if (byId.ContainsKey(texture.name))
                    continue;
                byId[texture.name] = texture;
                fileIds.Add(texture.name);
            }

            if (fileIds.Count == 0)
                return;

            var bound = MemoryPhotoCatalogBinder.Bind(ToSnapshot(), fileIds);
            if (bound.Entries.Count > entries.Length)
                AppendMissingEntries(bound);

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;
                if (byId.TryGetValue(entry.Id, out var dropped))
                    entry.Image = dropped;
            }
        }

        void AppendMissingEntries(MemoryCatalogSnapshot bound)
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null)
                    existing.Add(entries[i].Id);
            }

            var grown = new List<MemoryWallEntryDef>(bound.Entries.Count);
            for (var i = 0; i < entries.Length; i++)
                grown.Add(entries[i]);

            for (var i = 0; i < bound.Entries.Count; i++)
            {
                var extra = bound.Entries[i];
                if (existing.Contains(extra.Id))
                    continue;
                grown.Add(new MemoryWallEntryDef
                {
                    Id = extra.Id,
                    GenreId = extra.GenreId,
                    Kind = extra.Kind,
                    FrameVariantId = extra.FrameVariantId,
                    SelectionWeight = extra.SelectionWeight
                });
            }

            entries = grown.ToArray();
        }

        public Texture2D FindNormalMap(string entryId)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].Id == entryId)
                    return entries[i].NormalMap;
            }

            return null;
        }

        public GameObject FindObjectPrefab(string entryId)
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].Id == entryId)
                    return entries[i].ObjectPrefab;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class MemoryGenreDef
    {
        public string Id = "genre";
        public string DisplayName = "Genre";

        public MemoryGenreSnapshot ToSnapshot()
        {
            return new MemoryGenreSnapshot(Id, DisplayName);
        }
    }

    [Serializable]
    public sealed class MemoryWallEntryDef
    {
        public string Id = "entry";
        public string GenreId = "genre";
        public MemoryDisplayKind Kind = MemoryDisplayKind.Photo;
        public string FrameVariantId = "plain";
        [Min(1)] public int SelectionWeight = 1;
        public Texture2D Image;
        public Texture2D NormalMap;
        public GameObject ObjectPrefab;

        public MemoryEntrySnapshot ToSnapshot()
        {
            return new MemoryEntrySnapshot(Id, GenreId, Kind, FrameVariantId, SelectionWeight);
        }
    }
}
