using UnityEngine;

namespace Game.Unity.Graph
{
    /// <summary>
    /// Shared mapping between the authored photo, the editor canvas, and play-mode world space.
    /// Normalized node coords are UV-style over the photo (0,0 bottom-left, y-up).
    /// </summary>
    public static class GraphImageFit
    {
        public static float Aspect(Texture texture)
        {
            if (texture == null)
                return 1f;

            return texture.width / (float)Mathf.Max(1, texture.height);
        }

        public static void WorldSize(float worldWidth, Texture texture, out float width, out float depth) =>
            WorldSize(worldWidth, Aspect(texture), out width, out depth);

        public static void WorldSize(float worldWidth, float imageAspect, out float width, out float depth)
        {
            width = Mathf.Max(1f, worldWidth);
            depth = width / Mathf.Max(0.01f, imageAspect);
        }

        public static Rect FittedRect(Rect canvas, float imageAspect)
        {
            if (canvas.width <= 0.001f || canvas.height <= 0.001f)
                return canvas;

            var canvasAspect = canvas.width / canvas.height;
            var aspect = Mathf.Max(0.01f, imageAspect);
            if (canvasAspect > aspect)
            {
                var width = canvas.height * aspect;
                return new Rect(canvas.x + (canvas.width - width) * 0.5f, canvas.y, width, canvas.height);
            }

            var height = canvas.width / aspect;
            return new Rect(canvas.x, canvas.y + (canvas.height - height) * 0.5f, canvas.width, height);
        }

        public static Rect FittedRect(Rect canvas, Texture texture) =>
            texture == null ? canvas : FittedRect(canvas, Aspect(texture));

        public static Vector2 GuiToNormalized(Rect fitted, Vector2 gui)
        {
            var x = Mathf.Clamp01((gui.x - fitted.x) / Mathf.Max(0.0001f, fitted.width));
            var y = 1f - Mathf.Clamp01((gui.y - fitted.y) / Mathf.Max(0.0001f, fitted.height));
            return new Vector2(x, y);
        }

        public static Vector2 NormalizedToGui(Rect fitted, Vector2 normalized) =>
            new Vector2(
                fitted.x + normalized.x * fitted.width,
                fitted.y + (1f - normalized.y) * fitted.height);

        public static Vector3 NormalizedToWorld(Vector2 normalized, float worldWidth, float worldDepth, Vector3 origin)
        {
            var x = (normalized.x - 0.5f) * worldWidth;
            var z = (normalized.y - 0.5f) * worldDepth;
            return origin + new Vector3(x, 0f, z);
        }
    }
}
