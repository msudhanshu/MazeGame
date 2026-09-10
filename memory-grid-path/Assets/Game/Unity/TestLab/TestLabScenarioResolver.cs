using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.TestLab
{
    /// <summary>
    /// Maps each test-lab scenario to a tile factory, camera mode, and board spacing.
    /// </summary>
    public static class TestLabScenarioResolver
    {
        public readonly struct Profile
        {
            public Profile(
                ITileViewFactory factory,
                ArenaCameraMode cameraMode,
                float tileSize,
                float tileGap,
                float followOrthographicSize,
                float followSmoothing,
                string title,
                string hint)
            {
                Factory = factory;
                CameraMode = cameraMode;
                TileSize = tileSize;
                TileGap = tileGap;
                FollowOrthographicSize = followOrthographicSize;
                FollowSmoothing = followSmoothing;
                Title = title;
                Hint = hint;
            }

            public ITileViewFactory Factory { get; }
            public ArenaCameraMode CameraMode { get; }
            public float TileSize { get; }
            public float TileGap { get; }
            public float FollowOrthographicSize { get; }
            public float FollowSmoothing { get; }
            public string Title { get; }
            public string Hint { get; }
        }

        public static Profile Resolve(
            TestLabScenario scenario,
            ArenaVisualSettings settings,
            Texture2D proceduralMosaic,
            Texture2D[] proceduralPatchwork,
            PatchworkTextureSet patchworkSetOverride = null)
        {
            settings ??= ScriptableObject.CreateInstance<ArenaVisualSettings>();

            switch (scenario)
            {
                case TestLabScenario.ClassicColorPath:
                    return new Profile(
                        new DanceFloorTileViewFactory(),
                        ArenaCameraMode.StaticTopDown,
                        settings.ClassicTileSize,
                        settings.ClassicGap,
                        settings.FollowOrthographicSize,
                        settings.FollowSmoothing,
                        "1 — Classic colour path",
                        "Coloured tiles with thin borders. Walk the hidden path.");

                case TestLabScenario.MosaicCoordinateDebug:
                {
                    var mosaic = settings.MosaicTexture != null ? settings.MosaicTexture : proceduralMosaic;
                    return new Profile(
                        new MosaicDebugTileViewFactory(settings, mosaic),
                        ArenaCameraMode.StaticTopDown,
                        settings.SeamlessTileSize,
                        settings.TileGap,
                        settings.FollowOrthographicSize,
                        settings.FollowSmoothing,
                        "2 — Mosaic + coordinates",
                        "Photograph under glass tiles. Frosted covers, clear walked panes, coord labels.");
                }

                case TestLabScenario.PatchworkFollowWalker:
                {
                    ArenaVisualSettings patchworkSettings;
                    if (patchworkSetOverride != null && patchworkSetOverride.HasTextures)
                    {
                        patchworkSettings = ArenaVisualSettings.CreatePatchworkOverride(
                            patchworkSetOverride,
                            settings.PatchworkSeed);
                    }
                    else if (settings.HasPatchworkTextures)
                    {
                        patchworkSettings = settings;
                    }
                    else
                    {
                        patchworkSettings = ArenaVisualSettings.CreatePatchworkOverride(
                            proceduralPatchwork,
                            settings.PatchworkSeed);
                    }

                    return new Profile(
                        new PatchworkArenaTileViewFactory(patchworkSettings),
                        ArenaCameraMode.FollowWalker,
                        settings.SeamlessTileSize,
                        0f,
                        settings.FollowOrthographicSize,
                        settings.FollowSmoothing,
                        "3 — Patchwork + follow camera",
                        "Random texture per tile, seamless floor. Camera tracks the walker.");
                }

                case TestLabScenario.GraphNodeArena:
                default:
                    return new Profile(
                        null,
                        ArenaCameraMode.StaticTopDown,
                        0f,
                        0f,
                        settings.FollowOrthographicSize,
                        settings.FollowSmoothing,
                        "4 — Graph node arena",
                        "Junction graph on a background image. Click nodes or press 1–6.");
            }
        }
    }
}
