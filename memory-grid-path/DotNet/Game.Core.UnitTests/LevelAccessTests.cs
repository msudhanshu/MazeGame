using System;
using Game.Core.Domain;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class LevelAccessTests
    {
        [Test]
        public void FreshProgressHasLevelOneCurrentAndTheRestLocked()
        {
            var progress = new PlayerProgress();

            Assert.That(LevelAccess.Lane(progress, 1), Is.EqualTo(LevelLane.Current));
            Assert.That(LevelAccess.Lane(progress, 2), Is.EqualTo(LevelLane.Locked));
            Assert.That(LevelAccess.StarsOn(LevelLane.Current), Is.EqualTo(0));
            Assert.That(LevelAccess.StarsEarned(progress), Is.EqualTo(0));
        }

        [Test]
        public void ClearingALevelMovesTheCurrentLaneForwardAndAwardsStars()
        {
            var progress = new PlayerProgress();
            progress.RecordResult(1, 900, completed: true, levelCount: 20);

            Assert.That(LevelAccess.Lane(progress, 1), Is.EqualTo(LevelLane.Cleared));
            Assert.That(LevelAccess.Lane(progress, 2), Is.EqualTo(LevelLane.Current));
            Assert.That(LevelAccess.StarsOn(LevelLane.Cleared), Is.EqualTo(3));
            Assert.That(LevelAccess.StarsEarned(progress), Is.EqualTo(3));
            Assert.That(LevelAccess.StarsPossible(20), Is.EqualTo(60));
        }

        [Test]
        public void TitlesStayInRange()
        {
            Assert.That(LevelTitles.Headline(2), Is.EqualTo("Level 2: Lost Forest"));
            Assert.That(LevelTitles.Name(12), Is.EqualTo(LevelTitles.Name(2)));
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelTitles.Name(0));
        }
    }
}
