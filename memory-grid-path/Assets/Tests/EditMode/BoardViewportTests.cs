using Game.Unity.View;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class BoardViewportTests
    {
        [Test]
        public void DefaultFramingContainsTheWholeBoard()
        {
            var limits = BoardViewport.ComputeLimits(Vector3.zero, worldWidth: 12f, worldDepth: 18f, aspect: 9f / 16f);
            var framing = BoardViewport.DefaultFraming(limits);

            Assert.That(framing.Focus, Is.EqualTo(Vector3.zero));
            Assert.That(
                framing.OrthographicSize,
                Is.EqualTo(BoardCamera.ContainOrthographicSize(12f, 18f, 9f / 16f)).Within(0.001f));
        }

        [Test]
        public void DefaultFramingShowsATallPhotoOnAWideScreen()
        {
            var aspect = 16f / 9f;
            var limits = BoardViewport.ComputeLimits(Vector3.zero, worldWidth: 12f, worldDepth: 24f, aspect);
            var framing = BoardViewport.DefaultFraming(limits);

            Assert.That(framing.OrthographicSize * 2f, Is.GreaterThanOrEqualTo(24f - 0.001f));
            Assert.That(framing.OrthographicSize * 2f * aspect, Is.GreaterThanOrEqualTo(12f - 0.001f));
            Assert.That(
                framing.OrthographicSize,
                Is.GreaterThan(BoardCamera.FitWidthOrthographicSize(12f, aspect)));
        }

        [Test]
        public void ZoomIsClampedBetweenConfiguredFactors()
        {
            var limits = BoardViewport.ComputeLimits(Vector3.zero, 10f, 8f, aspect: 1f);
            var framing = BoardViewport.DefaultFraming(limits);

            var zoomedIn = BoardViewport.Clamp(
                new BoardViewport.Framing { Focus = Vector3.zero, OrthographicSize = limits.MinOrthographicSize * 0.1f },
                limits,
                aspect: 1f);
            var zoomedOut = BoardViewport.Clamp(
                new BoardViewport.Framing { Focus = Vector3.zero, OrthographicSize = limits.MaxOrthographicSize * 2f },
                limits,
                aspect: 1f);

            Assert.That(zoomedIn.OrthographicSize, Is.EqualTo(limits.MinOrthographicSize).Within(0.001f));
            Assert.That(zoomedOut.OrthographicSize, Is.EqualTo(limits.MaxOrthographicSize).Within(0.001f));
        }

        [Test]
        public void HasPanRoomWhenZoomedInButNotAtDefaultOverview()
        {
            var limits = BoardViewport.ComputeLimits(Vector3.zero, worldWidth: 12f, worldDepth: 12f, aspect: 1f);
            var framing = new BoardViewport.Framing
            {
                Focus = Vector3.zero,
                OrthographicSize = limits.MinOrthographicSize
            };

            Assert.That(BoardViewport.HasPanRoom(framing, limits, aspect: 1f), Is.True);
            Assert.That(BoardViewport.HasPanRoom(BoardViewport.DefaultFraming(limits), limits, aspect: 1f), Is.False);
        }

        [Test]
        public void PanStaysInsideBoardBoundsWhenZoomedIn()
        {
            var limits = BoardViewport.ComputeLimits(Vector3.zero, worldWidth: 12f, worldDepth: 12f, aspect: 1f);
            var framing = new BoardViewport.Framing
            {
                Focus = Vector3.zero,
                OrthographicSize = limits.MinOrthographicSize
            };

            var panned = BoardViewport.ApplyPan(framing, new Vector2(50f, -50f), limits, aspect: 1f);

            Assert.That(Mathf.Abs(panned.Focus.x), Is.LessThanOrEqualTo(6f));
            Assert.That(Mathf.Abs(panned.Focus.z), Is.LessThanOrEqualTo(6f));
        }

        [Test]
        public void PanLocksToOriginWhenFullyZoomedOut()
        {
            var limits = BoardViewport.ComputeLimits(
                Vector3.zero,
                worldWidth: 8f,
                worldDepth: 8f,
                aspect: 16f / 9f,
                panSlack: 0f);
            var framing = BoardViewport.DefaultFraming(limits);

            var panned = BoardViewport.ApplyPan(framing, new Vector2(4f, -3f), limits, aspect: 16f / 9f);

            Assert.That(panned.Focus.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(panned.Focus.z, Is.EqualTo(0f).Within(0.001f));
        }
    }
}
