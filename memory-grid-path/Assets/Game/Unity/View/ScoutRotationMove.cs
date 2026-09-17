using System.Collections;
using System.Collections.Generic;
using Game.Unity.Themes.Experimental;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Heading-up Scout follow math. PanMoveMode keeps north-up; RotationMoveMode yaws
    /// the ortho camera and walker with the path.
    /// </summary>
    public static class ScoutRotationMove
    {
        public const float PlayYawDegreesPerSecond = 150f;
        public const float TourYawDegreesPerSecond = 110f;
        /// <summary>Seconds to walk one world unit. Graph splines scale hop time by arc length.</summary>
        public const float PlayTravelSeconds = 0.46f;
        public const float FacingAlignDegrees = 0.35f;
        /// <summary>Seconds to walk one world unit during the radar tour (level 4+).</summary>
        public const float TourHopSeconds = 1.9f;
        /// <summary>Seconds to walk one world unit on the first three Scout boards.</summary>
        public const float EarlyTourHopSeconds = 4.2f;
        public const int EarlyTourLevels = 3;
        public const float TourPauseSeconds = 0.45f;
        public const float EarlyTourPauseSeconds = 0.8f;
        public const float DestinationHoldSeconds = 0.75f;
        public const float OverviewHoldSeconds = 1.2f;
        public const float ZoomSeconds = 1.45f;
        public const float FollowSizeTwo = 0.48f;
        public const float FollowSizeThree = 0.58f;
        public const float FollowSizeFour = 0.72f;
        public const float FollowSizeFive = 0.88f;
        /// <summary>Scout walker is smaller than a tile-arena pawn so city tiles stay readable.</summary>
        public const float WalkerScale = 0.42f;
        /// <summary>Yellow walked trail, relative to the default overlay width.</summary>
        public const float TrailWidthScale = 0.55f;

        public static float FollowOrthographicSizeFor(GridSize size) =>
            FollowOrthographicSizeFor(size.Width, size.Height);

        public static float FollowOrthographicSizeFor(int width, int height)
        {
            var longest = Mathf.Max(1, Mathf.Max(width, height));
            if (longest <= 2)
                return FollowSizeTwo;
            if (longest <= 3)
                return FollowSizeThree;
            if (longest <= 4)
                return FollowSizeFour;
            return FollowSizeFive;
        }

        public static bool UsesRotation(ArenaVisualSettings settings) =>
            settings != null && settings.ScoutMoveMode == ScoutMoveMode.RotationMoveMode;

        public static float TourHopSecondsFor(int levelNumber) =>
            levelNumber <= EarlyTourLevels ? EarlyTourHopSeconds : TourHopSeconds;

        public static float TourPauseSecondsFor(int levelNumber) =>
            levelNumber <= EarlyTourLevels ? EarlyTourPauseSeconds : TourPauseSeconds;

        public static float TourTotalSeconds(IReadOnlyList<float> hopSeconds) =>
            TourTotalSeconds(hopSeconds, TourPauseSeconds);

        public static float TourTotalSeconds(IReadOnlyList<float> hopSeconds, float pauseSeconds)
        {
            if (hopSeconds == null || hopSeconds.Count == 0)
                return 0f;

            var total = 0f;
            for (var i = 0; i < hopSeconds.Count; i++)
            {
                total += Mathf.Max(0f, hopSeconds[i]);
                if (i < hopSeconds.Count - 1)
                    total += Mathf.Max(0f, pauseSeconds);
            }

            return total;
        }

        public static float TourCoveredSeconds(
            IReadOnlyList<float> hopSeconds,
            int completedHops,
            float currentHopNormalized) =>
            TourCoveredSeconds(hopSeconds, completedHops, currentHopNormalized, TourPauseSeconds);

        public static float TourCoveredSeconds(
            IReadOnlyList<float> hopSeconds,
            int completedHops,
            float currentHopNormalized,
            float pauseSeconds)
        {
            if (hopSeconds == null || hopSeconds.Count == 0)
                return 0f;

            var covered = 0f;
            var hops = Mathf.Clamp(completedHops, 0, hopSeconds.Count);
            for (var i = 0; i < hops; i++)
            {
                covered += Mathf.Max(0f, hopSeconds[i]);
                if (i < hops - 1)
                    covered += Mathf.Max(0f, pauseSeconds);
            }

            if (hops >= hopSeconds.Count)
                return covered;

            return covered + Mathf.Max(0f, hopSeconds[hops]) * Mathf.Clamp01(currentHopNormalized);
        }

        public static string FormatClock(float seconds)
        {
            var whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }

        public static bool TryYawFromDelta(Vector3 delta, out float yawDegrees)
        {
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
            {
                yawDegrees = 0f;
                return false;
            }

            yawDegrees = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            return true;
        }

        public static float YawFromDelta(Vector3 delta)
        {
            TryYawFromDelta(delta, out var yaw);
            return yaw;
        }

        public static float HeadingYaw(Camera camera)
        {
            if (camera == null)
                return 0f;
            TryYawFromDelta(camera.transform.up, out var yaw);
            return yaw;
        }

        public static GridCoord RotateSwipe(GridCoord offset, float yawDegrees)
        {
            if (offset.X == 0 && offset.Y == 0)
                return offset;

            var rotation = BoardCamera.TopDownHeading(yawDegrees);
            var world = rotation * Vector3.right * offset.X + rotation * Vector3.up * offset.Y;
            world.y = 0f;
            if (world.sqrMagnitude < 0.0001f)
                return offset;
            if (Mathf.Abs(world.x) >= Mathf.Abs(world.z))
                return new GridCoord(world.x >= 0f ? 1 : -1, 0);
            return new GridCoord(0, world.z >= 0f ? 1 : -1);
        }

        public static IEnumerator LerpFollow(
            Camera camera,
            Vector3 fromFocus,
            float fromSize,
            float fromYaw,
            Vector3 toFocus,
            float toSize,
            float toYaw,
            float seconds)
        {
            if (camera == null)
                yield break;

            if (seconds <= 0.01f)
            {
                BoardCamera.FrameFollow(camera, toFocus, toSize, toYaw);
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds));
                var focus = Vector3.Lerp(fromFocus, toFocus, t);
                var yaw = Mathf.LerpAngle(fromYaw, toYaw, t);
                BoardCamera.FrameFollow(camera, focus, Mathf.Lerp(fromSize, toSize, t), yaw);
                yield return null;
            }

            BoardCamera.FrameFollow(camera, toFocus, toSize, toYaw);
        }
    }
}
