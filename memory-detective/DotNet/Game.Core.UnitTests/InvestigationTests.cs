using Game.Core.Domain;
using Game.Core.Rules;
using NUnit.Framework;
using Nixin.Memory.Core;

namespace Game.Core.Tests
{
    public sealed class InvestigationTests
    {
        [Test]
        public void Briefing_ShowsCluesAndBlocksAnswers()
        {
            var investigation = new Investigation(FirstCase.ManorMurder());
            Assert.AreEqual(InvestigationPhase.Briefing, investigation.Phase);
            Assert.AreEqual(3, investigation.Clues.Count);
            Assert.Throws<System.InvalidOperationException>(() => investigation.Answer("maid"));
        }

        [Test]
        public void CorrectAnswer_AdvancesWithoutRestarting()
        {
            var investigation = StartQuestioning();
            Assert.AreEqual(RecallOutcome.Advanced, investigation.Answer("maid"));
            Assert.AreEqual(1, investigation.Step);
            Assert.AreEqual(InvestigationPhase.Questioning, investigation.Phase);
            Assert.AreEqual("Where did the intruder come from?", investigation.CurrentQuestion);
        }

        [Test]
        public void WrongAnswer_StaysOnTheSameQuestionAndRemembers()
        {
            var investigation = StartQuestioning();
            Assert.AreEqual(RecallOutcome.FailedAndStayed, investigation.Answer("butler"));
            Assert.AreEqual(0, investigation.Step);
            Assert.AreEqual(2, investigation.Attempt);
            Assert.AreEqual(InvestigationPhase.Questioning, investigation.Phase);

            var options = investigation.CurrentOptions();
            Assert.IsTrue(Remembered(options, "butler"));
            Assert.IsFalse(Remembered(options, "maid"));
        }

        [Test]
        public void FullCorrectSequence_ClosesTheCase()
        {
            var investigation = StartQuestioning();
            Assert.AreEqual(RecallOutcome.Advanced, investigation.Answer("maid"));
            Assert.AreEqual(RecallOutcome.Completed, investigation.Answer("garden"));
            Assert.IsTrue(investigation.IsClosed);
            Assert.AreEqual(InvestigationPhase.Closed, investigation.Phase);
        }

        [Test]
        public void WrongThenCorrect_StillClosesTheCase()
        {
            var investigation = StartQuestioning();
            investigation.Answer("cook");
            Assert.AreEqual(RecallOutcome.Advanced, investigation.Answer("maid"));
            Assert.AreEqual(RecallOutcome.Completed, investigation.Answer("garden"));
            Assert.IsTrue(investigation.IsClosed);
        }

        static Investigation StartQuestioning()
        {
            var investigation = new Investigation(FirstCase.ManorMurder());
            investigation.BeginQuestioning();
            return investigation;
        }

        static bool Remembered(System.Collections.Generic.IReadOnlyList<RecallOption> options, string choice)
        {
            foreach (var option in options)
            {
                if (option.Token == choice)
                    return option.RememberedAsWrong;
            }

            Assert.Fail("Missing choice " + choice);
            return false;
        }
    }
}
