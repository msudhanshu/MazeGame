using System;
using System.Collections.Generic;
using Game.Core.Domain;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class OffPathHintPlacerTests
    {
        static readonly GridSize Size = new GridSize(5, 5);
        static readonly IRandomSource Random = new XorShiftRandom(42);

        static GridPath PathOfLength(int cells)
        {
            var route = new List<GridCoord>(cells) { new GridCoord(0, 0) };
            var x = 0;
            var y = 0;
            var dir = 1;
            while (route.Count < cells)
            {
                var nextX = x + dir;
                if (nextX < 0 || nextX >= Size.Width)
                {
                    y++;
                    dir = -dir;
                    route.Add(new GridCoord(x, y));
                    continue;
                }

                x = nextX;
                route.Add(new GridCoord(x, y));
            }

            return new GridPath(Size, route);
        }

        static LevelDefinition Level(int blockedHints = 0) =>
            new LevelDefinition(
                1,
                Size,
                new GridCoord(0, 0),
                new GridCoord(4, 4),
                new PathShapeSpec(5, 20, 0, 12),
                "test",
                blockedHintCount: blockedHints);

        [Test]
        public void ZeroCountPlacesNothing()
        {
            var blocked = OffPathHintPlacer.Place(PathOfLength(9), Level(), Random);

            Assert.That(blocked, Is.Empty);
        }

        [Test]
        public void BlockedCellsAreOffPath()
        {
            var path = PathOfLength(9);
            var blocked = OffPathHintPlacer.Place(path, Level(blockedHints: 4), Random);

            Assert.That(blocked.Count, Is.GreaterThan(0));
            foreach (var cell in blocked)
                Assert.That(path.Contains(cell), Is.False);
        }

        [Test]
        public void BlockedCellsKeepEveryPromptPlayable()
        {
            var path = PathOfLength(9);
            var blocked = OffPathHintPlacer.Place(path, Level(blockedHints: 6), Random);
            var blockedSet = new HashSet<GridCoord>(blocked);

            var prompts = GridPrompts.FromPath(path, blocked: blockedSet);
            foreach (var prompt in prompts)
                Assert.That(prompt.Choices.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void RejectsMissingArguments()
        {
            var path = PathOfLength(5);
            var level = Level(blockedHints: 2);

            Assert.Throws<ArgumentNullException>(() => OffPathHintPlacer.Place(null, level, Random));
            Assert.Throws<ArgumentNullException>(() => OffPathHintPlacer.Place(path, null, Random));
            Assert.Throws<ArgumentNullException>(() => OffPathHintPlacer.Place(path, level, null));
        }
    }
}
