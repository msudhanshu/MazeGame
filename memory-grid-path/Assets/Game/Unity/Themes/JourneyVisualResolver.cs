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
            bool lockOrthographicSize = false)
        {
            if (entry == null)
            {
                return ArenaVisualSettings.CreateClassicOverride(
                    cameraMode,
                    followOrthographicSize,
                    followSmoothing,
                    lockOrthographicSize);
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
                    return mosaic;
                case ArenaVisualType.PatchworkTiles:
                    var patchwork = ArenaVisualSettings.CreatePatchworkOverride(entry.PatchworkSet);
                    patchwork.ApplyCamera(cameraMode, followOrthographicSize, followSmoothing, lockOrthographicSize);
                    return patchwork;
                default:
                    return ArenaVisualSettings.CreateClassicOverride(
                        cameraMode,
                        followOrthographicSize,
                        followSmoothing,
                        lockOrthographicSize);
            }
        }
    }
}
