using System.Linq;
using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Data;
using Game.Unity.Editor;
using Game.Unity.Graph;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class JourneyCatalogBuilderTests
    {
        [Test]
        public void TileArenaShipsTwentyFiveLevelsWithAlternatingVisuals()
        {
            var catalog = JourneyCatalogBuilder.BuildDefault();
            try
            {
                var tile = catalog.TileArena;
                Assert.That(tile.Count, Is.EqualTo(JourneyCatalogBuilder.TileArenaLevelCount));

                for (var i = 0; i < tile.Count; i++)
                {
                    var entry = tile.Levels[i];
                    Assert.That(entry.IsGrid, Is.True, "level " + (i + 1));
                    var mosaicLevel = i % 2 == 1;
                    Assert.That(
                        entry.VisualType,
                        Is.EqualTo(mosaicLevel ? ArenaVisualType.MosaicImage : ArenaVisualType.ClassicDanceFloor),
                        "level " + (i + 1));
                    if (mosaicLevel)
                    {
                        Assert.That(entry.MosaicTexture, Is.Not.Null, "level " + (i + 1));
                        Assert.That(entry.ThumbnailTexture, Is.SameAs(entry.MosaicTexture), "level " + (i + 1));
                    }
                    else
                    {
                        Assert.That(entry.MosaicTexture, Is.Null, "level " + (i + 1));
                        Assert.That(entry.ThumbnailTexture, Is.Null, "level " + (i + 1));
                    }
                }

                var mosaicLevels = Enumerable.Range(0, tile.Count)
                    .Where(i => i % 2 == 1)
                    .Select(i => tile.Levels[i])
                    .ToArray();
                Assert.That(mosaicLevels.Select(l => l.MosaicTexture).Distinct().Count(), Is.GreaterThan(1));
                Assert.That(mosaicLevels.Any(l => l.MosaicBackgroundParticles), Is.True);
                Assert.That(mosaicLevels.Any(l => !l.MosaicBackgroundParticles), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ScoutArenaUsesACloserConfigurableFollowZoom()
        {
            var catalog = JourneyCatalogBuilder.BuildDefault();
            try
            {
                var scout = catalog.ScoutArena;

                Assert.That(scout.CameraMode, Is.EqualTo(ArenaCameraMode.FollowWalker));
                Assert.That(scout.FollowOrthographicSize, Is.EqualTo(ScoutRotationMove.FollowSizeTwo).Within(0.01f));
                Assert.That(scout.ResolvedFollowOrthographicSize, Is.LessThan(3.2f));
                Assert.That(scout.Get(1).FollowOrthographicSize, Is.EqualTo(ScoutRotationMove.FollowSizeTwo).Within(0.01f));
                Assert.That(
                    ScoutRotationMove.FollowOrthographicSizeFor(2, 2),
                    Is.LessThan(ScoutRotationMove.FollowOrthographicSizeFor(3, 4)));
                Assert.That(scout.WalkerScale, Is.EqualTo(ScoutRotationMove.WalkerScale).Within(0.01f));
                Assert.That(scout.WalkerScaleFor(scout.Get(1)), Is.EqualTo(ScoutRotationMove.WalkerScale).Within(0.01f));
                Assert.That(scout.ScoutMoveMode, Is.EqualTo(ScoutMoveMode.RotationMoveMode));
                Assert.That(scout.Count, Is.EqualTo(LevelCatalog.ScoutSpecs.Count));
                Assert.That(scout.Get(1).IsGrid, Is.True);
                Assert.That(scout.Get(1).VisualType, Is.EqualTo(ArenaVisualType.PatchworkTiles));
                Assert.That(scout.Get(1).Grid.Width, Is.EqualTo(2));
                Assert.That(scout.Get(1).Grid.Height, Is.EqualTo(2));
                Assert.That(scout.Get(2).Grid.Width, Is.EqualTo(2));
                Assert.That(scout.Get(2).Grid.Height, Is.EqualTo(2));
                Assert.That(scout.Get(3).Grid.Width, Is.EqualTo(2));
                Assert.That(scout.Get(3).Grid.Height, Is.EqualTo(3));
                Assert.That(scout.Get(4).Grid.Width, Is.EqualTo(2));
                Assert.That(scout.Get(4).Grid.Height, Is.EqualTo(3));
                Assert.That(scout.Get(5).Grid.Width, Is.EqualTo(3));
                Assert.That(scout.Get(5).Grid.Height, Is.EqualTo(3));
                Assert.That(scout.Get(12).Grid.Width, Is.EqualTo(4));
                Assert.That(scout.Get(12).Grid.Height, Is.EqualTo(5));
                for (var i = 0; i < scout.Count; i++)
                {
                    Assert.That(scout.Levels[i].IsGrid, Is.True, "scout level " + (i + 1));
                    Assert.That(
                        scout.Levels[i].VisualType,
                        Is.EqualTo(ArenaVisualType.PatchworkTiles),
                        "scout level " + (i + 1));
                    Assert.That(scout.Levels[i].Kind, Is.EqualTo(JourneyBoardKind.Grid), "scout level " + (i + 1));
                }
                if (scout.Get(1).PatchworkSet != null)
                    Assert.That(scout.Get(1).PatchworkSet.name, Is.EqualTo("BuildingTiles"));
                Assert.That(
                    JourneyCatalog.PlayableGridSpec(GameModeId.ScoutArena, 0, default).Width,
                    Is.EqualTo(2));
                Assert.That(
                    JourneyCatalog.PlayableLevelCount(catalog, GameModeId.ScoutArena),
                    Is.EqualTo(LevelCatalog.ScoutSpecs.Count));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CreateNewAssetDoesNotOverwriteExistingCatalog()
        {
            var existing = JourneyCatalogBuilder.LoadExisting();
            Assume.That(existing, Is.Not.Null, "Project already has JourneyCatalog.asset");

            var created = JourneyCatalogBuilder.CreateNewAsset();
            var path = AssetDatabase.GetAssetPath(created);
            try
            {
                Assert.That(path, Is.Not.EqualTo(JourneyCatalogBuilder.CatalogPath));
                Assert.That(created.TileArena.Count, Is.EqualTo(JourneyCatalogBuilder.TileArenaLevelCount));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void TileArenaPlayUsesCoreEconomyEvenWhenAssetGridsAreStale()
        {
            var catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
            try
            {
                var stale = new JourneyLevelEntry
                {
                    Kind = JourneyBoardKind.Grid,
                    Grid = LevelRow.FromSpec(new LevelSpec(3, 1, 2, lengthSlack: 2, runs: 2, width: 3, height: 3))
                };
                catalog.ApplyModes(
                    new JourneyModeDefinition
                    {
                        ModeId = GameModeId.TileArena,
                        Levels = new[] { stale }
                    },
                    catalog.GraphArena,
                    catalog.ScoutArena);

                var play = catalog.CreateGridCatalog(GameModeId.TileArena);
                Assert.That(play.Get(1).Size.Width, Is.EqualTo(4));
                Assert.That(play.Get(1).Size.Height, Is.EqualTo(4));
                Assert.That(play.Get(1).Shape.MinTurns, Is.EqualTo(1));
                Assert.That(play.Get(1).Shape.MaxTurns, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ScoutPlayIgnoresGraphSlotsAndStaysOnCityPatchwork()
        {
            var catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
            var city = ScriptableObject.CreateInstance<PatchworkTextureSet>();
            var graph = GraphLevelDefinition.CreateSampleRuntime();
            city.name = "BuildingTiles";
            try
            {
                catalog.ApplyModes(
                    catalog.TileArena,
                    catalog.GraphArena,
                    new JourneyModeDefinition
                    {
                        ModeId = GameModeId.ScoutArena,
                        Levels = new[]
                        {
                            new JourneyLevelEntry
                            {
                                Kind = JourneyBoardKind.Grid,
                                VisualType = ArenaVisualType.PatchworkTiles,
                                PatchworkSet = city
                            },
                            new JourneyLevelEntry
                            {
                                Kind = JourneyBoardKind.Graph,
                                VisualType = ArenaVisualType.ClassicDanceFloor,
                                GraphLevel = graph
                            }
                        }
                    });

                Assert.That(
                    JourneyCatalog.PlayableLevelCount(catalog, GameModeId.ScoutArena),
                    Is.EqualTo(LevelCatalog.ScoutSpecs.Count));

                var five = catalog.PlayableEntry(GameModeId.ScoutArena, 5);
                Assert.That(five.IsGrid, Is.True);
                Assert.That(five.IsGraph, Is.False);
                Assert.That(five.VisualType, Is.EqualTo(ArenaVisualType.PatchworkTiles));
                Assert.That(five.PatchworkSet, Is.SameAs(city));
                Assert.That(five.Grid.Width, Is.EqualTo(3));
                Assert.That(five.Grid.Height, Is.EqualTo(3));
                Assert.That(five.WalkerScale, Is.EqualTo(ScoutRotationMove.WalkerScale).Within(0.01f));

                var twelve = catalog.PlayableEntry(GameModeId.ScoutArena, 12);
                Assert.That(twelve.IsGrid, Is.True);
                Assert.That(twelve.VisualType, Is.EqualTo(ArenaVisualType.PatchworkTiles));
                Assert.That(twelve.PatchworkSet, Is.SameAs(city));
                Assert.That(twelve.Grid.Width, Is.EqualTo(4));
                Assert.That(twelve.Grid.Height, Is.EqualTo(5));
            }
            finally
            {
                Object.DestroyImmediate(graph);
                Object.DestroyImmediate(city);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void GraphArenaPlayClonesTheRealCatalogArenaAndOnlyChangesTheRoute()
        {
            var catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
            var extracted = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                catalog.ApplyModes(
                    catalog.TileArena,
                    new JourneyModeDefinition
                    {
                        ModeId = GameModeId.GraphArena,
                        Levels = new[]
                        {
                            new JourneyLevelEntry { Kind = JourneyBoardKind.Graph, GraphLevel = extracted }
                        }
                    },
                    catalog.ScoutArena);

                Assert.That(JourneyCatalog.PlayableLevelCount(catalog, GameModeId.GraphArena),
                    Is.EqualTo(GraphLevelLadder.Count));

                var source = catalog.PlayableGraphSource(1);
                Assert.That(source, Is.SameAs(extracted));
                Assert.That(catalog.PlayableGraphSource(8), Is.SameAs(extracted));

                var play = GraphLevelDefinition.CreatePlayable(1, source);
                try
                {
                    Assert.That(play.Nodes.Count, Is.EqualTo(extracted.Nodes.Count));
                    Assert.That(play.StartNodeId, Is.EqualTo(extracted.StartNodeId));
                    Assert.That(play.GoalNodeId, Is.EqualTo(extracted.GoalNodeId));
                    Assert.That(play.PreviewKind, Is.EqualTo(PathPreviewKind.Radar));
                    Assert.That(play.PreviewSeconds, Is.GreaterThan(3f));
                }
                finally
                {
                    Object.DestroyImmediate(play);
                }

                var two = GraphLevelDefinition.CreatePlayable(2, catalog.PlayableGraphSource(2));
                try
                {
                    Assert.That(two.Nodes.Count, Is.EqualTo(extracted.Nodes.Count));
                    Assert.That(two.PreviewKind, Is.EqualTo(PathPreviewKind.Radar));
                }
                finally
                {
                    Object.DestroyImmediate(two);
                }

                var flash = GraphLevelDefinition.CreatePlayable(8, catalog.PlayableGraphSource(8));
                try
                {
                    Assert.That(flash.Nodes.Count, Is.EqualTo(extracted.Nodes.Count));
                    Assert.That(flash.PreviewKind, Is.EqualTo(PathPreviewKind.CameraFlash));
                    Assert.That(flash.PreviewSeconds, Is.GreaterThan(GraphPathPreviewSeconds.FlashLongest));
                }
                finally
                {
                    Object.DestroyImmediate(flash);
                }
            }
            finally
            {
                Object.DestroyImmediate(extracted);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ExtractedGraphLevel10StaysTheArenaWhenPlayableCopiesAreMade()
        {
            var arena = AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(
                "Assets/Game/Unity/Data/GraphLevels/GraphLevel-1 0.asset");
            Assume.That(arena, Is.Not.Null, "GraphLevel-1 0.asset must exist");
            Assert.That(arena.Nodes.Count, Is.GreaterThan(50));
            Assert.That(arena.StartNodeId, Is.EqualTo("n41"));
            Assert.That(arena.GoalNodeId, Is.EqualTo("n4"));

            for (var level = 1; level <= GraphLevelLadder.Count; level++)
            {
                var play = GraphLevelDefinition.CreatePlayable(level, arena);
                try
                {
                    Assert.That(play.Nodes.Count, Is.EqualTo(arena.Nodes.Count), "level " + level);
                    Assert.That(play.StartNodeId, Is.EqualTo("n41"), "level " + level);
                    Assert.That(play.GoalNodeId, Is.EqualTo("n4"), "level " + level);
                    Assert.That(play.Background, Is.EqualTo(arena.Background), "level " + level);
                    var run = GraphBoardPresenter.CreateRun(play, seed: 11 + level);
                    var spec = GraphLevelLadder.For(level);
                    Assert.That(run.Path.Nodes.Count, Is.GreaterThanOrEqualTo(spec.MinPath), "level " + level);
                    Assert.That(run.Path.Nodes.Count, Is.LessThanOrEqualTo(spec.MaxPath), "level " + level);
                }
                finally
                {
                    Object.DestroyImmediate(play);
                }
            }
        }

        [Test]
        public void GraphArenaSampleDefaultsToCameraFlashAndCreateRunWorks()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                Assert.That(level.PreviewKind, Is.EqualTo(PathPreviewKind.CameraFlash));
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                Assert.That(run.Path.Nodes.Count, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }
    }
}
