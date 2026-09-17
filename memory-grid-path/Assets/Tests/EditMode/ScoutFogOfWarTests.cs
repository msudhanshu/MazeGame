using Game.Unity.Data;
using Game.Unity.Graph;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class ScoutFogOfWarTests
    {
        [Test]
        public void PaddingShaderCompiles()
        {
            var shader = Resources.Load<Shader>(ScoutFogOfWar.PaddingShaderPath)
                         ?? Shader.Find("Nixin Studio/ScoutFogPadding");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            Assert.That(shader.name, Does.Not.Contain("InternalError"));
        }

        [Test]
        public void PaddingIsClearInTheArenaAndOpaqueOutside()
        {
            var origin = Vector3.zero;
            const float width = 4f;
            const float depth = 3f;

            Assert.That(ScoutFogOfWar.PaddingAlpha(origin, origin, width, depth), Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                ScoutFogOfWar.PaddingAlpha(new Vector3(width * 0.5f - 0.05f, 0f, 0f), origin, width, depth),
                Is.EqualTo(0f).Within(0.01f));
            Assert.That(
                ScoutFogOfWar.PaddingAlpha(new Vector3(width, 0f, depth), origin, width, depth),
                Is.EqualTo(1f).Within(0.01f));
            Assert.That(
                ScoutFogOfWar.PaddingAlpha(new Vector3(width * 0.5f + 0.3f, 0f, 0f), origin, width, depth),
                Is.GreaterThan(0.4f));
        }

        [Test]
        public void BuildPlacesPaddingWithoutCoveringTheArena()
        {
            var host = new GameObject("ScoutFogHost");
            try
            {
                var layout = new BoardLayout(new GridSize(4, 3), 1f, 0f, Vector3.zero);
                ScoutFogOfWar.Build(layout, host.transform);

                var padding = host.transform.Find(ScoutFogOfWar.PaddingName);
                Assert.That(padding, Is.Not.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
                Assert.That(padding.GetComponent<Collider>(), Is.Null);

                Assert.That(padding.position.y, Is.EqualTo(ScoutFogOfWar.PaddingLift).Within(0.001f));
                Assert.That(padding.position.y, Is.LessThan(GridPathOverlay.Lift));

                var paddingRenderer = padding.GetComponent<Renderer>()
                    ?? padding.GetComponentInChildren<Renderer>();
                Assert.That(paddingRenderer, Is.Not.Null);
                Assert.That(paddingRenderer.bounds.size.x, Is.GreaterThan(layout.SurfaceWidth * 4f));

                var paddingMaterial = paddingRenderer.sharedMaterial;
                Assert.That(paddingMaterial.mainTexture, Is.Not.Null);
                Assert.That(paddingMaterial.shader.name, Does.Not.Contain("InternalError"));
                if (paddingMaterial.HasProperty("_EdgeOverlap"))
                    Assert.That(paddingMaterial.GetFloat("_EdgeOverlap"), Is.EqualTo(ScoutFogOfWar.EdgeOverlap).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GraphArenaUsesFogAndFitsTheWholePhoto()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            var host = new GameObject("GraphFogHost");
            var cameraGo = new GameObject("GraphFogCam", typeof(Camera));
            try
            {
                var camera = cameraGo.GetComponent<Camera>();
                camera.aspect = 16f / 9f;
                var layout = new GraphBoardLayout(level, Vector3.zero);
                var factory = new GraphNodeCircleViewFactory { FogOfWar = true };
                factory.ApplyEnvironment(camera, layout, host.transform);

                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Not.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
                Assert.That(host.transform.Find(PatchworkOceanBackdrop.OceanName), Is.Null);
                Assert.That(camera.backgroundColor, Is.EqualTo(ScoutFogOfWar.Background));
                Assert.That(camera.orthographicSize * 2f, Is.GreaterThanOrEqualTo(layout.WorldDepth - 0.001f));
                Assert.That(
                    camera.orthographicSize * 2f * camera.aspect,
                    Is.GreaterThanOrEqualTo(layout.WorldWidth - 0.001f));
                var bottomZ = layout.Origin.z - layout.WorldDepth * 0.5f;
                Assert.That(
                    BoardCamera.ViewportY(bottomZ, camera.transform.position.z, camera.orthographicSize),
                    Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ScoutGraphUsesFogAroundThePhoto()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            var host = new GameObject("ScoutGraphHost");
            try
            {
                var layout = new GraphBoardLayout(level, Vector3.zero);
                var factory = new GraphNodeCircleViewFactory { OceanBackdrop = true };
                factory.ApplyEnvironment(null, layout, host.transform);

                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Not.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
                Assert.That(host.transform.Find(PatchworkOceanBackdrop.OceanName), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void TileArenaDanceFloorHasNoScoutFog()
        {
            var host = new GameObject("DanceFogHost");
            try
            {
                new DanceFloorTileViewFactory().ApplyEnvironment(
                    null,
                    new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                    host.transform);

                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
                Assert.That(host.transform.Find(ArenaEnvironment.RoomFloorName), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void PatchworkScoutEnvironmentUsesFogAroundTheTiles()
        {
            var host = new GameObject("PatchworkFogHost");
            try
            {
                var settings = ArenaVisualSettings.CreatePatchworkOverride(new[] { Texture2D.whiteTexture });
                var factory = new PatchworkArenaTileViewFactory(settings);
                factory.ApplyEnvironment(
                    null,
                    new BoardLayout(new GridSize(4, 3), 1f, 0f, Vector3.zero),
                    host.transform);

                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Not.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
                Assert.That(host.transform.Find(PatchworkOceanBackdrop.OceanName), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void FogDoesNotStealGraphPicks()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            var host = new GameObject("FogPickHost");
            try
            {
                var layout = new GraphBoardLayout(level, Vector3.zero);
                new GraphNodeCircleViewFactory { FogOfWar = true }.ApplyEnvironment(null, layout, host.transform);

                var padding = host.transform.Find(ScoutFogOfWar.PaddingName);
                Assert.That(padding, Is.Not.Null);
                Assert.That(padding.GetComponent<Collider>(), Is.Null);
                Assert.That(padding.GetComponentInChildren<Collider>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ClearingTheEnvironmentRemovesFog()
        {
            var host = new GameObject("FogClearHost");
            try
            {
                ScoutFogOfWar.Build(
                    new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                    host.transform);
                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Not.Null);

                ArenaEnvironment.Clear(host.transform);

                Assert.That(host.transform.Find(ScoutFogOfWar.PaddingName), Is.Null);
                Assert.That(host.transform.Find(ScoutFogOfWar.OverlayName), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
