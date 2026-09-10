using Game.Core;
using NUnit.Framework;
using Nixin.Locomotion;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public class MazeGameHostTests
    {
        [Test]
        public void ApplyWalkSettingsCopiesHostFieldsOntoWalkerControls()
        {
            var hostGo = new GameObject("MazeGameHostTest");
            var walkerGo = new GameObject("WalkerHostTest");
            try
            {
                walkerGo.AddComponent<CharacterController>();
                walkerGo.AddComponent<FirstPersonController>();
                var walker = walkerGo.AddComponent<MazeWalker>();
                var host = hostGo.AddComponent<MazeGameHost>();
                host.Walker = walker;
                host.Scheme = ArenaControlScheme.DualSticks;
                host.LookStick = LookStickMode.FixedBottom;
                host.WaypointStyle = WaypointStyle.SpaceBlob;
                host.IntermediateWaypoint = IntermediateWaypointMode.Remove;
                host.RailMoveSpeed = 2.4f;
                host.JoystickMoveSpeed = 0.55f;
                host.LookDegreesPerSecond = 90f;
                host.LimitLookYaw = false;
                host.AllowLookDown = false;
                host.DefaultLookDown = 12f;
                host.MaxLookDown = 40f;
                host.SwipeToStop = false;
                host.KeyboardWasd = false;

                host.ApplyWalkSettings();

                Assert.AreEqual(ArenaControlScheme.DualSticks, walker.Controls.Scheme);
                Assert.AreEqual(LookStickMode.FixedBottom, walker.Controls.LookStick);
                Assert.AreEqual(WaypointStyle.SpaceBlob, walker.Controls.WaypointStyle);
                Assert.AreEqual(IntermediateWaypointMode.Remove, walker.Controls.IntermediateWaypoint);
                Assert.AreEqual(2.4f, walker.Controls.RailMoveSpeed, 0.0001f);
                Assert.AreEqual(0.55f, walker.Controls.JoystickMoveSpeed, 0.0001f);
                Assert.AreEqual(90f, walker.Controls.LookDegreesPerSecond, 0.0001f);
                Assert.IsFalse(walker.Controls.LimitLookYaw);
                Assert.IsFalse(walker.Controls.AllowLookDown);
                Assert.AreEqual(12f, walker.Controls.DefaultLookDown, 0.0001f);
                Assert.AreEqual(40f, walker.Controls.MaxLookDown, 0.0001f);
                Assert.IsFalse(walker.Controls.SwipeToStop);
                Assert.IsFalse(walker.Controls.KeyboardWasd);
            }
            finally
            {
                Object.DestroyImmediate(walkerGo);
                Object.DestroyImmediate(hostGo);
            }
        }

        [Test]
        public void WireCreatesArenaHudWalkerAndRun()
        {
            var hostGo = new GameObject("MazeGameHostWireTest");
            var before = SnapshotRoots(hostGo.scene);
            before.Add(hostGo);
            MazeGameHost host = null;
            try
            {
                host = hostGo.AddComponent<MazeGameHost>();
                host.ShowDebugTools = false;
                host.Wire();

                Assert.NotNull(host.Arena);
                Assert.NotNull(host.Hud);
                Assert.NotNull(host.Walker);
                Assert.NotNull(host.Run);
                Assert.NotNull(host.Memory);
                Assert.AreEqual(host.Scheme, host.Walker.Controls.Scheme);
                Assert.IsFalse(host.Hud.AllowDebugTools);
                Assert.AreEqual(hostGo.scene, host.Arena.gameObject.scene);
            }
            finally
            {
                DestroyCreatedRoots(hostGo.scene, before);
                if (hostGo != null)
                    Object.DestroyImmediate(hostGo);
            }
        }

        static System.Collections.Generic.HashSet<GameObject> SnapshotRoots(UnityEngine.SceneManagement.Scene scene)
        {
            var ids = new System.Collections.Generic.HashSet<GameObject>();
            if (!scene.IsValid())
                return ids;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                ids.Add(roots[i]);
            return ids;
        }

        static void DestroyCreatedRoots(UnityEngine.SceneManagement.Scene scene, System.Collections.Generic.HashSet<GameObject> before)
        {
            if (!scene.IsValid())
                return;
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (!before.Contains(roots[i]))
                    Object.DestroyImmediate(roots[i]);
            }
        }
    }
}
