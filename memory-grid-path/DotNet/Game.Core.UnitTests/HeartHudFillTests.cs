using Game.Core.Rules;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class HeartHudFillTests
    {
        [Test]
        public void SpentAmountGrowsFromTheBottomAsLivesDrop()
        {
            Assert.That(HeartHudFill.SpentAmount(3, 3), Is.EqualTo(0f));
            Assert.That(HeartHudFill.SpentAmount(2, 3), Is.EqualTo(1f / 3f).Within(0.0001f));
            Assert.That(HeartHudFill.SpentAmount(1, 3), Is.EqualTo(2f / 3f).Within(0.0001f));
            Assert.That(HeartHudFill.SpentAmount(0, 3), Is.EqualTo(1f));
        }

        [Test]
        public void RemainingGlowDimsWithTheCurrentHeart()
        {
            Assert.That(HeartHudFill.RemainingGlow(3, 3), Is.EqualTo(1f));
            Assert.That(HeartHudFill.RemainingGlow(1, 3), Is.EqualTo(1f / 3f).Within(0.0001f));
            Assert.That(HeartHudFill.RemainingGlow(0, 3), Is.EqualTo(0f));
        }
    }
}
