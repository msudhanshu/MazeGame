using NUnit.Framework;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public class MazeGeometryTests
    {
        [Test]
        public void CellFromWorldRoundTripsCellCenter()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 3f, Origin = new Vector3(4f, 0f, -6f) };
            var size = new GridSize(5, 7);
            var expected = new GridCoord(2, 4);
            var world = MazeGeometry.CellCenter(expected, dims);
            world.y = 1.4f;

            Assert.IsTrue(MazeGeometry.TryCellFromWorld(world, dims, size, out var cell));
            Assert.AreEqual(expected, cell);
            Assert.AreEqual(expected, MazeGeometry.CellFromWorld(world, dims));
        }

        [Test]
        public void TryCellFromWorldRejectsPointsOutsideTheBoard()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 3f };
            var outside = new Vector3(-0.1f, 0f, 0.5f);
            Assert.IsFalse(MazeGeometry.TryCellFromWorld(outside, dims, new GridSize(3, 3), out _));
        }

        [Test]
        public void CellCenterSitsOnTheXzPlaneAtHalfCell()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 3f, Origin = Vector3.zero };
            var center = MazeGeometry.CellCenter(new GridCoord(1, 2), dims);

            Assert.AreEqual(3f, center.x, 0.0001f);
            Assert.AreEqual(0f, center.y, 0.0001f);
            Assert.AreEqual(5f, center.z, 0.0001f);
        }

        [Test]
        public void EastWallSitsOnThePositiveXFace()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
            var xform = MazeGeometry.ForEdge(new MazeEdge(new GridCoord(0, 0), WallSide.East), dims);

            Assert.AreEqual(2f, xform.Position.x, 0.0001f);
            Assert.AreEqual(2f, xform.Position.y, 0.0001f);
            Assert.AreEqual(1f, xform.Position.z, 0.0001f);
            Assert.AreEqual(0.2f, xform.Scale.x, 0.0001f);
            Assert.AreEqual(4f, xform.Scale.y, 0.0001f);
            Assert.AreEqual(2f, xform.Scale.z, 0.0001f);
        }

        [Test]
        public void NorthWallSitsOnThePositiveZFace()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
            var xform = MazeGeometry.ForEdge(new MazeEdge(new GridCoord(0, 0), WallSide.North), dims);

            Assert.AreEqual(1f, xform.Position.x, 0.0001f);
            Assert.AreEqual(2f, xform.Position.z, 0.0001f);
            Assert.AreEqual(2f, xform.Scale.x, 0.0001f);
            Assert.AreEqual(0.2f, xform.Scale.z, 0.0001f);
        }

        [Test]
        public void AvatarSpawnMatchesCellCenterAndFacesInward()
        {
            var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 3f };
            var entry = new MazeOpening(new GridCoord(1, 0), WallSide.South);
            var pose = Game.Core.AvatarSpawn.AtEntry(entry, dims.CellSize, dims.Origin.x, dims.Origin.y, dims.Origin.z);
            var center = MazeGeometry.CellCenter(entry.Cell, dims);

            Assert.AreEqual(center.x, pose.X, 0.0001f);
            Assert.AreEqual(center.z, pose.Z, 0.0001f);
            Assert.AreEqual(0f, pose.YawDegrees, 0.0001f);
        }

        [Test]
        public void PictureQuadLocalRotationTurnsTheVisibleFaceTowardTheCorridor()
        {
            Assert.AreEqual(180f, MazeGeometry.PictureQuadLocalRotation.eulerAngles.y, 0.01f);
        }

        [Test]
        public void ApplyPhotoLocalScalesMeshChildNotThePhotoRoot()
        {
            var root = new GameObject("Photo");
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(root.transform, false);
            try
            {
                var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
                MazeGeometry.ApplyPhotoLocal(
                    root.transform,
                    new MazeEdge(new GridCoord(0, 0), WallSide.East),
                    dims,
                    new GridSize(4, 4));

                Assert.AreEqual(Vector3.one, root.transform.localScale);
                Assert.AreEqual(2f * 0.72f, mesh.transform.localScale.x, 0.0001f);
                Assert.AreEqual(4f * 0.55f, mesh.transform.localScale.y, 0.0001f);
                var expectedX = -(dims.WallThickness * 0.5f + MazeGeometry.PhotoSurfaceStandoff);
                Assert.AreEqual(expectedX, root.transform.localPosition.x, 0.0001f);
                var towardCorridor = MazeGeometry.PhotoTowardCorridor(
                    new MazeEdge(new GridCoord(0, 0), WallSide.East),
                    new GridSize(4, 4),
                    WallFace.Canonical);
                Assert.Greater(Vector3.Dot(-mesh.transform.forward, towardCorridor), 0.99f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyPhotoLocalOppositeFaceSitsOnTheOtherSideOfAnInteriorWall()
        {
            var root = new GameObject("PhotoOpposite");
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(root.transform, false);
            try
            {
                var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
                var edge = new MazeEdge(new GridCoord(0, 0), WallSide.East);
                var size = new GridSize(4, 4);
                MazeGeometry.ApplyPhotoLocal(root.transform, edge, dims, size, WallFace.Opposite);

                var expectedX = dims.WallThickness * 0.5f + MazeGeometry.PhotoSurfaceStandoff;
                Assert.AreEqual(expectedX, root.transform.localPosition.x, 0.0001f);
                Assert.AreEqual(1f, root.transform.forward.x, 0.0001f);
                Assert.AreEqual(0f, root.transform.forward.y, 0.0001f);
                Assert.AreEqual(0f, root.transform.forward.z, 0.0001f);
                Assert.Greater(Vector3.Dot(-mesh.transform.forward, Vector3.right), 0.99f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ApplyPhotoLocalScalesFrameRelativeToThePicture()
        {
            var root = new GameObject("Photo");
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(root.transform, false);
            var frame = new GameObject("Frame");
            frame.transform.SetParent(root.transform, false);
            frame.transform.localScale = new Vector3(1.14f, 1.14f, 1f);
            try
            {
                var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
                MazeGeometry.ApplyPhotoLocal(
                    root.transform,
                    new MazeEdge(new GridCoord(0, 0), WallSide.East),
                    dims,
                    new GridSize(4, 4));

                var picture = MazeGeometry.PictureSize(dims);
                Assert.AreEqual(picture.x * 1.12f, frame.transform.localScale.x, 0.0001f);
                Assert.AreEqual(picture.y * 1.12f, frame.transform.localScale.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WallColliderIsThickerAndLongerThanTheVisualMesh()
        {
            var visual = new Vector3(0.2f, 3f, 2f);
            var local = MazeGeometry.WallColliderLocalSize(visual);
            var world = new Vector3(local.x * visual.x, local.y * visual.y, local.z * visual.z);

            Assert.AreEqual(visual.x + MazeGeometry.WallColliderFacePad * 2f, world.x, 0.0001f);
            Assert.AreEqual(visual.y, world.y, 0.0001f);
            Assert.AreEqual(visual.z + MazeGeometry.WallColliderEndPad * 2f, world.z, 0.0001f);
        }

        [Test]
        public void NorthWallColliderPadsTheThinZAxisAsThickness()
        {
            var visual = new Vector3(2f, 3f, 0.2f);
            var local = MazeGeometry.WallColliderLocalSize(visual);
            var world = new Vector3(local.x * visual.x, local.y * visual.y, local.z * visual.z);

            Assert.AreEqual(visual.x + MazeGeometry.WallColliderEndPad * 2f, world.x, 0.0001f);
            Assert.AreEqual(visual.z + MazeGeometry.WallColliderFacePad * 2f, world.z, 0.0001f);
        }

        [Test]
        public void ApplyPhotoLocalOrientsANestedFrameMeshTowardTheCorridor()
        {
            var root = new GameObject("Photo");
            var frame = new GameObject("PhotoPlain");
            frame.transform.SetParent(root.transform, false);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mesh.name = "Mesh";
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(frame.transform, false);
            try
            {
                var dims = new MazeDimensions { CellSize = 2f, WallThickness = 0.2f, WallHeight = 4f };
                var edge = new MazeEdge(new GridCoord(0, 0), WallSide.East);
                var size = new GridSize(4, 4);
                MazeGeometry.ApplyPhotoLocal(root.transform, edge, dims, size);

                Assert.AreEqual(Vector3.one, root.transform.localScale);
                var towardCorridor = MazeGeometry.PhotoTowardCorridor(edge, size, WallFace.Canonical);
                Assert.Greater(Vector3.Dot(-mesh.transform.forward, towardCorridor), 0.99f);
                Assert.AreEqual(2f * 0.72f, mesh.transform.localScale.x, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
