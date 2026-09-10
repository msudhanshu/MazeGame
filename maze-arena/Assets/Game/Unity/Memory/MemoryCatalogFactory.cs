using Game.Core.Memory;
using UnityEngine;

namespace Game.Unity.Memory
{
    public static class MemoryCatalogFactory
    {
        public static MemoryWallCatalog CreateDefaultCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<MemoryWallCatalog>();
            catalog.name = "DefaultMemoryCatalog";
            catalog.Configure(
                new[]
                {
                    new MemoryGenreDef { Id = "nature", DisplayName = "Nature" },
                    new MemoryGenreDef { Id = "landmarks", DisplayName = "Landmarks" },
                    new MemoryGenreDef { Id = "objects", DisplayName = "Objects" },
                    new MemoryGenreDef { Id = "painting", DisplayName = "Paintings" },
                    new MemoryGenreDef { Id = "art", DisplayName = "Art" }
                },
                new[]
                {
                    Entry("desert", "nature", MemoryDisplayKind.Photo, "polaroid", new Color(0.86f, 0.72f, 0.38f)),
                    Entry("waterfall", "nature", MemoryDisplayKind.Photo, "frame", new Color(0.35f, 0.68f, 0.82f)),
                    Entry("clock", "landmarks", MemoryDisplayKind.Relief, "plain", new Color(0.75f, 0.7f, 0.55f)),
                    Entry("pot", "objects", MemoryDisplayKind.Object3d, string.Empty, new Color(0.6f, 0.35f, 0.2f)),
                    Entry("lamp", "objects", MemoryDisplayKind.Object3d, string.Empty, new Color(0.85f, 0.75f, 0.35f)),
                    Entry("monalisa", "painting", MemoryDisplayKind.Photo, "frame", new Color(0.35f, 0.25f, 0.15f)),
                    Entry("starrynight", "painting", MemoryDisplayKind.Photo, "glow", new Color(0.15f, 0.25f, 0.55f)),
                    Entry("scream", "painting", MemoryDisplayKind.Photo, "frame", new Color(0.65f, 0.35f, 0.25f)),
                    Entry("studio", "art", MemoryDisplayKind.Photo, "plain", new Color(0.45f, 0.35f, 0.25f)),
                    Entry("illustration", "art", MemoryDisplayKind.Photo, "frame", new Color(0.25f, 0.45f, 0.35f)),
                    Entry("watercolor", "art", MemoryDisplayKind.Photo, "glow", new Color(0.35f, 0.25f, 0.45f))
                });
            return catalog;
        }

        public static MemoryLevelProfile CreateDefaultLevel()
        {
            var level = ScriptableObject.CreateInstance<MemoryLevelProfile>();
            level.name = "DefaultMemoryLevel";
            return level;
        }

        public static MemoryWallDisplayKit CreateDefaultDisplayKit()
        {
            var kit = ScriptableObject.CreateInstance<MemoryWallDisplayKit>();
            kit.name = "DefaultMemoryDisplayKit";
            kit.Configure(Resources.Load<GameObject>("NixinMaze/Photo"));
            return kit;
        }

        static MemoryWallEntryDef Entry(string id, string genre, MemoryDisplayKind kind, string frame, Color color)
        {
            return new MemoryWallEntryDef
            {
                Id = id,
                GenreId = genre,
                Kind = kind,
                FrameVariantId = frame,
                Image = CreateSwatch(id, color),
                ObjectPrefab = kind == MemoryDisplayKind.Object3d ? CreateObjectPrefab(id, color) : null
            };
        }

        static Texture2D CreateSwatch(string name, Color color)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.name = name;
            var pixels = new Color32[32 * 32];
            var c = (Color32)color;
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = c;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }

        static GameObject CreateObjectPrefab(string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MemoryObject_" + name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                renderer.sharedMaterial = mat;
            }

            go.SetActive(false);
            return go;
        }
    }
}
