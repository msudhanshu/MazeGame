using System.Collections.Generic;
using Game.Core.Rules;
using NUnit.Framework;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class PathDragTrackerTests
    {
        static readonly GridCoord A = new GridCoord(0, 0);
        static readonly GridCoord B = new GridCoord(1, 0);
        static readonly GridCoord C = new GridCoord(2, 0);
        static readonly GridCoord North = new GridCoord(0, 1);

        [Test]
        public void PressOnANeighbourWithoutMovingDoesNotTakeIt()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, B);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out _), Is.False);
        }

        [Test]
        public void SlidingOntoTheNextOptionTakesThatTile()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, B);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out var target), Is.True);
            Assert.That(target, Is.EqualTo(B));
        }

        [Test]
        public void SlidingAlongSeveralOptionsYieldsThemInOrder()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, C);

            var taken = new List<GridCoord>();
            var current = A;
            while (drag.TryTake(current, cell => cell.Equals(current.Offset(1, 0)), out var next))
            {
                taken.Add(next);
                current = next;
            }

            Assert.That(taken, Is.EqualTo(new[] { B, C }));
        }

        [Test]
        public void ANonOptionTileStopsTheGesture()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, B);
            drag.Feed(true, false, true, North);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out var first), Is.True);
            Assert.That(first, Is.EqualTo(B));
            Assert.That(drag.TryTake(B, cell => cell.Equals(C), out _), Is.False);
            Assert.That(drag.TryTake(B, cell => cell.Equals(C), out _), Is.False);
        }

        [Test]
        public void LiftingLetsANewSlideStart()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, North);
            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out _), Is.False);

            drag.Feed(false, false, false, default);
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, B);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out var target), Is.True);
            Assert.That(target, Is.EqualTo(B));
        }

        [Test]
        public void StopEndsTheCurrentSlideUntilLift()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            drag.Feed(true, false, true, B);
            drag.Stop();

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out _), Is.False);

            drag.Feed(true, false, true, C);
            Assert.That(drag.TryTake(B, cell => cell.Equals(C), out _), Is.False);
        }

        [Test]
        public void PressOnNextTileThenSlideOnwardTakesBoth()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, B);
            drag.Feed(true, false, true, C);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out var first), Is.True);
            Assert.That(first, Is.EqualTo(B));
            Assert.That(drag.TryTake(B, cell => cell.Equals(C), out var second), Is.True);
            Assert.That(second, Is.EqualTo(C));
        }

        [Test]
        public void LeavingThePressTileSuppressesASwipeForThatGesture()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, false, true, A);
            Assert.That(drag.IgnoreStroke, Is.False);

            drag.Feed(true, false, true, B);
            Assert.That(drag.IgnoreStroke, Is.True);
        }

        [Test]
        public void PressStartingOnUiDoesNotTrack()
        {
            var drag = new PathDragTracker();
            drag.Feed(true, true, true, A);
            drag.Feed(true, false, true, B);

            Assert.That(drag.TryTake(A, cell => cell.Equals(B), out _), Is.False);
        }
    }
}
