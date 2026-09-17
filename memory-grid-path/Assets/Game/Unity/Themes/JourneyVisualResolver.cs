using Game.Unity.Data;
using Game.Unity.Themes.Experimental;
using UnityEngine;

namespace Game.Unity.Themes
{
    public static class JourneyVisualResolver
    {
        public static ArenaVisualSettings Resolve(
            JourneyLevelEntry entry,
            ArenaCameraMode cameraMode,
            Texture2D fallbackMosaic,
            float followOrthographicSize = 1.8f,
            float followSmoothing = 10f,
            bool lockOrthographicSize = false,
            ScoutMoveMode scoutMoveMode = ScoutMoveMode.PanMoveMode)
        {
            if (entry == null)
            {
                return WithScoutMove(
                    ArenaVisualSettings.CreateClassicOverride(
                        cameraMode,
                        followOrthographicSize,
                        followSmoothing,
                        lockOrthographicSize),
                    scoutMoveMode);
            }

            switch (entry.VisualType)
            {
                case ArenaVisualType.MosaicImage:
                    var mosaic = ArenaVisualSettings.CreateMosaicOverride(
                        entry.MosaicTexture != null ? entry.MosaicTexture : fallbackMosaic,
                        cameraMode,
                        entry.MosaicVfxPrefab,
                        entry.MosaicBackgroundParticles);
                    mosaic.ApplyCamera(cameraMode, followOrthographicSize, followSmoothing, lockOrthographicSize);
                    return WithScoutMove(mosaic, scoutMoveMode);
                case ArenaVisualType.PatchworkTiles:
                    var patchwork = ArenaVisualSettings.CreatePatchworkOverride(entry.PatchworkSet);
                    patchwork.ApplyCamera(cameraMode, followOrthographicSize, followSmoothing, lockOrthographicSize);
                    return WithScoutMove(patchwork, scoutMoveMode);
                default:
                    return WithScoutMove(
                        ArenaVisualSettings.CreateClassicOverride(
                            cameraMode,
                            followOrthographicSize,
                            followSmoothing,
                            lockOrthographicSize),
                        scoutMoveMode);
            }
        }

        static ArenaVisualSettings WithScoutMove(ArenaVisualSettings settings, ScoutMoveMode scoutMoveMode)
        {
            settings?.ApplyScoutMoveMode(scoutMoveMode);
            return settings;
        }
    }
}
