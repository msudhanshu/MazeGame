using System;
using Game.Core.Fue;
using Game.Core.Rules;
using Game.Core.State;
using Nixin.Grid.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class TutorialSessionTests
    {
        [Test]
        public void StartsOnTheCenterIntroUntilDismissed()
        {
            var session = new TutorialSession();
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Intro));
            Assert.That(session.IsIntro, Is.True);
            Assert.That(session.IsPlaying, Is.False);
            Assert.Throws<InvalidOperationException>(() => session.Choose(new GridCoord(1, 0)));

            session.DismissIntro();

            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Watching));
            Assert.That(session.IsWatching, Is.True);
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(session.VisibleOptions(), Is.Empty);
            Assert.Throws<InvalidOperationException>(() => session.Choose(new GridCoord(1, 0)));
        }

        [Test]
        public void AfterTheRadarTheOnlyOptionIsTheNextPathTile()
        {
            var session = ReadyToWalk();
            var next = session.Run.Path.Cells[1];
            var offPath = new GridCoord(0, 1);

            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.PromptChoice));
            Assert.That(session.IsPlaying, Is.True);
            Assert.That(session.VisibleOptions(), Is.EqualTo(new[] { next }));
            Assert.That(session.IsOption(next), Is.True);
            Assert.That(session.IsOption(offPath), Is.False);
            Assert.Throws<ArgumentException>(() => session.Choose(offPath));
        }

        [Test]
        public void WalkingTheAuthoredPathReachesHome()
        {
            var progress = new PlayerProgress();
            var unlocked = progress.HighestUnlockedLevel;
            var session = ReadyToWalk();

            WalkHome(session);

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Completed));
            Assert.That(session.CurrentCell, Is.EqualTo(TutorialSpec.Goal));
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(unlocked));
            Assert.That(progress.CareerScore, Is.EqualTo(0));
        }

        [Test]
        public void WrongNeighboursStayLockedAfterTheFirstStep()
        {
            var session = ReadyToWalk();
            session.Choose(session.Run.Path.Cells[1]);

            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Advanced));
            Assert.That(session.VisibleOptions(), Is.EqualTo(new[] { session.Run.Path.Cells[2] }));
            Assert.That(session.IsOption(new GridCoord(1, 1)), Is.False);
            Assert.Throws<ArgumentException>(() => session.Choose(new GridCoord(1, 1)));
        }

        [Test]
        public void AuthoredPathIsAShortCornerWalkHome()
        {
            Assert.That(TutorialSpec.Start, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(TutorialSpec.Goal, Is.EqualTo(new GridCoord(3, 2)));
            Assert.That(TutorialSpec.PathCells, Is.EqualTo(new[]
            {
                new GridCoord(0, 0),
                new GridCoord(1, 0),
                new GridCoord(2, 0),
                new GridCoord(2, 1),
                new GridCoord(3, 1),
                new GridCoord(3, 2)
            }));

            var path = TutorialSpec.CreatePath();
            Assert.That(path.Size, Is.EqualTo(TutorialSpec.Size));
            Assert.That(path.Start, Is.EqualTo(TutorialSpec.Start));
            Assert.That(path.Goal, Is.EqualTo(TutorialSpec.Goal));
            Assert.That(path.StepCount, Is.EqualTo(5));
            Assert.That(TutorialSpec.RadarSweepSeconds, Is.GreaterThanOrEqualTo(5f));
            Assert.That(TutorialSpec.RadarHoldSeconds, Is.GreaterThanOrEqualTo(1.5f));
        }

        [Test]
        public void SkipGateAutoPlaysUntilTwoSkipsThenStops()
        {
            Assert.That(TutorialSkipGate.ShouldAutoPlay(hasSeen: false, skipCount: 0, replayRequested: false), Is.True);
            Assert.That(TutorialSkipGate.ShouldAutoPlay(hasSeen: false, skipCount: 1, replayRequested: false), Is.True);
            Assert.That(TutorialSkipGate.ShouldAutoPlay(hasSeen: false, skipCount: 2, replayRequested: false), Is.False);
            Assert.That(TutorialSkipGate.ShouldMarkSeen(1), Is.False);
            Assert.That(TutorialSkipGate.ShouldMarkSeen(2), Is.True);
        }

        [Test]
        public void SkipGateReplayIgnoresSeenAndSkipCount()
        {
            Assert.That(TutorialSkipGate.ShouldAutoPlay(hasSeen: true, skipCount: 2, replayRequested: true), Is.True);
            Assert.That(TutorialSkipGate.ShouldAutoPlay(hasSeen: true, skipCount: 0, replayRequested: false), Is.False);
        }

        static TutorialSession ReadyToWalk()
        {
            var session = new TutorialSession();
            session.DismissIntro();
            session.BeginWalk();
            return session;
        }

        static void WalkHome(TutorialSession session)
        {
            var guard = 0;
            while (session.IsPlaying && guard++ < 32)
            {
                var next = session.Run.Path.Cells[session.Step + 1];
                var outcome = session.Choose(next);
                if (session.IsCompleted)
                {
                    Assert.That(outcome, Is.EqualTo(WalkOutcome.LevelCompleted));
                    return;
                }

                Assert.That(outcome, Is.EqualTo(WalkOutcome.Advanced));
            }

            Assert.That(session.IsCompleted, Is.True);
        }
    }
}
