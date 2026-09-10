using Game.Unity.Themes.Experimental;
using NUnit.Framework;
using Nixin.Grid.Core;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class ArenaTextureMappingTests
    {
        [Test]
        public void CornerTileMapsToTextureCorner()
        {
            var size = new GridSize(4, 3);
            var region = ArenaTextureMapping.ForTile(new GridCoord(0, 0), size);

            Assert.That(region.MinU, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(region.MinV, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(region.MaxU, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(region.MaxV, Is.EqualTo(1f / 3f).Within(0.0001f));
        }

        [Test]
        public void OppositeCornerMapsToFarTextureCorner()
        {
            var size = new GridSize(4, 3);
            var region = ArenaTextureMapping.ForTile(new GridCoord(3, 2), size);

            Assert.That(region.MinU, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(region.MinV, Is.EqualTo(2f / 3f).Within(0.0001f));
            Assert.That(region.MaxU, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(region.MaxV, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void VerticalFlipSwapsVAxis()
        {
            var size = new GridSize(2, 2);
            var normal = ArenaTextureMapping.ForTile(new GridCoord(0, 1), size);
            var flipped = ArenaTextureMapping.ForTile(new GridCoord(0, 1), size, flipVertical: true);

            Assert.That(flipped.MinU, Is.EqualTo(normal.MinU));
            Assert.That(flipped.MaxU, Is.EqualTo(normal.MaxU));
            Assert.That(flipped.MinV, Is.EqualTo(1f - normal.MaxV).Within(0.0001f));
            Assert.That(flipped.MaxV, Is.EqualTo(1f - normal.MinV).Within(0.0001f));
        }

        [Test]
        public void AdjacentTilesShareEdgesWithoutOverlap()
        {
            var size = new GridSize(5, 5);
            var left = ArenaTextureMapping.ForTile(new GridCoord(1, 2), size);
            var right = ArenaTextureMapping.ForTile(new GridCoord(2, 2), size);

            Assert.That(left.MaxU, Is.EqualTo(right.MinU).Within(0.0001f));
            Assert.That(left.MinV, Is.EqualTo(right.MinV).Within(0.0001f));
            Assert.That(left.MaxV, Is.EqualTo(right.MaxV).Within(0.0001f));
        }
    }
}
