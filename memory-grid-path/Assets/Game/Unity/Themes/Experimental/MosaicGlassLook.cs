using Game.Unity.Themes;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Mosaic glass is clear by default. The walked path and lighthouses frost white.
    /// Wrong is a solid red pulse held for the fail sting, then the tile goes clear again.
    /// </summary>
    public readonly struct MosaicGlassLook
    {
        public MosaicGlassLook(Color tint, float tintStrength, float frost, float alpha)
        {
            Tint = tint;
            TintStrength = tintStrength;
            Frost = frost;
            Alpha = alpha;
        }

        public Color Tint { get; }
        public float TintStrength { get; }
        public float Frost { get; }
        public float Alpha { get; }

        public static MosaicGlassLook Clear { get; } =
            new MosaicGlassLook(Color.white, 0.04f, 0.05f, 0.1f);

        public static MosaicGlassLook WhiteCover { get; } =
            new MosaicGlassLook(Color.white, 0.92f, 0.9f, 0.82f);

        public static MosaicGlassLook For(TileVisualState state)
        {
            switch (state)
            {
                case TileVisualState.Walked:
                case TileVisualState.Lighthouse:
                    return WhiteCover;
                case TileVisualState.Revealed:
                    return new MosaicGlassLook(DanceFloorPalette.Goal, 0.88f, 0.72f, 0.9f);
                case TileVisualState.Wrong:
                    return new MosaicGlassLook(DanceFloorPalette.Wrong, 1f, 0.9f, 0.94f);
                case TileVisualState.WrongIntense:
                    return new MosaicGlassLook(new Color(1f, 0.05f, 0.08f), 1f, 0.95f, 1f);
                case TileVisualState.Candidate:
                    return new MosaicGlassLook(DanceFloorPalette.CandidateEdge, 0.42f, 0.22f, 0.34f);
                case TileVisualState.Start:
                    return new MosaicGlassLook(DanceFloorPalette.Start, 0.22f, 0.08f, 0.16f);
                case TileVisualState.Goal:
                    return new MosaicGlassLook(DanceFloorPalette.Goal, 0.22f, 0.08f, 0.16f);
                case TileVisualState.Pickup:
                    return new MosaicGlassLook(DanceFloorPalette.Pickup, 0.28f, 0.1f, 0.18f);
                case TileVisualState.Blocked:
                    return new MosaicGlassLook(DanceFloorPalette.Blocked, 0.35f, 0.2f, 0.28f);
                default:
                    return Clear;
            }
        }
    }
}
