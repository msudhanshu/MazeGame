using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Fue;
using Game.Core.Rules;
using Game.Core.State;
using Nixin.Game.Core;
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

            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.PromptChoice));
            Assert.That(session.IsPlaying, Is.True);
        }

        [Test]
        public void FirstClickIsAlwaysLucky()
        {
            var session = Opened();
            var pick = FirstVisible(session);

            var outcome = session.Choose(pick);

            Assert.That(outcome, Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Lucky));
            Assert.That(session.LivesLeft, Is.EqualTo(2));
            Assert.That(session.CurrentCell, Is.EqualTo(pick));
            Assert.That(session.HudRunNumber, Is.EqualTo(1));
        }

        [Test]
        public void SecondClickIsAlwaysAPartialFailOntoTheOtherNeighbour()
        {
            var session = Opened();
            session.Choose(FirstVisible(session));
            var wrong = FirstVisible(session);

            var outcome = session.Choose(wrong);

            Assert.That(outcome, Is.EqualTo(WalkOutcome.WrongRevealed));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.UnluckyPartial));
            Assert.That(session.LivesLeft, Is.EqualTo(1));
            Assert.That(session.CurrentCell, Is.Not.EqualTo(wrong));
            Assert.That(session.LastRevealed, Is.EqualTo(session.CurrentCell));
            Assert.That(session.PathPrefix, Does.Not.Contain(wrong));
        }

        [Test]
        public void ThirdClickFailsTheWalkAndWaitsForMemory()
        {
            var session = PlayDiscoveryToRunFailed();

            Assert.That(session.IsAwaitingMemory, Is.True);
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.UnluckyRunOver));
            Assert.That(session.IsPlaying, Is.False);
            Assert.That(session.PathPrefix.Count, Is.EqualTo(4));
        }

        [Test]
        public void MemoryWalkKeepsTheDiscoveryPrefixThenASeededRemainder()
        {
            var session = PlayDiscoveryToRunFailed();
            var prefix = new List<GridCoord>(session.PathPrefix);

            var path = session.BeginMemoryWalk(seed: 17);

            Assert.That(session.IsDiscovery, Is.False);
            Assert.That(session.HudRunNumber, Is.EqualTo(2));
            Assert.That(session.LivesLeft, Is.EqualTo(2));
            Assert.That(path.Start, Is.EqualTo(TutorialSpec.Start));
            Assert.That(path.Goal, Is.EqualTo(TutorialSpec.Goal));
            Assert.That(path.Cells.Count, Is.GreaterThan(prefix.Count));
            for (var i = 0; i < prefix.Count; i++)
                Assert.That(path.Cells[i], Is.EqualTo(prefix[i]));

            var again = TutorialPathCompleter.Complete(
                TutorialSpec.Size,
                prefix,
                new XorShiftRandom(17));
            Assert.That(again.Cells, Is.EqualTo(path.Cells));
        }

        [Test]
        public void RememberingAForcedCorrectTileSetsRememberedBeat()
        {
            var session = PlayDiscoveryToRunFailed();
            var lucky = session.PathPrefix[1];
            var remembered = session.PathPrefix[2];
            session.BeginMemoryWalk(3);

            Assert.That(session.Choose(lucky), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Advanced));

            Assert.That(session.Choose(remembered), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Remembered));
        }

        [Test]
        public void RepeatingADiscoveryMistakeSetsRepeatedMistakeBeat()
        {
            var session = Opened();
            session.Choose(FirstVisible(session));
            var firstWrong = FirstVisible(session);
            session.Choose(firstWrong);
            session.Choose(FirstVisible(session));
            session.BeginMemoryWalk(9);

            session.Choose(session.MemoryRun.Path.Cells[1]);
            var outcome = session.Choose(firstWrong);

            Assert.That(outcome, Is.EqualTo(WalkOutcome.WrongRevealed));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.RepeatedMistake));
            Assert.That(session.LivesLeft, Is.EqualTo(1));
        }

        [Test]
        public void CompletingTheMemoryWalkDoesNotTouchCareerProgress()
        {
            var progress = new PlayerProgress();
            var unlocked = progress.HighestUnlockedLevel;
            var session = PlayDiscoveryToRunFailed();
            session.BeginMemoryWalk(21);
            WalkCorrectlyUntilDone(session);

            Assert.That(session.IsCompleted, Is.True);
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Completed));
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(unlocked));
            Assert.That(progress.CareerScore, Is.EqualTo(0));
        }

        [Test]
        public void RunningOutOfMemoryLivesIsSessionOverWithoutProgress()
        {
            var progress = new PlayerProgress();
            var session = PlayDiscoveryToRunFailed();
            session.BeginMemoryWalk(4);
            FailUntilSessionOver(session);

            Assert.That(session.IsSessionOver, Is.True);
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.SessionFailed));
            Assert.That(progress.HighestUnlockedLevel, Is.EqualTo(1));
        }

        [Test]
        public void CompleterFinishesThePrefixAtTheHomeTile()
        {
            var prefix = new[]
            {
                TutorialSpec.Start,
                new GridCoord(1, 0),
                new GridCoord(1, 1),
                new GridCoord(2, 1)
            };
            var path = TutorialPathCompleter.Complete(TutorialSpec.Size, prefix, new XorShiftRandom(1));
            Assert.That(path.Size, Is.EqualTo(TutorialSpec.Size));
            Assert.That(path.Start, Is.EqualTo(TutorialSpec.Start));
            Assert.That(path.Goal, Is.EqualTo(TutorialSpec.Goal));
            for (var i = 0; i < prefix.Length; i++)
                Assert.That(path.Cells[i], Is.EqualTo(prefix[i]));
        }

        [Test]
        public void TutorialRunsFromBottomLeftToTopRightHome()
        {
            Assert.That(TutorialSpec.Start, Is.EqualTo(new GridCoord(0, 0)));
            Assert.That(TutorialSpec.Goal, Is.EqualTo(new GridCoord(3, 2)));

            var session = PlayDiscoveryToRunFailed();
            Assert.That(session.PathPrefix, Does.Not.Contain(TutorialSpec.Goal));

            var path = session.BeginMemoryWalk(17);
            Assert.That(path.Start, Is.EqualTo(TutorialSpec.Start));
            Assert.That(path.Goal, Is.EqualTo(TutorialSpec.Goal));
        }

        [Test]
        public void OnlyVisibleNeighbourIsTakenAsLuckyInsteadOfThrowing()
        {
            var session = Opened();
            session.Choose(new GridCoord(0, 1));
            session.Choose(new GridCoord(1, 1));

            Assert.That(session.CurrentCell, Is.EqualTo(new GridCoord(0, 2)));
            Assert.That(session.LivesLeft, Is.EqualTo(1));

            var only = session.VisibleOptions();
            Assert.That(only.Count, Is.EqualTo(1));
            Assert.DoesNotThrow(() => session.Choose(only[0]));
            Assert.That(session.LivesLeft, Is.EqualTo(1));
            Assert.That(session.CurrentCell, Is.EqualTo(only[0]));
            Assert.That(session.Beat, Is.EqualTo(TutorialBeat.Lucky));
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

        static TutorialSession Opened()
        {
            var session = new TutorialSession();
            session.DismissIntro();
            return session;
        }

        static TutorialSession PlayDiscoveryToRunFailed()
        {
            var session = Opened();
            Assert.That(session.Choose(FirstVisible(session)), Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(session.Choose(FirstVisible(session)), Is.EqualTo(WalkOutcome.WrongRevealed));
            Assert.That(session.Choose(FirstVisible(session)), Is.EqualTo(WalkOutcome.RunFailed));
            return session;
        }

        static GridCoord FirstVisible(TutorialSession session)
        {
            var options = session.VisibleOptions();
            Assert.That(options.Count, Is.GreaterThan(0));
            return options[0];
        }

        static void WalkCorrectlyUntilDone(TutorialSession session)
        {
            var guard = 0;
            while (session.IsPlaying && guard++ < 32)
            {
                var next = session.MemoryRun.Path.Cells[session.MemoryRun.Step + 1];
                session.Choose(next);
            }

            Assert.That(session.IsCompleted, Is.True);
        }

        static void FailUntilSessionOver(TutorialSession session)
        {
            var guard = 0;
            while (session.IsPlaying && guard++ < 32)
            {
                GridCoord wrong = default;
                var found = false;
                foreach (var option in session.VisibleOptions())
                {
                    if (option != session.MemoryRun.Path.Cells[session.MemoryRun.Step + 1])
                    {
                        wrong = option;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    session.Choose(session.MemoryRun.Path.Cells[session.MemoryRun.Step + 1]);
                    continue;
                }

                session.Choose(wrong);
            }

            Assert.That(session.IsSessionOver, Is.True);
        }
    }
}
