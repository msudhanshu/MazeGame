using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Straight top-down framing so the grid reads as a 2D board. Orthographic, looking
    /// down -Y, with +Z as screen-up to match swipe-north.
    /// </summary>
    public static class BoardCamera
    {
        public const float Padding = 1f;
        public const float Height = 12f;
        public const float FarClip = 24f;

        public static Quaternion TopDownRotation { get; } = Quaternion.Euler(90f, 0f, 0f);

        public static void FrameTopDown(Camera camera, BoardLayout layout, float aspect, float topViewportInset = 0f)
        {
            if (camera == null)
                return;

            var size = OrthographicSize(layout, aspect, topViewportInset);
            var focus = layout.Origin + Vector3.forward * TopHudFocusOffset(topViewportInset, size);
            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(
                focus + Vector3.up * Height,
                TopDownRotation);
            camera.orthographicSize = size;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        public static float OrthographicSize(BoardLayout layout, float aspect, float topViewportInset = 0f)
        {
            var usableVertical = Mathf.Max(0.01f, 1f - ClampTopInset(topViewportInset));
            var halfDepth = layout.Depth * 0.5f * Padding;
            var halfWidth = layout.Width * 0.5f * Padding;
            var safeAspect = aspect > 0.01f ? aspect : 1f;
            return Mathf.Max(halfDepth / usableVertical, halfWidth / safeAspect);
        }

        public static float ClampTopInset(float topViewportInset) =>
            Mathf.Clamp(topViewportInset, 0f, 0.45f);

        public static float TopHudFocusOffset(float topViewportInset, float orthographicSize) =>
            ClampTopInset(topViewportInset) * Mathf.Max(0f, orthographicSize);

        /// <summary>Viewport Y of a world Z on a top-down ortho camera (0 = bottom, 1 = top).</summary>
        public static float ViewportY(float worldZ, float cameraZ, float orthographicSize) =>
            0.5f + (worldZ - cameraZ) / (2f * Mathf.Max(0.0001f, orthographicSize));

        /// <summary>Zoomed top-down camera locked on a board point (walker focus).</summary>
        public static void FrameFollow(Camera camera, Vector3 focusOnBoard, float orthographicSize)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(
                new Vector3(focusOnBoard.x, Height, focusOnBoard.z),
                TopDownRotation);
            camera.orthographicSize = Mathf.Max(0.5f, orthographicSize);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        /// <summary>Smoothly pans the follow camera toward the walker.</summary>
        public static void SmoothFollowWalker(
            Camera camera,
            Vector3 focusOnBoard,
            float orthographicSize,
            float smoothing)
        {
            if (camera == null)
                return;

            var targetPosition = new Vector3(focusOnBoard.x, Height, focusOnBoard.z);
            var blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.deltaTime);
            camera.orthographic = true;
            camera.transform.rotation = TopDownRotation;
            camera.transform.position = Vector3.Lerp(camera.transform.position, targetPosition, blend);
            camera.orthographicSize = Mathf.Lerp(
                camera.orthographicSize,
                Mathf.Max(0.5f, orthographicSize),
                blend);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        public static bool NearlyMatchesOverview(Camera camera, Vector3 targetPosition, float targetSize)
        {
            if (camera == null || !camera.orthographic)
                return false;

            var size = Mathf.Max(0.5f, targetSize);
            if (Mathf.Abs(camera.orthographicSize - targetSize) > Mathf.Max(0.12f, size * 0.08f))
                return false;

            var delta = camera.transform.position - targetPosition;
            delta.y = 0f;
            var maxDelta = size * 0.2f;
            return delta.sqrMagnitude <= maxDelta * maxDelta;
        }

        public static bool IsTopDown(Camera camera)
        {
            if (camera == null || !camera.orthographic)
                return false;

            var forward = camera.transform.forward;
            return Vector3.Dot(forward, Vector3.down) > 0.99f;
        }

        /// <summary>
        /// Orthographic size that shows the whole width×depth rectangle without stretching.
        /// Wider or taller screens letterbox; the photo aspect stays intact.
        /// </summary>
        public static float ContainOrthographicSize(float width, float depth, float aspect)
        {
            var safeAspect = aspect > 0.01f ? aspect : 1f;
            var halfWidth = Mathf.Max(0.01f, width) * 0.5f;
            var halfDepth = Mathf.Max(0.01f, depth) * 0.5f;
            return Mathf.Max(halfDepth, halfWidth / safeAspect);
        }

        /// <summary>Frames a rectangular board area defined by width and depth on the XZ plane.</summary>
        public static void FrameTopDownForBounds(
            Camera camera,
            Vector3 origin,
            float width,
            float depth,
            float aspect)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(origin + Vector3.up * Height, TopDownRotation);
            camera.orthographicSize = ContainOrthographicSize(width, depth, aspect) * Padding;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        /// <summary>
        /// Top-down framing that pins world width to the screen width (horizontal fit).
        /// Extra height is cropped or letterboxed; the image aspect is not stretched.
        /// </summary>
        public static void FrameTopDownFitWidth(Camera camera, Vector3 origin, float width, float aspect)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(origin + Vector3.up * Height, TopDownRotation);
            camera.orthographicSize = FitWidthOrthographicSize(width, aspect);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        public static float FitWidthOrthographicSize(float width, float aspect)
        {
            var safeAspect = aspect > 0.01f ? aspect : 1f;
            return (Mathf.Max(0.01f, width) * 0.5f) / safeAspect;
        }
    }
}
