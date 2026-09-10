using Game.Unity.Data;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class MosaicArenaTests
    {
        [Test]
        public void IdleGlassIsClearAndThePathFrostsWhite()
        {
            var idle = MosaicGlassLook.For(TileVisualState.Idle);
            var walked = MosaicGlassLook.For(TileVisualState.Walked);
            var lighthouse = MosaicGlassLook.For(TileVisualState.Lighthouse);
            var revealed = MosaicGlassLook.For(TileVisualState.Revealed);
            var failed = MosaicGlassLook.For(TileVisualState.Wrong);

            Assert.That(idle.Alpha, Is.LessThan(0.2f));
            Assert.That(idle.Frost, Is.LessThan(0.2f));
            Assert.That(revealed.Alpha, Is.LessThan(0.2f));
            Assert.That(walked.Tint, Is.EqualTo(Color.white));
            Assert.That(walked.Frost, Is.GreaterThan(0.7f));
            Assert.That(walked.Alpha, Is.GreaterThan(idle.Alpha));
            Assert.That(lighthouse.Frost, Is.EqualTo(walked.Frost));
            Assert.That(failed.Tint.r, Is.GreaterThan(failed.Tint.g));
            Assert.That(failed.TintStrength, Is.GreaterThan(0.9f));
            Assert.That(failed.Alpha, Is.GreaterThan(0.85f));
        }

        [Test]
        public void MosaicTilesSitFlushWithParticlesOffByDefault()
        {
            var settings = ArenaVisualSettings.CreateMosaicOverride(Texture2D.whiteTexture);

            Assert.That(settings.TileGap, Is.EqualTo(0.02f));
            Assert.That(settings.MosaicBackgroundParticles, Is.False);
            Assert.That(settings.MosaicVfxPrefab, Is.Null);
        }

        [Test]
        public void ApplyEnvironmentPlacesThePhotographUnderTheTiles()
        {
            var host = new GameObject("MosaicHost");
            try
            {
                var settings = ArenaVisualSettings.CreateMosaicOverride(
                    Texture2D.whiteTexture,
                    backgroundParticles: true);
                var factory = new MosaicArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(3, 3), 1f, settings.TileGap, Vector3.zero);

                factory.ApplyEnvironment(null, layout, host.transform);

                var photo = host.transform.Find(MosaicArenaBackdrop.PhotoName);
                Assert.That(photo, Is.Not.Null);
                var box = MosaicParticleBox.ForBoard(layout);
                Assert.That(photo.position.y, Is.EqualTo(box.Min.y).Within(0.001f));
                var floor = host.transform.Find(ArenaEnvironment.RoomFloorName);
                Assert.That(floor, Is.Not.Null);
                Assert.That(floor.GetComponent<Renderer>().sharedMaterial.shader.name, Does.Contain("UnlitColor"));
                Assert.That(floor.position.y, Is.EqualTo(layout.Origin.y - ArenaEnvironment.RoomFloorDepth).Within(0.001f));
                Assert.That(floor.position.y, Is.LessThan(photo.position.y - 4f));
                var dust = host.transform.Find(MosaicArenaBackdrop.DustName);
                Assert.That(dust, Is.Not.Null);
                Assert.That(dust.position.y, Is.EqualTo(box.Center.y).Within(0.001f));
                Assert.That(photo.position.y, Is.LessThan(dust.position.y));
                Assert.That(dust.position.y, Is.LessThan(0f));
                Assert.That(box.Max.y, Is.LessThan(0f));
                Assert.That(photo.localScale.x, Is.EqualTo(layout.SurfaceWidth).Within(0.001f));
                Assert.That(photo.localScale.y, Is.EqualTo(layout.SurfaceDepth).Within(0.001f));
                var cage = host.GetComponentInChildren<MosaicParticleCage>();
                Assert.That(cage, Is.Not.Null);
                Assert.That(cage.Extents.x, Is.EqualTo(box.Extents.x).Within(0.001f));
                Assert.That(cage.Extents.z, Is.EqualTo(box.Extents.z).Within(0.001f));
                Assert.That(host.GetComponentsInChildren<ParticleSystem>().Length, Is.GreaterThanOrEqualTo(4));
                var vfx = host.GetComponentInChildren<ParticleSystemRenderer>();
                Assert.That(vfx, Is.Not.Null);
                Assert.That(vfx.sharedMaterial, Is.Not.Null);
                if (vfx.sharedMaterial.HasProperty("_ClipExtents"))
                {
                    Assert.That(vfx.sharedMaterial.GetVector("_ClipExtents").x, Is.EqualTo(box.Extents.x).Within(0.001f));
                    Assert.That(vfx.sharedMaterial.GetVector("_ClipCenter").y, Is.EqualTo(box.Center.y).Within(0.001f));
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ParticlesCanBeTurnedOff()
        {
            var host = new GameObject("MosaicHost");
            try
            {
                var settings = ArenaVisualSettings.CreateMosaicOverride(Texture2D.whiteTexture);
                settings.SetMosaicBackgroundParticles(false);
                var factory = new MosaicArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(2, 2), 1f, settings.TileGap, Vector3.zero);

                factory.ApplyEnvironment(null, layout, host.transform);

                Assert.That(host.transform.Find(MosaicArenaBackdrop.PhotoName), Is.Not.Null);
                Assert.That(host.transform.Find(MosaicArenaBackdrop.DustName), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MosaicTilesStartCoveredAndShareTheGlassShader()
        {
            var host = new GameObject("MosaicHost");
            try
            {
                var settings = ArenaVisualSettings.CreateMosaicOverride(Texture2D.whiteTexture);
                var factory = new MosaicArenaTileViewFactory(settings);
                var view = factory.CreateTile(
                    new GridCoord(1, 1),
                    new GridSize(3, 3),
                    Vector3.zero,
                    settings.SeamlessTileSize,
                    host.transform);

                Assert.That(view.State, Is.EqualTo(TileVisualState.Idle));
                var tileRenderer = host.GetComponentInChildren<Renderer>();
                Assert.That(tileRenderer, Is.Not.Null);
                Assert.That(tileRenderer.sharedMaterial, Is.Not.Null);
                Assert.That(tileRenderer.sharedMaterial.name, Does.Contain("MosaicGlass"));
                Assert.That(tileRenderer.sharedMaterial.shader.name, Does.Contain("MosaicGlass"));
                Assert.That(tileRenderer.sharedMaterial.HasProperty("_Tint"), Is.True);
                Assert.That(tileRenderer.sharedMaterial.HasProperty("_SeamWidth"), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void NeighbouringGlassTilesTouch()
        {
            var host = new GameObject("MosaicHost");
            try
            {
                var settings = ArenaVisualSettings.CreateMosaicOverride(Texture2D.whiteTexture);
                var factory = new MosaicArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(3, 3), settings.SeamlessTileSize, settings.TileGap, Vector3.zero);
                var a = layout.WorldPosition(new GridCoord(0, 1));
                var b = layout.WorldPosition(new GridCoord(1, 1));

                factory.CreateTile(new GridCoord(0, 1), layout.Size, a, settings.SeamlessTileSize, host.transform);
                factory.CreateTile(new GridCoord(1, 1), layout.Size, b, settings.SeamlessTileSize, host.transform);

                var gapBetweenEdges = layout.Pitch - settings.SeamlessTileSize;
                Assert.That(gapBetweenEdges, Is.EqualTo(settings.TileGap).Within(0.0001f));
                Assert.That(Vector3.Distance(a, b), Is.EqualTo(settings.SeamlessTileSize + settings.TileGap).Within(0.001f));
                Assert.That(host.transform.childCount, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void ParticleBoxStaysInsideTheGridFootprintAndBelowTheTiles()
        {
            var layout = new BoardLayout(new GridSize(4, 3), 1f, 0.08f, Vector3.zero);
            var box = MosaicParticleBox.ForBoard(layout);

            Assert.That(box.Max.y, Is.LessThan(0f));
            Assert.That(box.Max.x, Is.LessThan(layout.SurfaceWidth * 0.5f));
            Assert.That(box.Max.z, Is.LessThan(layout.SurfaceDepth * 0.5f));
            Assert.That(box.Contains(box.Center), Is.True);

            var outside = box.Max + Vector3.right;
            var velocity = Vector3.right;
            box.Confine(ref outside, ref velocity);
            Assert.That(box.Contains(outside), Is.True);
            Assert.That(velocity.x, Is.LessThan(0f));
        }

        [Test]
        public void ParticleCageKeepsEmittedParticlesInsideTheBox()
        {
            var host = new GameObject("MosaicHost");
            try
            {
                var settings = ArenaVisualSettings.CreateMosaicOverride(
                    Texture2D.whiteTexture,
                    backgroundParticles: true);
                var factory = new MosaicArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(3, 3), 1f, settings.TileGap, Vector3.zero);
                factory.ApplyEnvironment(null, layout, host.transform);

                var cage = host.GetComponentInChildren<MosaicParticleCage>();
                var system = host.GetComponentInChildren<ParticleSystem>();
                Assert.That(cage, Is.Not.Null);
                Assert.That(system, Is.Not.Null);

                system.Clear(true);
                system.Emit(1);
                var particles = new ParticleSystem.Particle[8];
                Assert.That(system.GetParticles(particles), Is.EqualTo(1));
                particles[0].position = new Vector3(50f, 20f, -40f);
                particles[0].velocity = Vector3.one;
                system.SetParticles(particles, 1);

                Assert.That(cage.ConfineAll(), Is.EqualTo(1));
                Assert.That(system.GetParticles(particles), Is.EqualTo(1));
                Assert.That(cage.ContainsLocal(particles[0].position), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void JourneyMosaicLevelUsesTheDraggedVfxPrefab()
        {
            var prefab = new GameObject("LevelStarVfx");
            prefab.AddComponent<ParticleSystem>();
            var host = new GameObject("MosaicHost");
            try
            {
                var entry = new JourneyLevelEntry
                {
                    VisualType = ArenaVisualType.MosaicImage,
                    MosaicTexture = Texture2D.whiteTexture,
                    MosaicVfxPrefab = prefab
                };
                var settings = JourneyVisualResolver.Resolve(entry, ArenaCameraMode.StaticTopDown, null);
                Assert.That(settings.MosaicVfxPrefab, Is.EqualTo(prefab));

                var factory = new MosaicArenaTileViewFactory(settings);
                var layout = new BoardLayout(new GridSize(2, 2), 1f, settings.TileGap, Vector3.zero);
                factory.ApplyEnvironment(null, layout, host.transform);

                var dust = host.transform.Find(MosaicArenaBackdrop.DustName);
                Assert.That(dust, Is.Not.Null);
                var placed = dust.Find("LevelStarVfx");
                Assert.That(placed, Is.Not.Null);
                Assert.That(placed.localScale, Is.EqualTo(MosaicParticleBox.ForBoard(layout).Size));
                Assert.That(placed.GetComponent<MosaicBoxedVfx>(), Is.Not.Null);
                Assert.That(host.GetComponentsInChildren<ParticleSystem>().Length, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(prefab);
            }
        }

        [Test]
        public void JourneyMosaicLevelCanTurnBuiltInParticlesOn()
        {
            var entry = new JourneyLevelEntry
            {
                VisualType = ArenaVisualType.MosaicImage,
                MosaicTexture = Texture2D.whiteTexture,
                MosaicBackgroundParticles = true
            };

            var settings = JourneyVisualResolver.Resolve(entry, ArenaCameraMode.StaticTopDown, null);

            Assert.That(settings.MosaicBackgroundParticles, Is.True);
            Assert.That(settings.MosaicVfxPrefab, Is.Null);
        }

        [Test]
        public void FireworkFitScaleIsUniformAndFitsInsideTheBoard()
        {
            var layout = new BoardLayout(new GridSize(4, 3), 1f, 0f, Vector3.zero);
            var box = MosaicParticleBox.ForBoard(layout);
            var scale = MosaicBoxedVfx.UniformScale(box);

            Assert.That(scale.x, Is.EqualTo(scale.y).Within(0.0001f));
            Assert.That(scale.y, Is.EqualTo(scale.z).Within(0.0001f));
            Assert.That(scale.x * MosaicBoxedVfx.SourceSpan, Is.LessThanOrEqualTo(Mathf.Min(box.Size.x, box.Size.z) + 0.001f));
            Assert.That(scale.y, Is.Not.EqualTo(box.Size.y).Within(0.001f));
        }

        [Test]
        public void ClassicDanceFloorHasNoMosaicParticles()
        {
            var host = new GameObject("ClassicHost");
            try
            {
                var mosaic = ArenaVisualSettings.CreateMosaicOverride(
                    Texture2D.whiteTexture,
                    backgroundParticles: true);
                new MosaicArenaTileViewFactory(mosaic).ApplyEnvironment(
                    null,
                    new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                    host.transform);
                Assert.That(host.transform.Find(MosaicArenaBackdrop.DustName), Is.Not.Null);

                new DanceFloorTileViewFactory().ApplyEnvironment(
                    null,
                    new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                    host.transform);

                Assert.That(host.transform.Find(MosaicArenaBackdrop.DustName), Is.Null);
                Assert.That(host.transform.Find(MosaicArenaBackdrop.PhotoName), Is.Null);
                Assert.That(host.GetComponentsInChildren<ParticleSystem>().Length, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
