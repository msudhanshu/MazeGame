using Game.Unity.Data;
using Game.Unity.Input;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using Nixin.Grid.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class ScoutRotationMoveTests
    {
        [Test]
        public void YawFromDeltaUsesWorldForwardAsZero()
        {
            Assert.That(ScoutRotationMove.YawFromDelta(Vector3.forward), Is.EqualTo(0f).Within(0.01f));
            Assert.That(ScoutRotationMove.YawFromDelta(Vector3.right), Is.EqualTo(90f).Within(0.01f));
            Assert.That(ScoutRotationMove.TryYawFromDelta(Vector3.zero, out _), Is.False);
        }

        [Test]
        public void RotateSwipeKeepsNorthUpWhenYawIsZero()
        {
            Assert.That(ScoutRotationMove.RotateSwipe(new GridCoord(1, 0), 0f), Is.EqualTo(new GridCoord(1, 0)));
            Assert.That(ScoutRotationMove.RotateSwipe(new GridCoord(0, 1), 0f), Is.EqualTo(new GridCoord(0, 1)));
        }

        [Test]
        public void RotateSwipeTurnsScreenUpIntoFacingWhenYawedEast()
        {
            Assert.That(ScoutRotationMove.RotateSwipe(new GridCoord(0, 1), 90f), Is.EqualTo(new GridCoord(1, 0)));
            Assert.That(ScoutRotationMove.RotateSwipe(new GridCoord(1, 0), 90f), Is.EqualTo(new GridCoord(0, -1)));
        }

        [Test]
        public void BoardMoveSwipeUsesHeadingYaw()
        {
            var layout = new BoardLayout(new GridSize(5, 5), 1f, 0f, Vector3.zero);
            var current = new GridCoord(2, 2);

            Assert.That(
                BoardMove.TryResolve(
                    StrokeResult.Swipe(new GridCoord(0, 1)),
                    Vector2.zero,
                    null,
                    layout,
                    current,
                    90f,
                    out var target),
                Is.True);
            Assert.That(target, Is.EqualTo(new GridCoord(3, 2)));
        }

        [Test]
        public void VisualResolverCopiesScoutMoveMode()
        {
            var entry = new JourneyLevelEntry { VisualType = ArenaVisualType.PatchworkTiles };
            var settings = JourneyVisualResolver.Resolve(
                entry,
                ArenaCameraMode.FollowWalker,
                null,
                scoutMoveMode: ScoutMoveMode.RotationMoveMode);

            Assert.That(settings.ScoutMoveMode, Is.EqualTo(ScoutMoveMode.RotationMoveMode));
            Assert.That(ScoutRotationMove.UsesRotation(settings), Is.True);
        }

        [Test]
        public void TourClockCountsHopsAndPauses()
        {
            var hops = new[] { 1f, 2f, 0.5f };
            Assert.That(
                ScoutRotationMove.TourTotalSeconds(hops),
                Is.EqualTo(3.5f + ScoutRotationMove.TourPauseSeconds * 2f).Within(0.0001f));
            Assert.That(ScoutRotationMove.TourCoveredSeconds(hops, 0, 0.5f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(ScoutRotationMove.TourCoveredSeconds(hops, 1, 0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(
                ScoutRotationMove.TourCoveredSeconds(hops, 1, 1f),
                Is.EqualTo(1f + 2f).Within(0.0001f));
            Assert.That(ScoutRotationMove.TourHopSecondsFor(1), Is.GreaterThan(ScoutRotationMove.TourHopSecondsFor(4)));
            Assert.That(ScoutRotationMove.TourHopSecondsFor(3), Is.EqualTo(ScoutRotationMove.EarlyTourHopSeconds));
            Assert.That(ScoutRotationMove.TourHopSecondsFor(4), Is.EqualTo(ScoutRotationMove.TourHopSeconds));
            Assert.That(ScoutRotationMove.FollowOrthographicSizeFor(2, 2), Is.EqualTo(ScoutRotationMove.FollowSizeTwo));
            Assert.That(ScoutRotationMove.FollowOrthographicSizeFor(2, 3), Is.EqualTo(ScoutRotationMove.FollowSizeThree));
            Assert.That(ScoutRotationMove.FollowOrthographicSizeFor(3, 4), Is.EqualTo(ScoutRotationMove.FollowSizeFour));
            Assert.That(ScoutRotationMove.FollowOrthographicSizeFor(4, 5), Is.EqualTo(ScoutRotationMove.FollowSizeFive));
            Assert.That(ScoutRotationMove.FollowSizeTwo, Is.LessThan(0.65f));
            Assert.That(ScoutRotationMove.WalkerScale, Is.LessThan(0.5f));
            Assert.That(ScoutRotationMove.TrailWidthScale, Is.LessThan(0.7f));
        }
    }
}
