using System;
using Game.Core.Rules;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class SessionHealthBarsTests
    {
        [Test]
        public void EarlierWalksAreSpentAndLaterWalksStayLit()
        {
            Assert.That(SessionHealthBars.Segment(0, 0, currentRun: 3, livesLeft: 2, livesPerRun: 3, runsPerSession: 5),
                Is.EqualTo(HealthSegmentKind.Spent));
            Assert.That(SessionHealthBars.Segment(3, 1, currentRun: 3, livesLeft: 2, livesPerRun: 3, runsPerSession: 5),
                Is.EqualTo(HealthSegmentKind.Remaining));
        }

        [Test]
        public void CurrentWalkShowsRemainingHeartsFromTheLeft()
        {
            Assert.That(SessionHealthBars.Segment(2, 0, currentRun: 3, livesLeft: 2, livesPerRun: 3, runsPerSession: 5),
                Is.EqualTo(HealthSegmentKind.Remaining));
            Assert.That(SessionHealthBars.Segment(2, 1, currentRun: 3, livesLeft: 2, livesPerRun: 3, runsPerSession: 5),
                Is.EqualTo(HealthSegmentKind.Remaining));
            Assert.That(SessionHealthBars.Segment(2, 2, currentRun: 3, livesLeft: 2, livesPerRun: 3, runsPerSession: 5),
                Is.EqualTo(HealthSegmentKind.Spent));
        }

        [Test]
        public void LastLifeOfAWalkDarkensTheWholeBar()
        {
            Assert.That(SessionHealthBars.Segment(0, 0, currentRun: 1, livesLeft: 0, livesPerRun: 3, runsPerSession: 2),
                Is.EqualTo(HealthSegmentKind.Spent));
            Assert.That(SessionHealthBars.Segment(0, 2, currentRun: 1, livesLeft: 0, livesPerRun: 3, runsPerSession: 2),
                Is.EqualTo(HealthSegmentKind.Spent));
        }

        [Test]
        public void RejectsIncoherentInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SessionHealthBars.Segment(5, 0, 1, 3, 3, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SessionHealthBars.Segment(0, 3, 1, 3, 3, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SessionHealthBars.Segment(0, 0, 0, 3, 3, 5));
        }
    }
}
