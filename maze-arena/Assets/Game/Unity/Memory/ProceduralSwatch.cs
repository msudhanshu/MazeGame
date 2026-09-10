using UnityEngine;

namespace Game.Unity.Memory
{
    static class ProceduralSwatch
    {
        public static Texture2D ForId(string entryId)
        {
            var hash = entryId != null ? entryId.GetHashCode() : 0;
            var r = ((hash & 0xFF) / 255f) * 0.55f + 0.25f;
            var g = (((hash >> 8) & 0xFF) / 255f) * 0.55f + 0.25f;
            var b = (((hash >> 16) & 0xFF) / 255f) * 0.55f + 0.25f;
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            tex.name = entryId ?? "swatch";
            var color = new Color(r, g, b, 1f);
            var pixels = new Color32[32 * 32];
            var c = (Color32)color;
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = c;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
