using System;
using Game.Core;
using NUnit.Framework;
using Nixin.Maze.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class MazePresetCatalogTests
    {
        [Test]
        public void DefaultCatalogHasNamedSizeAndDifficultyCombinations()
        {
            var catalog = MazePresetCatalog.Default();

            Assert.That(catalog.All.Count, Is.EqualTo(4));
            Assert.That(catalog.Get("Garden").Width, Is.EqualTo(8));
            Assert.That(catalog.Get("Keep").Difficulty, Is.EqualTo(MazeDifficulty.Hard));
            Assert.That(catalog.Get("Labyrinth").BraidFactor, Is.EqualTo(0f));
        }

        [Test]
        public void ToSpecMatchesThePreset()
        {
            var preset = new MazePreset("Yard", 5, 7, MazeDifficulty.Easy, 0.4f, 3);
            var spec = preset.ToSpec();

            Assert.That(spec.Size.Width, Is.EqualTo(5));
            Assert.That(spec.Size.Height, Is.EqualTo(7));
            Assert.That(spec.Difficulty, Is.EqualTo(MazeDifficulty.Easy));
            Assert.That(spec.BraidFactor, Is.EqualTo(0.4f));
        }

        [Test]
        public void RejectsDuplicatesUnknownNamesAndTinyBoards()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MazePreset("Tiny", 1, 4, MazeDifficulty.Easy, 0f, 1));
            Assert.Throws<ArgumentException>(() =>
                new MazePresetCatalog(new[]
                {
                    new MazePreset("A", 4, 4, MazeDifficulty.Easy, 0f, 1),
                    new MazePreset("A", 6, 6, MazeDifficulty.Easy, 0f, 2)
                }));
            Assert.Throws<ArgumentException>(() => MazePresetCatalog.Default().Get("missing"));
        }
    }
}
