using System;
using Game.Core;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class CampaignCatalogTests
    {
        [Test]
        public void DefaultCampaignHasTwelveMonotonicLevels()
        {
            var catalog = CampaignCatalog.Default();
            Assert.That(catalog.Count, Is.EqualTo(12));

            for (var i = 1; i < catalog.Count; i++)
            {
                var previous = catalog.Get(i - 1);
                var next = catalog.Get(i);
                Assert.That((int)next.Difficulty, Is.GreaterThanOrEqualTo((int)previous.Difficulty));
                Assert.That(next.BraidFactor, Is.LessThanOrEqualTo(previous.BraidFactor));
                Assert.That(
                    next.CellCount >= previous.CellCount || (int)next.Difficulty > (int)previous.Difficulty,
                    Is.True,
                    "Level " + i + " must grow in size or difficulty.");
            }
        }

        [Test]
        public void CampaignSpecsUseRandomOppositeOpenings()
        {
            var spec = CampaignCatalog.Default().Get(0).ToSpec();
            Assert.That(spec.Openings, Is.EqualTo(OpeningPlacement.RandomOpposite));
            Assert.That(spec.Size, Is.EqualTo(new GridSize(6, 6)));
            Assert.That(spec.Difficulty, Is.EqualTo(MazeDifficulty.Easy));
        }

        [Test]
        public void SameLevelTwoSeedsYieldDifferentLayoutsAndTheSameSpec()
        {
            var spec = CampaignCatalog.Default().Get(3).ToSpec();
            var first = DifficultyTunedMazeGenerator.Generate(spec, new XorShiftRandom(11));
            var second = DifficultyTunedMazeGenerator.Generate(spec, new XorShiftRandom(29));

            Assert.That(first.LayoutEquals(second), Is.False);
            Assert.That(first.Size, Is.EqualTo(spec.Size));
            Assert.That(second.Size, Is.EqualTo(spec.Size));
            Assert.That(MazeTimeBudget.SecondsFor(spec), Is.EqualTo(MazeTimeBudget.SecondsFor(spec)));
        }

        [Test]
        public void EasyBudgetOnTheSameSizeIsMoreGenerousThanHard()
        {
            var size = new GridSize(8, 8);
            var easy = MazeSpec.For(size, MazeDifficulty.Easy);
            var hard = MazeSpec.For(size, MazeDifficulty.Hard);
            Assert.That(MazeTimeBudget.SecondsFor(easy), Is.GreaterThan(MazeTimeBudget.SecondsFor(hard)));
        }

        [Test]
        public void RejectsEmptyCatalogs()
        {
            Assert.Throws<ArgumentException>(() => new CampaignCatalog(Array.Empty<CampaignLevel>()));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CampaignLevel("Tiny", 1, 4, MazeDifficulty.Easy, 0f));
        }
    }
}
