using Game.Core.Domain;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GraphPathPreviewSecondsTests
    {
        [Test]
        public void AuthoredAboveBurstWins()
        {
            Assert.That(
                GraphPathPreviewSeconds.Resolve(0.28f, 18, 12, PathPreviewKind.CameraFlash),
                Is.EqualTo(0.28f).Within(0.001f));
        }

        [Test]
        public void ZeroAndBurstUseAutoWindow()
        {
            var auto = GraphPathPreviewSeconds.For(8, 4, PathPreviewKind.CameraFlash);
            Assert.That(GraphPathPreviewSeconds.Resolve(0f, 8, 4, PathPreviewKind.CameraFlash),
                Is.EqualTo(auto).Within(0.001f));
            Assert.That(GraphPathPreviewSeconds.Resolve(GraphPathPreviewSeconds.FlashBurst, 8, 4, PathPreviewKind.CameraFlash),
                Is.EqualTo(auto).Within(0.001f));
        }

        [Test]
        public void FlashStaysInsideTheBlinkWindowAndGetsShorterOnHarderGraphs()
        {
            Assert.That(GraphPathPreviewSeconds.For(2, 0, PathPreviewKind.CameraFlash),
                Is.EqualTo(GraphPathPreviewSeconds.FlashLongest).Within(0.001f));
            Assert.That(GraphPathPreviewSeconds.For(40, 40, PathPreviewKind.CameraFlash),
                Is.EqualTo(GraphPathPreviewSeconds.FlashShortest).Within(0.001f));
            Assert.That(GraphPathPreviewSeconds.For(8, 3, PathPreviewKind.CameraFlash),
                Is.GreaterThan(GraphPathPreviewSeconds.For(16, 10, PathPreviewKind.CameraFlash)));
        }

        [Test]
        public void RadarStaysInsideItsWindowAndIsLongerThanFlash()
        {
            Assert.That(GraphPathPreviewSeconds.For(2, 0, PathPreviewKind.Radar),
                Is.EqualTo(GraphPathPreviewSeconds.RadarLongest).Within(0.001f));
            Assert.That(GraphPathPreviewSeconds.For(40, 40, PathPreviewKind.Radar),
                Is.EqualTo(GraphPathPreviewSeconds.RadarShortest).Within(0.001f));
            Assert.That(GraphPathPreviewSeconds.For(8, 4, PathPreviewKind.CameraFlash),
                Is.LessThan(GraphPathPreviewSeconds.For(8, 4, PathPreviewKind.Radar)));
        }

        [Test]
        public void BurstIsAFiftiethOfASecond()
        {
            Assert.That(GraphPathPreviewSeconds.FlashBurst, Is.EqualTo(0.05f).Within(0.001f));
        }
    }
}
