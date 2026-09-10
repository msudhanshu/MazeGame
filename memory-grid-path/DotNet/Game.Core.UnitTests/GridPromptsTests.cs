using System;
using System.Collections.Generic;
using Game.Core.Domain;
using NUnit.Framework;
using Nixin.Grid.Core;
using Nixin.Memory.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GridPromptsTests
    {
        static readonly GridSize Size = new GridSize(5, 5);

        static GridPath Path(params (int x, int y)[] pairs)
        {
            var cells = new List<GridCoord>(pairs.Length);
            foreach (var pair in pairs)
                cells.Add(new GridCoord(pair.x, pair.y));
            return new GridPath(Size, cells);
        }

        [Test]
        public void OnePromptPerStep()
        {
            var path = Path((0, 0), (1, 0), (1, 1), (1, 2));

            var prompts = GridPrompts.FromPath(path);

            Assert.That(prompts.Count, Is.EqualTo(path.StepCount));
            Assert.That(prompts[0].Id, Is.EqualTo(new PromptId("0,0")));
            Assert.That(prompts[2].Id, Is.EqualTo(new PromptId("1,1")));
        }

        [Test]
        public void TheOnlyCorrectChoiceIsTheNextPathCell()
        {
            var path = Path((0, 0), (1, 0), (1, 1));

            var prompts = GridPrompts.FromPath(path);

            Assert.That(prompts[0].CorrectTokens, Is.EqualTo(new[] { new TokenId("1,0") }));
            Assert.That(prompts[1].CorrectTokens, Is.EqualTo(new[] { new TokenId("1,1") }));

            foreach (var prompt in prompts)
            {
                var correct = 0;
                foreach (var choice in prompt.Choices)
                {
                    if (prompt.IsCorrect(choice))
                        correct++;
                }
                Assert.That(correct, Is.EqualTo(1));
            }
        }

        [Test]
        public void ChoicesAreTheNeighboursOfTheCurrentCell()
        {
            var path = Path((0, 0), (1, 0), (1, 1));

            var prompts = GridPrompts.FromPath(path);

            Assert.That(prompts[0].Choices, Is.EquivalentTo(new[] { new TokenId("1,0"), new TokenId("0,1") }));
        }

        [Test]
        public void EveryNeighbourIsOfferedIncludingTheTileJustLeft()
        {
            var path = Path((0, 0), (1, 0), (1, 1));

            var prompts = GridPrompts.FromPath(path);

            Assert.That(prompts[1].Choices, Is.EquivalentTo(new[]
            {
                new TokenId("0,0"), new TokenId("2,0"), new TokenId("1,1")
            }));
        }

        [Test]
        public void StepBackCanBeHiddenWhenAsked()
        {
            var path = Path((0, 0), (1, 0), (1, 1));

            var prompts = GridPrompts.FromPath(path, allowStepBack: false);

            Assert.That(prompts[1].Choices, Does.Not.Contain(new TokenId("0,0")));
            Assert.That(prompts[1].Choices, Is.EquivalentTo(new[] { new TokenId("2,0"), new TokenId("1,1") }));
        }

        [Test]
        public void APathThatVisitsACornerStillHasTwoChoices()
        {
            // Up the left edge into the far corner, then across. Hiding the previous tile
            // would leave (0,4) with one neighbour, which used to crash level start.
            var path = Path((0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (1, 4), (2, 4));

            IReadOnlyList<Prompt> prompts = null;
            Assert.DoesNotThrow(() => prompts = GridPrompts.FromPath(path));
            Assert.That(prompts, Is.Not.Null);

            var corner = prompts[4];
            Assert.That(corner.Id, Is.EqualTo(new PromptId("0,4")));
            Assert.That(corner.Choices.Count, Is.EqualTo(2));
            Assert.That(corner.IsCorrect(new TokenId("1,4")), Is.True);
        }

        [Test]
        public void TokensRoundTripToCoordinates()
        {
            var coord = new GridCoord(3, 4);

            Assert.That(GridPrompts.ToCoord(GridPrompts.ToToken(coord)), Is.EqualTo(coord));
            Assert.Throws<ArgumentException>(() => GridPrompts.ToCoord(new TokenId("not-a-cell")));
            Assert.Throws<ArgumentNullException>(() => GridPrompts.FromPath(null));
        }

        [Test]
        public void BlockedNeighboursAreNotOffered()
        {
            var path = Path((0, 0), (1, 0), (1, 1));
            var blocked = new HashSet<GridCoord> { new GridCoord(2, 0) };

            var prompts = GridPrompts.FromPath(path, blocked: blocked);

            Assert.That(prompts[1].Choices, Is.EquivalentTo(new[] { new TokenId("0,0"), new TokenId("1,1") }));
            Assert.That(prompts[1].IsCorrect(new TokenId("1,1")), Is.True);
        }

        [Test]
        public void TooManyBlockedCellsAreRejected()
        {
            var path = Path((0, 0), (1, 0), (1, 1));
            var blocked = new HashSet<GridCoord> { new GridCoord(0, 1), new GridCoord(1, 0) };

            Assert.Throws<ArgumentException>(() => GridPrompts.FromPath(path, blocked: blocked));
        }
    }
}
