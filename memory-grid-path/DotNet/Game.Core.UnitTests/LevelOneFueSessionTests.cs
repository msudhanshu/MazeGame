using Game.Core.Fue;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class LevelOneFueSessionTests
    {
        [Test]
        public void StartsOnEarlyTileArenaLevelsWhenUnseen()
        {
            Assert.That(LevelOneFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: false), Is.Not.Null);
            Assert.That(LevelOneFueSession.TryStart(GameModeId.TileArena, 2, alreadySeen: false), Is.Not.Null);
            Assert.That(LevelOneFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: true), Is.Null);
            Assert.That(LevelOneFueSession.TryStart(GameModeId.TileArena, 3, alreadySeen: false), Is.Null);
            Assert.That(LevelOneFueSession.TryStart(GameModeId.TileArena, 5, alreadySeen: false), Is.Null);
            Assert.That(LevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false), Is.Null);
            Assert.That(LevelOneFueSpec.LessonIdFor(2), Is.EqualTo("memory-path.tile-early.2"));
            Assert.That(LevelOneFueSpec.MaxLevel, Is.EqualTo(OpeningCardSpec.MaxLevel));
        }

        [Test]
        public void CompletesAfterFirstCorrectStep()
        {
            var session = LevelOneFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: false);

            session.OnCorrectStep();

            Assert.That(session.IsComplete, Is.True);
            Assert.That(session.IsActive, Is.False);
            Assert.That(session.Beat, Is.EqualTo(LevelOneFueBeat.Completed));
        }

        [Test]
        public void WrongStepShowsHealthHintBeforeCompletion()
        {
            var session = LevelOneFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: false);

            session.OnWrongStep();

            Assert.That(session.Beat, Is.EqualTo(LevelOneFueBeat.HealthHint));
            Assert.That(session.IsActive, Is.True);

            session.OnCorrectStep();

            Assert.That(session.IsComplete, Is.True);
        }
    }
}
