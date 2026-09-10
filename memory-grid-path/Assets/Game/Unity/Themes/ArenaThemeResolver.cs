using Game.Unity.Themes.Experimental;
using Game.Unity.View;

namespace Game.Unity.Themes
{
    /// <summary>
    /// Picks the arena theme implementation from designer settings.
    /// </summary>
    public static class ArenaThemeResolver
    {
        public const string ClassicThemeId = "dance_floor";
        public const string MosaicExperimentalThemeId = "mosaic_arena_experimental";
        public const string PatchworkExperimentalThemeId = "patchwork_arena_experimental";

        public static ITileViewFactory Resolve(ArenaVisualSettings settings)
        {
            if (settings == null)
                return new DanceFloorTileViewFactory();

            switch (settings.VisualType)
            {
                case ArenaVisualType.MosaicImage:
                    if (settings.MosaicTexture != null)
                        return new MosaicArenaTileViewFactory(settings);
                    break;

                case ArenaVisualType.PatchworkTiles:
                    if (settings.HasPatchworkTextures)
                        return new PatchworkArenaTileViewFactory(settings);
                    break;
            }

            return new DanceFloorTileViewFactory();
        }
    }
}
