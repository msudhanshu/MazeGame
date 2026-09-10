using Game.Unity.Themes.Experimental;
using NUnit.Framework;
using Nixin.Grid.Core;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class PatchworkTexturePickerTests
    {
        [Test]
        public void SameCellAlwaysPicksTheSameTexture()
        {
            var coord = new GridCoord(2, 3);
            var first = PatchworkTexturePicker.PickIndex(coord, 5, 99);
            var second = PatchworkTexturePicker.PickIndex(coord, 5, 99);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void DifferentCellsUsuallyPickDifferentTextures()
        {
            var a = PatchworkTexturePicker.PickIndex(new GridCoord(0, 0), 8, 42);
            var b = PatchworkTexturePicker.PickIndex(new GridCoord(1, 0), 8, 42);
            var c = PatchworkTexturePicker.PickIndex(new GridCoord(0, 1), 8, 42);

            Assert.That(a, Is.InRange(0, 7));
            Assert.That(b, Is.InRange(0, 7));
            Assert.That(c, Is.InRange(0, 7));
            Assert.That(new[] { a, b, c }, Is.Not.All.EqualTo(a));
        }

        [Test]
        public void EmptyPoolFallsBackToZero()
        {
            Assert.That(PatchworkTexturePicker.PickIndex(new GridCoord(1, 1), 0, 7), Is.EqualTo(0));
        }
    }
}
