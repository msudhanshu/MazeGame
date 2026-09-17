using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Themes
{
    /// <summary>
    /// Colours for the dance-floor look. The six base hues are laid out in a repeating
    /// diagonal so the resting board reads as an evenly mixed light floor and never hints
    /// at where the path runs.
    /// </summary>
    public static class DanceFloorPalette
    {
        static readonly Color[] BaseHues =
        {
            new Color(0.05f, 0.82f, 0.96f),
            new Color(0.96f, 0.22f, 0.55f),
            new Color(0.96f, 0.42f, 0.16f),
            new Color(0.64f, 0.34f, 0.94f),
            new Color(0.16f, 0.80f, 0.70f),
            new Color(0.96f, 0.58f, 0.24f)
        };

        public static readonly Color Grout = new Color(0.078f, 0.094f, 0.129f);
        public static readonly Color Background = new Color(0.043f, 0.051f, 0.078f);
        public static readonly Color Walked = new Color(1.00f, 1.00f, 1.00f);
        public static readonly Color Revealed = new Color(0.85f, 0.95f, 1.00f);
        public static readonly Color Wrong = new Color(1.00f, 0.12f, 0.12f);
        public static readonly Color Candidate = new Color(0.92f, 0.96f, 1.00f);
        public static readonly Color CandidateEdge = new Color(1.00f, 0.95f, 0.35f);
        public static readonly Color Start = new Color(0.30f, 1.00f, 0.55f);
        public static readonly Color Goal = new Color(1.00f, 0.85f, 0.25f);
        // Distinct pulse until real pickup VFX lands. Lighthouses reuse Revealed white.
        public static readonly Color Pickup = new Color(1.00f, 0.42f, 0.92f);
        public static readonly Color Blocked = new Color(0.22f, 0.24f, 0.30f);

        public static Color BaseColorFor(GridCoord coord)
        {
            var index = ((coord.X + coord.Y * 2) % BaseHues.Length + BaseHues.Length) % BaseHues.Length;
            return BaseHues[index];
        }
    }
}
