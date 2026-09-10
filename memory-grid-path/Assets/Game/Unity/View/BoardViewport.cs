using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Clamped top-down pan/zoom framing for graph arenas. World bounds are centered on
    /// <see cref="Limits.Origin"/> with width/depth on the XZ plane.
    /// </summary>
    public static class BoardViewport
    {
        public const float DefaultMinZoomFactor = 0.30f;
        public const float DefaultMaxZoomFactor = 1f;
        public const float DefaultPanSlack = 0.12f;

        public struct Framing
        {
            public Vector3 Focus;
            public float OrthographicSize;
        }

        public struct Limits
        {
            public Vector3 Origin;
            public float WorldWidth;
            public float WorldDepth;
            public float DefaultOrthographicSize;
            public float MinOrthographicSize;
            public float MaxOrthographicSize;
            public float PanSlack;
        }

        public static Limits ComputeLimits(
            Vector3 origin,
            float worldWidth,
            float worldDepth,
            float aspect,
            float minZoomFactor = DefaultMinZoomFactor,
            float maxZoomFactor = DefaultMaxZoomFactor,
            float panSlack = DefaultPanSlack)
        {
            var defaultSize = BoardCamera.ContainOrthographicSize(worldWidth, worldDepth, aspect);
            return new Limits
            {
                Origin = origin,
                WorldWidth = Mathf.Max(0.01f, worldWidth),
                WorldDepth = Mathf.Max(0.01f, worldDepth),
                DefaultOrthographicSize = defaultSize,
                MinOrthographicSize = defaultSize * Mathf.Max(0.1f, minZoomFactor),
                MaxOrthographicSize = defaultSize * Mathf.Max(minZoomFactor, maxZoomFactor),
                PanSlack = Mathf.Max(0f, panSlack)
            };
        }

        public static Framing DefaultFraming(Limits limits) =>
            new Framing
            {
                Focus = limits.Origin,
                OrthographicSize = limits.DefaultOrthographicSize
            };

        public static Framing Clamp(Framing framing, Limits limits, float aspect) =>
            new Framing
            {
                Focus = ClampFocus(framing.Focus, framing.OrthographicSize, limits, aspect),
                OrthographicSize = ClampSize(framing.OrthographicSize, limits)
            };

        public static Framing ApplyPan(Framing framing, Vector2 worldDelta, Limits limits, float aspect)
        {
            var next = framing;
            next.Focus += new Vector3(worldDelta.x, 0f, worldDelta.y);
            return Clamp(next, limits, aspect);
        }

        public static Framing ApplyZoom(
            Framing framing,
            float sizeDelta,
            Vector2 zoomAnchorScreen,
            Camera camera,
            Limits limits,
            float aspect)
        {
            if (camera == null || Mathf.Approximately(sizeDelta, 0f))
                return Clamp(framing, limits, aspect);

            var before = framing;
            var nextSize = ClampSize(before.OrthographicSize + sizeDelta, limits);
            if (Mathf.Approximately(nextSize, before.OrthographicSize))
                return Clamp(before, limits, aspect);

            var anchorWorld = ScreenToBoardPoint(camera, zoomAnchorScreen, before.Focus.y);
            var next = before;
            next.OrthographicSize = nextSize;
            var anchorAfter = ScreenToBoardPoint(camera, zoomAnchorScreen, next.Focus.y);
            next.Focus += anchorWorld - anchorAfter;
            return Clamp(next, limits, aspect);
        }

        public static Vector2 ScreenDeltaToWorldDelta(Vector2 screenDelta, Camera camera, float orthographicSize)
        {
            if (camera == null)
                return Vector2.zero;

            var height = Mathf.Max(1, Screen.height);
            var width = Mathf.Max(1, Screen.width);
            var worldPerPixelY = (2f * orthographicSize) / height;
            var worldPerPixelX = (2f * orthographicSize * camera.aspect) / width;
            return new Vector2(-screenDelta.x * worldPerPixelX, -screenDelta.y * worldPerPixelY);
        }

        public static bool HasPanRoom(Framing framing, Limits limits, float aspect)
        {
            var halfWidth = framing.OrthographicSize * Mathf.Max(0.01f, aspect);
            var halfDepth = framing.OrthographicSize;
            var boardHalfWidth = limits.WorldWidth * 0.5f;
            var boardHalfDepth = limits.WorldDepth * 0.5f;
            var slack = limits.PanSlack;

            var minX = limits.Origin.x - boardHalfWidth + halfWidth - slack;
            var maxX = limits.Origin.x + boardHalfWidth - halfWidth + slack;
            var minZ = limits.Origin.z - boardHalfDepth + halfDepth - slack;
            var maxZ = limits.Origin.z + boardHalfDepth - halfDepth + slack;

            return maxX - minX > 0.01f || maxZ - minZ > 0.01f;
        }

        static float ClampSize(float size, Limits limits) =>
            Mathf.Clamp(size, limits.MinOrthographicSize, limits.MaxOrthographicSize);

        static Vector3 ClampFocus(Vector3 focus, float orthographicSize, Limits limits, float aspect)
        {
            var halfWidth = orthographicSize * Mathf.Max(0.01f, aspect);
            var halfDepth = orthographicSize;
            var boardHalfWidth = limits.WorldWidth * 0.5f;
            var boardHalfDepth = limits.WorldDepth * 0.5f;
            var slack = limits.PanSlack;

            var minX = limits.Origin.x - boardHalfWidth + halfWidth - slack;
            var maxX = limits.Origin.x + boardHalfWidth - halfWidth + slack;
            var minZ = limits.Origin.z - boardHalfDepth + halfDepth - slack;
            var maxZ = limits.Origin.z + boardHalfDepth - halfDepth + slack;

            if (minX > maxX)
                minX = maxX = limits.Origin.x;
            if (minZ > maxZ)
                minZ = maxZ = limits.Origin.z;

            return new Vector3(
                Mathf.Clamp(focus.x, minX, maxX),
                limits.Origin.y,
                Mathf.Clamp(focus.z, minZ, maxZ));
        }

        static Vector3 ScreenToBoardPoint(Camera camera, Vector2 screenPosition, float boardY)
        {
            var ray = camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, boardY, 0f));
            return plane.Raycast(ray, out var distance) ? ray.GetPoint(distance) : camera.transform.position;
        }
    }
}
