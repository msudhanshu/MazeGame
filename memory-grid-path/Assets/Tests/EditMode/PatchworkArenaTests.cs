using Game.Unity.Data;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class PatchworkArenaTests
    {
        [Test]
        public void ApplyEnvironmentPlacesOceanUnderAndAroundTheTiles()
        {
            var host = new GameObject("PatchworkHost");
            try
            {
                var settings = ArenaVisualSettings.CreatePatchworkOverride(new[] { Texture2D.whiteTexture });
                var factory = new PatchworkArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(4, 3), 1f, settings.TileGap, Vector3.zero);

                factory.ApplyEnvironment(null, layout, host.transform);

                var ocean = host.transform.Find(PatchworkOceanBackdrop.OceanName);
                Assert.That(ocean, Is.Not.Null);
                Assert.That(ocean.position.y, Is.EqualTo(layout.Origin.y - PatchworkOceanBackdrop.OceanDepth).Within(0.001f));
                Assert.That(ocean.position.y, Is.LessThan(0f));
                Assert.That(host.transform.Find(ArenaEnvironment.RoomFloorName), Is.Null);

                var worldWidth = ocean.localScale.x * 10f;
                var worldDepth = ocean.localScale.z * 10f;
                Assert.That(worldWidth, Is.GreaterThan(layout.SurfaceWidth * 4f));
                Assert.That(worldDepth, Is.GreaterThan(layout.SurfaceDepth * 4f));

                var oceanRenderer = ocean.GetComponent<Renderer>();
                Assert.That(oceanRenderer, Is.Not.Null);
                Assert.That(oceanRenderer.sharedMaterial, Is.Not.Null);
                var shaderName = oceanRenderer.sharedMaterial.shader.name;
                Assert.That(
                    shaderName.Contains("ProceduralWater") || shaderName.Contains("OceanWater"),
                    Is.True,
                    shaderName);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CameraBackgroundMatchesTheOcean()
        {
            var host = new GameObject("PatchworkHost");
            var cameraGo = new GameObject("PatchworkCam", typeof(Camera));
            try
            {
                var camera = cameraGo.GetComponent<Camera>();
                var settings = ArenaVisualSettings.CreatePatchworkOverride(new[] { Texture2D.whiteTexture });
                var factory = new PatchworkArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero);

                factory.ApplyEnvironment(camera, layout, host.transform);

                Assert.That(camera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
                Assert.That(camera.backgroundColor, Is.EqualTo(PatchworkOceanBackdrop.Background));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void SwitchingThemesClearsTheOcean()
        {
            var host = new GameObject("PatchworkHost");
            try
            {
                var settings = ArenaVisualSettings.CreatePatchworkOverride(new[] { Texture2D.whiteTexture });
                new PatchworkArenaTileViewFactory(settings).ApplyEnvironment(
                    null,
                    new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                    host.transform);
                Assert.That(host.transform.Find(PatchworkOceanBackdrop.OceanName), Is.Not.Null);

                ArenaEnvironment.Clear(host.transform);

                Assert.That(host.transform.Find(PatchworkOceanBackdrop.OceanName), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ScoutTilesUseAPlainUnlitTextureWithNoGlow()
        {
            var host = new GameObject("PatchworkHost");
            try
            {
                var settings = ArenaVisualSettings.CreatePatchworkOverride(new[] { Texture2D.whiteTexture });
                var factory = new PatchworkArenaTileViewFactory(settings);
                var view = factory.CreateTile(
                    new GridCoord(0, 0),
                    new GridSize(3, 3),
                    Vector3.zero,
                    settings.SeamlessTileSize,
                    host.transform);

                var tileRenderer = host.GetComponentInChildren<Renderer>();
                Assert.That(tileRenderer, Is.Not.Null);
                Assert.That(tileRenderer.sharedMaterial.shader.name, Does.Contain("UnlitTexture"));
                Assert.That(tileRenderer.sharedMaterial.shader.name, Does.Not.Contain("Glow"));
                Assert.That(tileRenderer.sharedMaterial.HasProperty("_Intensity"), Is.False);

                view.SetState(TileVisualState.Walked);
                Assert.That(view.State, Is.EqualTo(TileVisualState.Walked));
                Assert.That(tileRenderer.sharedMaterial.shader.name, Does.Contain("UnlitTexture"));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void JourneyFollowZoomIsConfigurableAndCloserByDefault()
        {
            var entry = new JourneyLevelEntry
            {
                VisualType = ArenaVisualType.PatchworkTiles
            };

            var defaults = JourneyVisualResolver.Resolve(entry, ArenaCameraMode.FollowWalker, null);
            Assert.That(defaults.FollowOrthographicSize, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(defaults.FollowOrthographicSize, Is.LessThan(3.2f));

            var close = JourneyVisualResolver.Resolve(
                entry,
                ArenaCameraMode.FollowWalker,
                null,
                followOrthographicSize: 1.2f);
            Assert.That(close.FollowOrthographicSize, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(close.CameraMode, Is.EqualTo(ArenaCameraMode.FollowWalker));
        }

        [Test]
        public void ALevelCanOverrideModeZoom()
        {
            var mode = new JourneyModeDefinition
            {
                CameraMode = ArenaCameraMode.FollowWalker,
                FollowOrthographicSize = 1.8f
            };
            var inherit = new JourneyLevelEntry { VisualType = ArenaVisualType.PatchworkTiles };
            var overrideLevel = new JourneyLevelEntry
            {
                VisualType = ArenaVisualType.PatchworkTiles,
                FollowOrthographicSize = 1.1f
            };

            Assert.That(mode.FollowSizeFor(inherit), Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(mode.LocksOrthographicSize(inherit), Is.False);
            Assert.That(mode.FollowSizeFor(overrideLevel), Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(mode.LocksOrthographicSize(overrideLevel), Is.True);
        }
    }
}
