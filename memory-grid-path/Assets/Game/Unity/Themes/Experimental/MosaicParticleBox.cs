using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Axis-aligned volume under the mosaic tiles. Particle centres are confined here
    /// so the VFX reads as a sealed box beneath the grid.
    /// </summary>
    public readonly struct MosaicParticleBox
    {
        public const float Height = 0.28f;
        public const float EdgePad = 0.05f;

        public MosaicParticleBox(Vector3 center, Vector3 size)
        {
            Center = center;
            Size = new Vector3(Mathf.Max(0.05f, size.x), Mathf.Max(0.04f, size.y), Mathf.Max(0.05f, size.z));
        }

        public Vector3 Center { get; }
        public Vector3 Size { get; }
        public Vector3 Extents => Size * 0.5f;
        public Vector3 Min => Center - Extents;
        public Vector3 Max => Center + Extents;

        public static MosaicParticleBox ForBoard(BoardLayout layout)
        {
            var size = new Vector3(
                layout.SurfaceWidth - EdgePad * 2f,
                Height,
                layout.SurfaceDepth - EdgePad * 2f);
            var center = layout.Origin + new Vector3(0f, -Height * 0.5f - 0.008f, 0f);
            return new MosaicParticleBox(center, size);
        }

        public bool Contains(Vector3 world)
        {
            var delta = world - Center;
            var extents = Extents;
            return Mathf.Abs(delta.x) <= extents.x + 0.0001f
                && Mathf.Abs(delta.y) <= extents.y + 0.0001f
                && Mathf.Abs(delta.z) <= extents.z + 0.0001f;
        }

        public void Confine(ref Vector3 world, ref Vector3 velocity)
        {
            var min = Min;
            var max = Max;
            Bounce(ref world.x, ref velocity.x, min.x, max.x);
            Bounce(ref world.y, ref velocity.y, min.y, max.y);
            Bounce(ref world.z, ref velocity.z, min.z, max.z);
        }

        static void Bounce(ref float position, ref float speed, float min, float max)
        {
            if (position < min)
            {
                position = min;
                speed = Mathf.Abs(speed);
            }
            else if (position > max)
            {
                position = max;
                speed = -Mathf.Abs(speed);
            }
        }
    }
}
