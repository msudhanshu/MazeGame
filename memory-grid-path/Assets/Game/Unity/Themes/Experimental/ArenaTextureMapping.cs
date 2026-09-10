using Nixin.Grid.Core;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Maps each board cell to the portion of a shared arena image it should display.
    /// UVs are normalized 0–1 across the full texture; tile quads still use 0–1 local UVs
    /// and the shader lerps between the rect corners.
    /// </summary>
    public static class ArenaTextureMapping
    {
        public readonly struct Region
        {
            public Region(float minU, float minV, float maxU, float maxV)
            {
                MinU = minU;
                MinV = minV;
                MaxU = maxU;
                MaxV = maxV;
            }

            public float MinU { get; }
            public float MinV { get; }
            public float MaxU { get; }
            public float MaxV { get; }
        }

        public static Region ForTile(GridCoord coord, GridSize size, bool flipVertical = false)
        {
            var width = size.Width;
            var height = size.Height;
            var minU = coord.X / (float)width;
            var maxU = (coord.X + 1) / (float)width;
            var minV = coord.Y / (float)height;
            var maxV = (coord.Y + 1) / (float)height;

            if (!flipVertical)
                return new Region(minU, minV, maxU, maxV);

            return new Region(minU, 1f - maxV, maxU, 1f - minV);
        }
    }
}
