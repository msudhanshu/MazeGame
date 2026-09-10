using Game.Core;
using NUnit.Framework;
using Nixin.Locomotion;
using Nixin.Maze;
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
    }
}
