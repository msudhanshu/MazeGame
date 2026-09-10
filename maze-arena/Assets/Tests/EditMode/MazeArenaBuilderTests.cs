using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public class MazeArenaBuilderTests
    {
        [Test]
        public void PrimitiveWallColliderOverlapsAtACorner()
        {
            var parent = new GameObject("CornerColliders");
            try
            {
                var dims = new MazeDimensions();
                var size = new GridSize(2, 2);
                var factory = new PrimitiveWallSegmentFactory();
                var east = factory.Create(
                    new MazeEdge(new GridCoord(0, 0), WallSide.East),
                    parent.transform,
                    dims,
                    null,
                    size);
                var north = factory.Create(
                    new MazeEdge(new GridCoord(0, 0), WallSide.North),
                    parent.transform,
                    dims,
                    null,
                    size);

                var eastBox = east.transform.Find("Mesh").GetComponent<BoxCollider>();
                var northBox = north.transform.Find("Mesh").GetComponent<BoxCollider>();
                Assert.IsTrue(eastBox.bounds.Intersects(northBox.bounds));
                Assert.Greater(eastBox.bounds.size.x, dims.WallThickness);
                Assert.Greater(northBox.bounds.size.z, dims.WallThickness);
                Assert.Greater(eastBox.bounds.size.z, dims.CellSize);
                Assert.Greater(northBox.bounds.size.x, dims.CellSize);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void BuilderEmitsOneSegmentPerOccupiedWallAndOneAnchorPerCell()
        {
            var layout = DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(4, 4), MazeDifficulty.Easy),
                new XorShiftRandom(2));
            var parent = new GameObject("ArenaTest");
            try
            {
                var builder = new MazeArenaBuilder(new MazeDimensions(), null, new PrimitiveWallSegmentFactory());
                var built = builder.Build(layout, parent.transform, new XorShiftRandom(2), includeCeiling: false);

                Assert.AreEqual(layout.Grid.OccupiedWalls().Count, built.Segments.Count);
                Assert.AreEqual(layout.Size.CellCount, built.Anchors.Count);
                Assert.IsNotNull(built.Floor);
                Assert.IsNull(built.Ceiling);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SeededStyleAssignmentIsDeterministic()
        {
            var set = ScriptableObject.CreateInstance<MazeStyleSet>();
            set.Assignment = MazeStyleAssignment.SeededRandom;
            set.Walls = new[]
            {
                ScriptableObject.CreateInstance<MazeWallStyle>(),
                ScriptableObject.CreateInstance<MazeWallStyle>()
            };
            set.Walls[0].name = "A";
            set.Walls[1].name = "B";

            var first = Fingerprint(set, 9);
            var second = Fingerprint(set, 9);
            var third = Fingerprint(set, 10);

            Assert.AreEqual(first, second);
            Assert.AreNotEqual(first, third);
        }

        static string Fingerprint(MazeStyleSet set, int seed)
        {
            var random = new XorShiftRandom(seed);
            var size = new GridSize(4, 4);
            var names = new string[16];
            for (var i = 0; i < names.Length; i++)
            {
                var edge = new MazeEdge(new GridCoord(i % 4, i / 4), WallSide.East);
                names[i] = set.StyleFor(edge, size, random).name;
            }

            return string.Join(",", names);
        }

        [Test]
        public void PhotoPainterFillsOneSlotPerSegment()
        {
            var layout = DifficultyTunedMazeGenerator.Generate(
                MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                new XorShiftRandom(4));
            var content = MazeContentPlacer.Place(
                layout,
                new MazeContentRules(0, 0, photoEveryCorridorWall: true),
                new XorShiftRandom(4));

            var parent = new GameObject("PhotoTest");
            try
            {
                var builder = new MazeArenaBuilder(new MazeDimensions(), null, new PrimitiveWallSegmentFactory());
                var built = builder.Build(layout, parent.transform, new XorShiftRandom(4), false);
                var photos = ProceduralPhotoSource.CreateDefault();
                var painted = MazePhotoWallPainter.Paint(content.Decals, built.Segments, photos);

                Assert.AreEqual(content.Decals.Count, painted);
                Assert.AreEqual(built.Segments.Count, painted);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void PhotoPainterEnablesPrefabPhotoWithMeshChildRenderer()
        {
            var wall = CreateWallTemplate();
            var photo = CreatePhotoTemplate();
            try
            {
                var layout = DifficultyTunedMazeGenerator.Generate(
                    MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                    new XorShiftRandom(4));
                var content = MazeContentPlacer.Place(
                    layout,
                    new MazeContentRules(0, 0, photoEveryCorridorWall: true),
                    new XorShiftRandom(4));
                var parent = new GameObject("PrefabPhotoPaint");
                try
                {
                    var builder = new MazeArenaBuilder(
                        new MazeDimensions(),
                        null,
                        new PrefabWallSegmentFactory(wall, photo),
                        new MazePrefabKit { Wall = wall, Photo = photo });
                    var built = builder.Build(layout, parent.transform, new XorShiftRandom(4), false);
                    var painted = MazePhotoWallPainter.Paint(
                        content.Decals,
                        built.Segments,
                        ProceduralPhotoSource.CreateDefault());

                    Assert.AreEqual(content.Decals.Count, painted);
                    foreach (var segment in built.Segments.Values)
                    {
                        var photoTf = segment.transform.Find("Photo");
                        Assert.IsTrue(photoTf.gameObject.activeSelf);
                        Assert.IsTrue(photoTf.Find("Mesh").GetComponent<Renderer>().enabled);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(parent);
                }
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(photo);
            }
        }
        [Test]
        public void DefaultPrefabKitLoadsWallFloorCeilingCellAndPhotoFromResources()
        {
            var kit = MazePrefabKit.LoadDefaults();
            Assert.IsNotNull(kit.Wall, "NixinMaze/Wall prefab");
            Assert.IsNotNull(kit.Floor, "NixinMaze/Floor prefab");
            Assert.IsNotNull(kit.Ceiling, "NixinMaze/Ceiling prefab");
            Assert.IsNotNull(kit.Cell, "NixinMaze/Cell prefab");
            Assert.IsNotNull(kit.Photo, "NixinMaze/Photo prefab");
            Assert.IsNotNull(kit.Wall.transform.Find("Mesh"));
            Assert.IsNull(kit.Wall.transform.Find("Photo"));
            Assert.IsNotNull(kit.Wall.GetComponent<MazeWallSegment>());
            Assert.IsNotNull(kit.Cell.GetComponent<MazeCellAnchor>());
            Assert.IsNotNull(kit.Photo.transform.Find("Mesh"));
            Assert.IsNotNull(kit.Photo.transform.Find("Mesh").GetComponent<Renderer>().sharedMaterial);
        }

        [Test]
        public void PrefabFactoryScalesTheMeshChildNotTheRoot()
        {
            var wall = CreateWallTemplate();
            var photo = CreatePhotoTemplate();
            try
            {
                var layout = DifficultyTunedMazeGenerator.Generate(
                    MazeSpec.For(new GridSize(3, 3), MazeDifficulty.Easy),
                    new XorShiftRandom(4));
                var parent = new GameObject("PrefabArena");
                try
                {
                    var kit = new MazePrefabKit { Wall = wall, Photo = photo };
                    var builder = new MazeArenaBuilder(
                        new MazeDimensions(),
                        null,
                        new PrefabWallSegmentFactory(wall, photo),
                        kit);
                    var built = builder.Build(layout, parent.transform, new XorShiftRandom(4), false);

                    Assert.AreEqual(layout.Grid.OccupiedWalls().Count, built.Segments.Count);
                    foreach (var segment in built.Segments.Values)
                    {
                        Assert.AreEqual(Vector3.one, segment.transform.localScale);
                        var mesh = segment.transform.Find("Mesh");
                        Assert.IsNotNull(mesh);
                        Assert.AreNotEqual(Vector3.one, mesh.localScale);

                        var photoTf = segment.transform.Find("Photo");
                        Assert.IsNotNull(photoTf);
                        Assert.AreEqual(Vector3.one, photoTf.localScale);
                        var picture = photoTf.Find("Mesh");
                        Assert.IsNotNull(picture);
                        Assert.AreNotEqual(Vector3.one, picture.localScale);

                        if (MazeWallFaces.IsInterior(segment.Edge, layout.Size))
                        {
                            var opposite = segment.transform.Find(MazeWallPhotoAnchors.OppositeName);
                            Assert.IsNotNull(opposite);
                            Assert.AreEqual(Vector3.one, opposite.localScale);
                            Assert.AreNotEqual(photoTf.localPosition, opposite.localPosition);
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(parent);
                }
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(photo);
            }
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
