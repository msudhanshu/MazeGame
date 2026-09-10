using System.Linq;
using Game.Unity.Data;
using Game.Unity.Editor;
using Game.Unity.Themes.Experimental;
using NUnit.Framework;

namespace Game.Unity.Tests
{
    public sealed class JourneyCatalogBuilderTests
    {
        [Test]
        public void TileArenaShipsTwentyLevelsWithAlternatingVisuals()
        {
            var catalog = JourneyCatalogBuilder.CreateOrUpdate();
            var tile = catalog.TileArena;
            Assert.That(tile.Count, Is.EqualTo(JourneyCatalogBuilder.TileArenaLevelCount));

            for (var i = 0; i < tile.Count; i++)
            {
                var entry = tile.Levels[i];
                Assert.That(entry.IsGrid, Is.True, "level " + (i + 1));
                var mosaicLevel = i % 2 == 1;
                Assert.That(
                    entry.VisualType,
                    Is.EqualTo(mosaicLevel ? ArenaVisualType.MosaicImage : ArenaVisualType.ClassicDanceFloor),
                    "level " + (i + 1));
                if (mosaicLevel)
                {
                    Assert.That(entry.MosaicTexture, Is.Not.Null, "level " + (i + 1));
                    Assert.That(entry.ThumbnailTexture, Is.SameAs(entry.MosaicTexture), "level " + (i + 1));
                }
                else
                {
                    Assert.That(entry.MosaicTexture, Is.Null, "level " + (i + 1));
                    Assert.That(entry.ThumbnailTexture, Is.Null, "level " + (i + 1));
                }
            }

            var mosaicLevels = Enumerable.Range(0, tile.Count)
                .Where(i => i % 2 == 1)
                .Select(i => tile.Levels[i])
                .ToArray();
            Assert.That(mosaicLevels.Select(l => l.MosaicTexture).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(mosaicLevels.Any(l => l.MosaicBackgroundParticles), Is.True);
            Assert.That(mosaicLevels.Any(l => !l.MosaicBackgroundParticles), Is.True);
        }

        [Test]
        public void ScoutArenaUsesACloserConfigurableFollowZoom()
        {
            var catalog = JourneyCatalogBuilder.CreateOrUpdate();
            var scout = catalog.ScoutArena;

            Assert.That(scout.CameraMode, Is.EqualTo(ArenaCameraMode.FollowWalker));
            Assert.That(scout.FollowOrthographicSize, Is.EqualTo(1.8f).Within(0.01f));
            Assert.That(scout.ResolvedFollowOrthographicSize, Is.LessThan(3.2f));
            Assert.That(scout.WalkerScale, Is.EqualTo(0.55f).Within(0.01f));
            Assert.That(scout.WalkerScaleFor(scout.Get(1)), Is.EqualTo(0.55f).Within(0.01f));
        }
    }
}
