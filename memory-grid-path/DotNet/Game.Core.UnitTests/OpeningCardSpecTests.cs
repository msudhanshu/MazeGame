using Game.Core.Fue;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class OpeningCardSpecTests
    {
        [Test]
        public void ShowsOnTheFirstFiveLevels()
        {
            Assert.That(OpeningCardSpec.ShouldShow(1), Is.True);
            Assert.That(OpeningCardSpec.ShouldShow(5), Is.True);
            Assert.That(OpeningCardSpec.ShouldShow(6), Is.False);
            Assert.That(OpeningCardSpec.ShouldShow(0), Is.False);
        }

        [Test]
        public void LessonIdsArePerLevel()
        {
            Assert.That(OpeningCardSpec.LessonIdFor(1), Is.EqualTo("memory-path.opening-card.1"));
            Assert.That(OpeningCardSpec.LessonIdFor(5), Is.Not.EqualTo(OpeningCardSpec.LessonIdFor(4)));
        }
    }
}
