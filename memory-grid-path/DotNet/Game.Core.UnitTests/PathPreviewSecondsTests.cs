using Game.Core.Domain;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class PathPreviewSecondsTests
    {
        [Test]
        public void SweepFollowsHeightAndTurnsInsideClamps()
        {
            Assert.That(PathPreviewSeconds.For(4, 4), Is.EqualTo(PathPreviewSeconds.Shortest).Within(0.001f));
            Assert.That(PathPreviewSeconds.For(9, 17), Is.EqualTo(PathPreviewSeconds.Longest).Within(0.001f));
            Assert.That(PathPreviewSeconds.For(6, 8), Is.GreaterThan(PathPreviewSeconds.Shortest));
            Assert.That(PathPreviewSeconds.For(6, 8), Is.LessThan(PathPreviewSeconds.Longest));
        }

        [Test]
        public void CatalogLevelsStayInsideTheGlanceWindow()
        {
            Assert.That(PathPreviewSeconds.ForLevel(1), Is.GreaterThan(PathPreviewSeconds.Shortest));
            Assert.That(PathPreviewSeconds.ForLevel(1), Is.GreaterThan(PathPreviewSeconds.ForLevel(8)));
            Assert.That(PathPreviewSeconds.ForLevel(1), Is.LessThanOrEqualTo(PathPreviewSeconds.TeachingLongest));
            Assert.That(PathPreviewSeconds.ForLevel(25), Is.EqualTo(PathPreviewSeconds.Longest).Within(0.001f));
            Assert.That(PathPreviewSeconds.For(new LevelCatalog().Get(1)),
                Is.EqualTo(PathPreviewSeconds.ForLevel(1)).Within(0.001f));
        }

        [Test]
        public void EarlyLevelsHoldThePathAfterTheSweep()
        {
            Assert.That(PathPreviewSeconds.HoldFor(1), Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(PathPreviewSeconds.HoldFor(2), Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(PathPreviewSeconds.HoldFor(3), Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(PathPreviewSeconds.HoldFor(5), Is.EqualTo(0.4f).Within(0.001f));
            Assert.That(PathPreviewSeconds.HoldFor(11), Is.EqualTo(PathPreviewSeconds.Hold).Within(0.001f));
            Assert.That(PathPreviewSeconds.HoldFor(new LevelCatalog().Get(1)),
                Is.EqualTo(PathPreviewSeconds.HoldFor(1)).Within(0.001f));
        }

        [Test]
        public void HoldIsAShortBeatAfterTheSweepOnLateLevels()
        {
            Assert.That(PathPreviewSeconds.Hold, Is.EqualTo(0.12f).Within(0.001f));
        }

        [Test]
        public void TileAndGraphScanPauseStartsAfterLevelTen()
        {
            Assert.That(PathPreviewSeconds.UsesScanPause(1), Is.False);
            Assert.That(PathPreviewSeconds.UsesScanPause(10), Is.False);
            Assert.That(PathPreviewSeconds.UsesScanPause(11), Is.True);
            Assert.That(PathPreviewSeconds.UsesScanPause(new LevelCatalog().Get(10)), Is.False);
        }
    }
}
