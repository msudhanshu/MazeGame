using Game.Core.Memory;
using Game.Unity.Memory;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public class MemoryWallDisplayTests
    {
        [Test]
        public void MemoryApplierPaintsEveryWallWithActivePhotoQuad()
        {
            var layout = DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                new XorShiftRandom(4));
            var level = MemoryCatalogFactory.CreateDefaultLevel();
            var kit = ScriptableObject.CreateInstance<MemoryWallDisplayKit>();
            kit.Configure(null);
            var slotCount = MemoryWallPlanner.CountSlots(layout, level.ToSpec());
            var snapshot = BuildPhotoOnlyCatalog(slotCount);
            var plan = MemoryWallPlanner.Plan(layout, snapshot, level.ToSpec(), new XorShiftRandom(4));
            var catalog = MemoryCatalogFactory.CreateDefaultCatalog();

            var photo = CreatePhotoTemplate();
            var wall = CreateWallTemplate();
            var parent = new GameObject("MemoryArena");
            try
            {
                var builder = new MazeArenaBuilder(
                    new MazeDimensions(),
                    null,
                    new PrefabWallSegmentFactory(wall, photo),
                    new MazePrefabKit { Wall = wall, Photo = photo });
                var built = builder.Build(layout, parent.transform, new XorShiftRandom(4), false);

                var painted = MemoryWallApplier.Apply(
                    plan,
                    built.Segments,
                    catalog,
                    kit,
                    new MazeDimensions(),
                    layout.Size);
                Assert.AreEqual(plan.Placements.Count, painted);
                foreach (var placement in plan.Placements)
                {
                    Assert.IsTrue(built.Segments.TryGetValue(placement.Edge.Normalized(), out var segment));
                    var photoTf = segment.AnchorFor(placement.Face);
                    Assert.IsNotNull(photoTf);
                    Assert.IsTrue(photoTf.gameObject.activeSelf);
                    Assert.Greater(photoTf.childCount, 0);
                    Assert.IsTrue(photoTf.GetChild(0).gameObject.activeSelf);
                    var mesh = photoTf.Find("Mesh") ?? photoTf.GetChild(0).Find("Mesh");
                    Assert.IsNotNull(mesh);
                    var toward = MazeGeometry.PhotoTowardCorridor(
                        placement.Edge, layout.Size, placement.Face);
                    Assert.Greater(Vector3.Dot(-mesh.forward, toward), 0.99f);
                    var tag = photoTf.GetComponent<MemoryDisplayTag>();
                    Assert.IsNotNull(tag);
                    Assert.AreEqual(placement.EntryId, tag.EntryId);
                    Assert.AreEqual(placement.Face, tag.Face);
                }

                var interiorPainted = 0;
                foreach (var segment in built.Segments.Values)
                {
                    if (!MazeWallFaces.IsInterior(segment.Edge, layout.Size))
                    {
                        Assert.IsNull(segment.transform.Find(MazeWallPhotoAnchors.OppositeName));
                        continue;
                    }

                    var opposite = segment.transform.Find(MazeWallPhotoAnchors.OppositeName);
                    Assert.IsNotNull(opposite);
                    if (opposite.gameObject.activeSelf)
                        interiorPainted++;
                }

                Assert.Greater(interiorPainted, 0);
            }
            finally
            {
                Object.DestroyImmediate(photo);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void BindDroppedPhotosFillsEmptySlotsAndAddsLeftoverFiles()
        {
            var assigned = MakeTexture("monalisa", 64, new Color32(10, 20, 30, 255));
            var extra = MakeTexture("bonus-photo", 64, new Color32(200, 10, 10, 255));
            var catalog = ScriptableObject.CreateInstance<MemoryWallCatalog>();
            try
            {
                catalog.Configure(
                    new[] { new MemoryGenreDef { Id = "painting", DisplayName = "Paintings" } },
                    new[]
                    {
                        new MemoryWallEntryDef
                        {
                            Id = "monalisa",
                            GenreId = "painting",
                            Kind = MemoryDisplayKind.Photo,
                            FrameVariantId = "frame"
                        }
                    });

                catalog.BindDroppedPhotos(new[] { assigned, extra });

                Assert.AreSame(assigned, catalog.FindImage("monalisa"));
                Assert.AreSame(extra, catalog.FindImage("bonus-photo"));
                Assert.AreEqual(2, catalog.Entries.Count);
                Assert.AreEqual("bonus-photo", catalog.Entries[1].Id);
                Assert.AreEqual(MemoryDisplayKind.Photo, catalog.Entries[1].Kind);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(assigned);
                Object.DestroyImmediate(extra);
            }
        }

        [Test]
        public void MemoryApplierPaintsTheAssignedCatalogTextureNotASwatch()
        {
            var texture = MakeTexture("hero-photo", 64, new Color32(12, 200, 40, 255));
            var layout = DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                new XorShiftRandom(4));
            var level = new MemoryLevelSpec(
                "level-1",
                new[] { "nature" },
                maxSameEntryPerWalk: 8,
                photoWeight: 3,
                reliefWeight: 0,
                object3dWeight: 0,
                decorateOppositeFaces: false);
            var catalog = ScriptableObject.CreateInstance<MemoryWallCatalog>();
            catalog.Configure(
                new[] { new MemoryGenreDef { Id = "nature", DisplayName = "Nature" } },
                new[]
                {
                    new MemoryWallEntryDef
                    {
                        Id = "hero-photo",
                        GenreId = "nature",
                        Kind = MemoryDisplayKind.Photo,
                        FrameVariantId = "plain",
                        Image = texture
                    }
                });
            var kit = ScriptableObject.CreateInstance<MemoryWallDisplayKit>();
            kit.Configure(null);
            var snapshot = MemoryCatalogExpander.EnsureCapacity(
                catalog.ToSnapshot(),
                MemoryWallPlanner.CountSlots(layout, level),
                level.MaxSameEntryPerWalk);
            var plan = MemoryWallPlanner.Plan(layout, snapshot, level, new XorShiftRandom(4));
            var photo = CreatePhotoTemplate();
            var wall = CreateWallTemplate();
            var parent = new GameObject("TextureArena");
            try
            {
                var builder = new MazeArenaBuilder(
                    new MazeDimensions(),
                    null,
                    new PrefabWallSegmentFactory(wall, photo),
                    new MazePrefabKit { Wall = wall, Photo = photo });
                var built = builder.Build(layout, parent.transform, new XorShiftRandom(4), false);
                var painted = MemoryWallApplier.Apply(
                    plan,
                    built.Segments,
                    catalog,
                    kit,
                    new MazeDimensions(),
                    layout.Size);
                Assert.Greater(painted, 0);

                var hero = 0;
                foreach (var placement in plan.Placements)
                {
                    if (placement.EntryId != "hero-photo")
                        continue;
                    Assert.IsTrue(built.Segments.TryGetValue(placement.Edge.Normalized(), out var segment));
                    var mesh = FindMesh(segment.AnchorFor(placement.Face));
                    Assert.IsNotNull(mesh);
                    var renderer = mesh.GetComponent<Renderer>();
                    Assert.IsNotNull(renderer);
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, 0);
                    var applied = block.GetTexture(Shader.PropertyToID("_BaseMap"))
                                  ?? block.GetTexture(Shader.PropertyToID("_MainTex"));
                    Assert.AreSame(texture, applied);
                    Assert.AreEqual(64, texture.width);
                    hero++;
                }

                Assert.Greater(hero, 0);
            }
            finally
            {
                Object.DestroyImmediate(photo);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(kit);
                Object.DestroyImmediate(texture);
            }
        }

        static Texture2D MakeTexture(string name, int size, Color32 color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return tex;
        }

        static Transform FindMesh(Transform anchor)
        {
            if (anchor == null)
                return null;
            return anchor.Find("Mesh") ?? (anchor.childCount > 0 ? anchor.GetChild(0).Find("Mesh") : null);
        }

        static MemoryCatalogSnapshot BuildPhotoOnlyCatalog(int count)
        {
            var genres = new[] { new MemoryGenreSnapshot("nature", "Nature") };
            var entries = new MemoryEntrySnapshot[count];
            for (var i = 0; i < count; i++)
                entries[i] = new MemoryEntrySnapshot("entry-" + i, "nature", MemoryDisplayKind.Photo, "plain");
            return new MemoryCatalogSnapshot(genres, entries);
        }

        static GameObject CreateWallTemplate()
        {
            var root = new GameObject("WallTemplate");
            root.AddComponent<MazeWallSegment>();
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root.transform, false);
            return root;
        }

        static GameObject CreatePhotoTemplate()
        {
            var root = new GameObject("PhotoTemplate");
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mesh.name = "Mesh";
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(root.transform, false);
            root.SetActive(false);
            return root;
        }
    }
}
