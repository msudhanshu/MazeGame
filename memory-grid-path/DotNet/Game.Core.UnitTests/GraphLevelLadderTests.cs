using Game.Core.Domain;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GraphLevelLadderTests
    {
        [Test]
        public void FirstLevelsStayOnTheShortRealRouteAndSlowRadar()
        {
            var one = GraphLevelLadder.For(1);
            Assert.That(one.MinPath, Is.EqualTo(8));
            Assert.That(one.MaxPath, Is.EqualTo(9));
            Assert.That(one.MaxTurns, Is.EqualTo(4));
            Assert.That(one.PreviewKind, Is.EqualTo(PathPreviewKind.Radar));
            Assert.That(one.PreviewSeconds, Is.GreaterThan(3f));
            Assert.That(one.HoldSeconds, Is.GreaterThan(1.4f));

            var two = GraphLevelLadder.For(2);
            Assert.That(two.MinPath, Is.GreaterThan(one.MinPath));
            Assert.That(two.MaxTurns, Is.GreaterThanOrEqualTo(one.MaxTurns));
            Assert.That(two.PreviewKind, Is.EqualTo(PathPreviewKind.Radar));
            Assert.That(two.PreviewSeconds, Is.GreaterThan(3f));
        }

        [Test]
        public void LevelsThreeToFiveStaySimpleAndRadarIsSlowThroughFive()
        {
            for (var level = 3; level <= 5; level++)
            {
                var spec = GraphLevelLadder.For(level);
                Assert.That(spec.MaxTurns, Is.LessThanOrEqualTo(8), "level " + level);
                Assert.That(spec.PreviewKind, Is.EqualTo(PathPreviewKind.Radar), "level " + level);
                Assert.That(spec.PreviewSeconds, Is.GreaterThan(2f), "level " + level);
                Assert.That(spec.HoldSeconds, Is.GreaterThan(0.8f), "level " + level);
            }

            Assert.That(GraphLevelLadder.For(1).PreviewSeconds, Is.GreaterThan(GraphLevelLadder.For(5).PreviewSeconds));
            Assert.That(GraphLevelLadder.For(5).PreviewSeconds, Is.GreaterThan(GraphLevelLadder.For(6).PreviewSeconds));
            Assert.That(GraphLevelLadder.For(1).MinPath, Is.LessThan(GraphLevelLadder.For(5).MinPath));
        }

        [Test]
        public void CameraFlashStartsAfterLevelSevenWithALongGlance()
        {
            for (var level = 1; level <= GraphLevelLadder.LastRadarLevel; level++)
                Assert.That(GraphLevelLadder.UsesRadar(level), Is.True, "level " + level);

            var flash = GraphLevelLadder.For(8);
            Assert.That(flash.IsFlash, Is.True);
            Assert.That(flash.PreviewSeconds, Is.GreaterThan(GraphPathPreviewSeconds.FlashLongest));
            Assert.That(GraphLevelLadder.Count, Is.EqualTo(8));
            Assert.That(GraphLevelLadder.For(99).IsFlash, Is.True);
        }
    }
}
