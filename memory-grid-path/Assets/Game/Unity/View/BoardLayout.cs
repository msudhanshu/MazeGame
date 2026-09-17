using System.Collections.Generic;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Maps board cells to world positions and back. Pure arithmetic, so it can be exercised
    /// without a scene. The board lies flat on the XZ plane, centred on <see cref="Origin"/>,
    /// with +X to the right and +Z away from the camera.
    /// </summary>
    public readonly struct BoardLayout
    {
        public BoardLayout(GridSize size, float tileSize, float gap, Vector3 origin)
        {
            Size = size;
            TileSize = tileSize;
            Gap = gap;
            Origin = origin;
        }

        public GridSize Size { get; }
        public float TileSize { get; }
        public float Gap { get; }
        public Vector3 Origin { get; }

        /// <summary>Distance between the centres of two neighbouring tiles.</summary>
        public float Pitch => TileSize + Gap;

        /// <summary>
        /// Dead zone on the standing cell. Smaller than a tile so the outgoing choice
        /// line stays clickable right next to the walker.
        /// </summary>
        public const float StandingPickRadius = 0.05f;

        public float Width => Size.Width * Pitch;
        public float Depth => Size.Height * Pitch;

        /// <summary>Outer edge-to-edge span of the tiles, excluding the extra trailing gap in <see cref="Width"/>.</summary>
        public float SurfaceWidth => Mathf.Max(0f, Width - Gap);

        /// <summary>Outer edge-to-edge span of the tiles, excluding the extra trailing gap in <see cref="Depth"/>.</summary>
        public float SurfaceDepth => Mathf.Max(0f, Depth - Gap);

        public Vector3 WorldPosition(GridCoord coord)
        {
            var x = (coord.X - (Size.Width - 1) * 0.5f) * Pitch;
            var z = (coord.Y - (Size.Height - 1) * 0.5f) * Pitch;
            return Origin + new Vector3(x, 0f, z);
        }

        public bool TryCoordAt(Vector3 worldPosition, out GridCoord coord)
        {
            var local = worldPosition - Origin;
            var x = Mathf.RoundToInt(local.x / Pitch + (Size.Width - 1) * 0.5f);
            var y = Mathf.RoundToInt(local.z / Pitch + (Size.Height - 1) * 0.5f);

            coord = new GridCoord(x, y);
            return Size.Contains(coord);
        }

        /// <summary>Drops a camera ray onto the board plane and reports the tile it lands on.</summary>
        public bool TryCoordUnderRay(Ray ray, out GridCoord coord)
        {
            coord = default;

            var plane = new Plane(Vector3.up, Origin);
            if (!plane.Raycast(ray, out var distance))
                return false;

            return TryCoordAt(ray.GetPoint(distance), out coord);
        }

        public bool TryPickOptionUnderRay(
            Ray ray,
            GridCoord current,
            IReadOnlyList<GridCoord> options,
            float tilePickRadius,
            float edgePickRadius,
            out GridCoord target)
        {
            target = default;
            if (options == null || options.Count == 0)
                return false;

            var plane = new Plane(Vector3.up, Origin);
            if (!plane.Raycast(ray, out var distance))
                return false;

            var world = ray.GetPoint(distance);
            var bestDistance = float.MaxValue;
            var found = false;
            var currentWorld = WorldPosition(current);
            if (Vector3.Distance(world, currentWorld) <= StandingPickRadius)
                return false;

            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var optionWorld = WorldPosition(option);
                var tileDistance = Vector3.Distance(world, optionWorld);
                if (tileDistance <= tilePickRadius && tileDistance < bestDistance)
                {
                    bestDistance = tileDistance;
                    target = option;
                    found = true;
                }

                var edgeDistance = DistanceToSegment(world, currentWorld, optionWorld);
                if (edgeDistance <= edgePickRadius && edgeDistance < bestDistance)
                {
                    bestDistance = edgeDistance;
                    target = option;
                    found = true;
                }
            }

            return found;
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0001f)
                return Vector3.Distance(point, a);

            var t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
            var closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }
    }
}
