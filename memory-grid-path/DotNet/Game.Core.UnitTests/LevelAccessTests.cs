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
            Assert.That(LevelAccess.StarsOn(progress, 1), Is.EqualTo(3));
            Assert.That(LevelAccess.StarsOn(progress, 2), Is.EqualTo(0));
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

        [Test]
        public void StarsFromMistakesDropWithMoreErrors()
        {
            Assert.That(LevelAccess.StarsFromMistakes(0), Is.EqualTo(3));
            Assert.That(LevelAccess.StarsFromMistakes(1), Is.EqualTo(2));
            Assert.That(LevelAccess.StarsFromMistakes(2), Is.EqualTo(1));
            Assert.That(LevelAccess.StarsFromMistakes(9), Is.EqualTo(1));
        }

        [Test]
        public void LastRunStarsOverwriteAndLegacyClearsStayAtThree()
        {
            var progress = new PlayerProgress();
            progress.RecordResult(1, 900, completed: true, levelCount: 20, mistakes: 0);
            progress.RecordResult(1, 300, completed: true, levelCount: 20, mistakes: 2);

            Assert.That(progress.BestScoreFor(1), Is.EqualTo(900));
            Assert.That(progress.LastStarsFor(1), Is.EqualTo(1));
            Assert.That(LevelAccess.StarsOn(progress, 1), Is.EqualTo(1));
            Assert.That(LevelAccess.StarsEarned(progress), Is.EqualTo(1));

            progress.RecordResult(1, 10, completed: false, levelCount: 20, mistakes: 6);
            Assert.That(progress.LastStarsFor(1), Is.EqualTo(1));
        }
    }
}
