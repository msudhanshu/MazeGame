using Game.Core;
using NUnit.Framework;
using Nixin.Locomotion;
using Nixin.Maze;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public class MazeWalkerTests
    {
        [Test]
        public void CreateUsesAWiderCapsuleSoCornersDoNotSwallowTheWalker()
        {
            MazeWalker walker = null;
            try
            {
                walker = MazeWalker.Create(null, null, null);
                var controller = walker.GetComponent<CharacterController>();
                Assert.AreEqual(MazeWalker.ControllerRadius, controller.radius, 0.0001f);
                Assert.AreEqual(MazeWalker.ControllerSkinWidth, controller.skinWidth, 0.0001f);
                Assert.Greater(controller.skinWidth, 0.04f);
            }
            finally
            {
                if (walker != null)
                    Object.DestroyImmediate(walker.gameObject);
            }
        }

        [Test]
        public void PlaceAtEntryPutsTheWalkerInTheEntryCell()
        {
            var arenaGo = new GameObject("ArenaWalk");
            var walkerGo = new GameObject("Walker");
            try
            {
                var arena = arenaGo.AddComponent<MazeArena>();
                arena.Width = 4;
                arena.Height = 4;
                arena.PaintPhotos = false;
                arena.Rebuild(2);

                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                var walker = walkerGo.AddComponent<MazeWalker>();
                walker.Arena = arena;
                walker.PlaceAtEntry();

                var pose = AvatarSpawn.AtEntry(arena.Layout.Entry, arena.Dimensions.CellSize);
                Assert.AreEqual(pose.X, walker.transform.position.x, 0.01f);
                Assert.AreEqual(pose.Z, walker.transform.position.z, 0.01f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(pose.YawDegrees, walker.transform.eulerAngles.y), 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(walkerGo);
                Object.DestroyImmediate(arenaGo);
            }
        }

        [Test]
        public void SetWalkingAddsDualTouchSticks()
        {
            var walkerGo = new GameObject("WalkerSticks");
            MazeWalker walker = null;
            try
            {
                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                walker = walkerGo.AddComponent<MazeWalker>();
                walker.SetWalking(true);
                Assert.NotNull(walkerGo.GetComponent<DualTouchSticks>());
            }
            finally
            {
                if (walker != null)
                    walker.SetWalking(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Object.DestroyImmediate(walkerGo);
            }
        }

        [Test]
        public void DefaultRailSchemeDoesNotActivateFirstPersonWalk()
        {
            var walkerGo = new GameObject("WalkerRail");
            MazeWalker walker = null;
            try
            {
                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                walker = walkerGo.AddComponent<MazeWalker>();
                walker.SetWalking(true);

                Assert.IsFalse(walkerGo.GetComponent<FirstPersonController>().Active);
                Assert.IsTrue(walkerGo.GetComponent<RailLocomotion>().Active);
                Assert.IsFalse(walkerGo.GetComponent<CharacterController>().enabled);
            }
            finally
            {
                if (walker != null)
                    walker.SetWalking(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Object.DestroyImmediate(walkerGo);
            }
        }

        [Test]
        public void RailJoystickSchemeStaysOnTheRail()
        {
            var walkerGo = new GameObject("WalkerJoystick");
            MazeWalker walker = null;
            try
            {
                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                walker = walkerGo.AddComponent<MazeWalker>();
                walker.Controls.Scheme = ArenaControlScheme.RailJoystick;
                walker.SetWalking(true);

                Assert.IsFalse(walkerGo.GetComponent<FirstPersonController>().Active);
                Assert.IsTrue(walkerGo.GetComponent<RailLocomotion>().Active);
                Assert.IsFalse(walkerGo.GetComponent<CharacterController>().enabled);
            }
            finally
            {
                if (walker != null)
                    walker.SetWalking(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Object.DestroyImmediate(walkerGo);
            }
        }

        [Test]
        public void DualStickSchemeActivatesFirstPersonWalk()
        {
            var walkerGo = new GameObject("WalkerDual");
            MazeWalker walker = null;
            try
            {
                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                walker = walkerGo.AddComponent<MazeWalker>();
                walker.Controls.Scheme = ArenaControlScheme.DualSticks;
                walker.SetWalking(true);

                Assert.IsTrue(walkerGo.GetComponent<FirstPersonController>().Active);
                Assert.IsFalse(walkerGo.GetComponent<RailLocomotion>().Active);
                Assert.IsTrue(walkerGo.GetComponent<CharacterController>().enabled);
            }
            finally
            {
                if (walker != null)
                    walker.SetWalking(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Object.DestroyImmediate(walkerGo);
            }
        }

        [Test]
        public void FloorPathGlowRebuildAddsAMeshFilterOnAChild()
        {
            var go = new GameObject("FloorWaypoints");
            try
            {
                var glow = go.AddComponent<FloorPathGlow>();
                glow.SetRunning(true);
                var arenaGo = new GameObject("GlowArena");
                try
                {
                    var arena = arenaGo.AddComponent<MazeArena>();
                    arena.Width = 4;
                    arena.Height = 4;
                    arena.PaintPhotos = false;
                    arena.Rebuild(2);
                    var rail = new CorridorRail(arena.Layout.Grid, arena.Dimensions.CellSize);
                    Assert.DoesNotThrow(() => glow.Rebuild(rail, 0f));
                    Assert.IsNull(go.GetComponent<MeshFilter>());
                    var mesh = go.transform.Find("Mesh");
                    Assert.NotNull(mesh);
                    var filter = mesh.GetComponent<MeshFilter>();
                    Assert.NotNull(filter);
                    Assert.NotNull(filter.sharedMesh);
                    Assert.Greater(filter.sharedMesh.vertexCount, 0);
                }
                finally
                {
                    Object.DestroyImmediate(arenaGo);
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PlaceAtEntryBuildsFloorPathGlowWithoutMissingMeshFilter()
        {
            var arenaGo = new GameObject("ArenaGlow");
            var walkerGo = new GameObject("WalkerGlow");
            GameObject glowRoot = null;
            try
            {
                var arena = arenaGo.AddComponent<MazeArena>();
                arena.Width = 4;
                arena.Height = 4;
                arena.PaintPhotos = false;
                arena.Rebuild(2);

                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                var walker = walkerGo.AddComponent<MazeWalker>();
                walker.Arena = arena;
                walker.SetWalking(true);
                walker.PlaceAtEntry();

                glowRoot = GameObject.Find("FloorWaypoints");
                Assert.NotNull(glowRoot);
                Assert.NotNull(glowRoot.GetComponent<FloorPathGlow>());
                Assert.IsNull(glowRoot.GetComponent<MeshFilter>());
                var mesh = glowRoot.transform.Find("Mesh");
                Assert.NotNull(mesh);
                var filter = mesh.GetComponent<MeshFilter>();
                Assert.NotNull(filter);
                Assert.NotNull(filter.sharedMesh);
            }
            finally
            {
                Object.DestroyImmediate(walkerGo);
                Object.DestroyImmediate(arenaGo);
                if (glowRoot != null)
                    Object.DestroyImmediate(glowRoot);
            }
        }

        [Test]
        public void WaypointChipSitsOnTheFloor()
        {
            FloorWaypointMarker marker = null;
            try
            {
                marker = FloorWaypointMarker.Create(null, WaypointStyle.FloorTile);
                var disc = marker.transform.Find("Disc");
                Assert.NotNull(disc);
                Assert.AreEqual(FloorWaypointMarker.FloorLift, disc.localPosition.y, 0.001f);
                Assert.IsNull(marker.transform.Find("Head"));
                Assert.IsNull(marker.transform.Find("Pole"));
            }
            finally
            {
                if (marker != null)
                    Object.DestroyImmediate(marker.gameObject);
            }
        }

        [Test]
        public void SpaceBlobWaypointHasAStandingHead()
        {
            FloorWaypointMarker marker = null;
            try
            {
                marker = FloorWaypointMarker.Create(null, WaypointStyle.SpaceBlob);
                var head = marker.transform.Find("Head");
                Assert.NotNull(head);
                Assert.Greater(head.localPosition.y, 1.0f);
                Assert.NotNull(marker.transform.Find("Pole"));
            }
            finally
            {
                if (marker != null)
                    Object.DestroyImmediate(marker.gameObject);
            }
        }
    }
}
