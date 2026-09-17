using Game.Core.Fue;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class ScoutScanFueSessionTests
    {
        [Test]
        public void StartsOnlyForFirstUnseenScoutLevel()
        {
            Assert.That(ScoutScanFueSession.TryStart(GameModeId.ScoutArena, 1, alreadySeen: false), Is.Not.Null);
            Assert.That(ScoutScanFueSession.TryStart(GameModeId.ScoutArena, 1, alreadySeen: true), Is.Null);
            Assert.That(ScoutScanFueSession.TryStart(GameModeId.ScoutArena, 2, alreadySeen: false), Is.Null);
            Assert.That(ScoutScanFueSession.TryStart(GameModeId.TileArena, 1, alreadySeen: false), Is.Null);
        }

        [Test]
        public void HoldThenReleaseCompletesTheLesson()
        {
            var session = ScoutScanFueSession.TryStart(GameModeId.ScoutArena, 1, alreadySeen: false);

            session.ObserveHold(held: false);
            Assert.That(session.Beat, Is.EqualTo(ScoutScanFueBeat.PromptHold));

            session.ObserveHold(held: true);
            Assert.That(session.Beat, Is.EqualTo(ScoutScanFueBeat.PromptRelease));

            session.ObserveHold(held: true);
            Assert.That(session.IsComplete, Is.False);

            session.ObserveHold(held: false);
            Assert.That(session.IsComplete, Is.True);
        }
    }
}
