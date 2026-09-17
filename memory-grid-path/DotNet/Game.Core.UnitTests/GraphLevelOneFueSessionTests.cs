using Game.Core.Fue;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class GraphLevelOneFueSessionTests
    {
        [Test]
        public void StartsOnlyForFirstGraphArenaLevelWhenUnseen()
        {
            Assert.That(GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false), Is.Not.Null);
            Assert.That(GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: true), Is.Null);
            Assert.That(GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 2, alreadySeen: false), Is.Null);
            Assert.That(GraphLevelOneFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: false), Is.Null);
        }

        [Test]
        public void StartsOnTapPromptAndKeepsCoachingUntilTheGoal()
        {
            var session = GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false);

            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptTap));

            session.OnCorrectStep(reachedGoal: false);

            Assert.That(session.IsComplete, Is.False);
            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptTap));

            session.OnCorrectStep(reachedGoal: true);

            Assert.That(session.IsComplete, Is.True);
        }

        [Test]
        public void WrongStepShowsHealthHintThenReturnsToTap()
        {
            var session = GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false);

            session.OnWrongStep();

            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.HealthHint));
            Assert.That(session.IsActive, Is.True);

            session.OnCorrectStep(reachedGoal: false);

            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptTap));
        }
    }
}
