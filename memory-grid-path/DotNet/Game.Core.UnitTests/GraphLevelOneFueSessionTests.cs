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
        public void StartsOnZoomPrompt()
        {
            var session = GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false);

            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptZoom));
        }

        [Test]
        public void CameraDriftWithoutGesturesDoesNotAdvance()
        {
            var session = GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false);

            session.ObserveGestures(zoomed: false, panned: false);

            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptZoom));
        }

        [Test]
        public void ZoomThenPanGesturesCompleteTheLesson()
        {
            var session = GraphLevelOneFueSession.TryStart(GameModeId.GraphArena, 1, alreadySeen: false);

            session.ObserveGestures(zoomed: true, panned: false);
            Assert.That(session.Beat, Is.EqualTo(GraphLevelOneFueBeat.PromptPan));

            session.ObserveGestures(zoomed: false, panned: false);
            Assert.That(session.IsComplete, Is.False);

            session.ObserveGestures(zoomed: false, panned: true);
            Assert.That(session.IsComplete, Is.True);
        }
    }
}
