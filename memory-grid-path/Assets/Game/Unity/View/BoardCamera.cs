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

        public static Quaternion TopDownHeading(float yawDegrees) =>
            Quaternion.Euler(90f, yawDegrees, 0f);

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
        public static void FrameFollow(Camera camera, Vector3 focusOnBoard, float orthographicSize) =>
            FrameFollow(camera, focusOnBoard, orthographicSize, headingYaw: 0f);

        public static void FrameFollow(Camera camera, Vector3 focusOnBoard, float orthographicSize, float headingYaw)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(
                new Vector3(focusOnBoard.x, Height, focusOnBoard.z),
                TopDownHeading(headingYaw));
            camera.orthographicSize = Mathf.Max(0.5f, orthographicSize);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        /// <summary>Smoothly pans the follow camera toward the walker.</summary>
        public static void SmoothFollowWalker(
            Camera camera,
            Vector3 focusOnBoard,
            float orthographicSize,
            float smoothing) =>
            SmoothFollowWalker(camera, focusOnBoard, orthographicSize, smoothing, headingYaw: 0f, yawSmoothing: 0f);

        public static void SmoothFollowWalker(
            Camera camera,
            Vector3 focusOnBoard,
            float orthographicSize,
            float smoothing,
            float headingYaw,
            float yawSmoothing)
        {
            if (camera == null)
                return;

            var targetPosition = new Vector3(focusOnBoard.x, Height, focusOnBoard.z);
            var blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.deltaTime);
            camera.orthographic = true;
            var targetRotation = TopDownHeading(headingYaw);
            if (yawSmoothing <= 0.01f)
                camera.transform.rotation = targetRotation;
            else
            {
                var yawBlend = 1f - Mathf.Exp(-Mathf.Max(0.01f, yawSmoothing) * Time.deltaTime);
                camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, targetRotation, yawBlend);
            }

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
        public static float ContainOrthographicSize(float width, float depth, float aspect, float topViewportInset = 0f)
        {
            var safeAspect = aspect > 0.01f ? aspect : 1f;
            var usableVertical = Mathf.Max(0.01f, 1f - ClampTopInset(topViewportInset));
            var halfWidth = Mathf.Max(0.01f, width) * 0.5f;
            var halfDepth = Mathf.Max(0.01f, depth) * 0.5f;
            return Mathf.Max(halfDepth / usableVertical, halfWidth / safeAspect);
        }

        /// <summary>
        /// Orthographic size that fills the screen with the width×depth rectangle.
        /// Extra photo is cropped; no letterbox bars remain around the arena.
        /// </summary>
        public static float CoverOrthographicSize(float width, float depth, float aspect)
        {
            var safeAspect = aspect > 0.01f ? aspect : 1f;
            var halfWidth = Mathf.Max(0.01f, width) * 0.5f;
            var halfDepth = Mathf.Max(0.01f, depth) * 0.5f;
            return Mathf.Min(halfDepth, halfWidth / safeAspect);
        }

        /// <summary>Frames a rectangular board area defined by width and depth on the XZ plane.</summary>
        public static void FrameTopDownForBounds(
            Camera camera,
            Vector3 origin,
            float width,
            float depth,
            float aspect,
            bool bottomAlign = false,
            float topViewportInset = 0f)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            var inset = bottomAlign ? 0f : topViewportInset;
            var size = ContainOrthographicSize(width, depth, aspect, inset) * Padding;
            var focus = origin;
            if (bottomAlign)
                focus.z += BottomAlignFocusOffset(depth, size);
            else
                focus.z += TopHudFocusOffset(inset, size);
            camera.transform.SetPositionAndRotation(focus + Vector3.up * Height, TopDownRotation);
            camera.orthographicSize = size;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = FarClip;
        }

        /// <summary>
        /// Extra camera +Z so a shorter-than-screen board sits on the bottom edge, with
        /// empty space (fog) above it. Zero when the board already fills the height.
        /// </summary>
        public static float BottomAlignFocusOffset(float worldDepth, float orthographicSize)
        {
            var halfDepth = Mathf.Max(0.01f, worldDepth) * 0.5f;
            return Mathf.Max(0f, Mathf.Max(0.0001f, orthographicSize) - halfDepth);
        }

        /// <summary>
        /// Top-down framing that fills the screen with the photo. Mismatched aspects crop
        /// rather than letterbox.
        /// </summary>
        public static void FrameTopDownCover(Camera camera, Vector3 origin, float width, float depth, float aspect)
        {
            if (camera == null)
                return;

            camera.orthographic = true;
            camera.transform.SetPositionAndRotation(origin + Vector3.up * Height, TopDownRotation);
            camera.orthographicSize = CoverOrthographicSize(width, depth, aspect);
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
