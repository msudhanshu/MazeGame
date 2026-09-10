using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Nixin.Memory.Core;

namespace Game.Core.Rules
{
    public sealed class Investigation
    {
        readonly CaseFile _case;
        readonly RecallSession _session;

        public Investigation(CaseFile file)
        {
            _case = file ?? throw new ArgumentNullException(nameof(file));
            _session = new RecallSession(file.Prompts, FailPolicy.Stay);
        }

        public InvestigationPhase Phase { get; private set; } = InvestigationPhase.Briefing;
        public string Title => _case.Title;
        public IReadOnlyList<string> Clues => _case.Clues;
        public int Attempt => _session.Attempt;
        public int Step => _session.Step;
        public bool IsClosed => Phase == InvestigationPhase.Closed;

        public string CurrentQuestion
        {
            get
            {
                EnsureQuestioning();
                return _case.Inquiries[Step].Question;
            }
        }

        public IReadOnlyList<RecallOption> CurrentOptions()
        {
            if (Phase != InvestigationPhase.Questioning)
                return Array.Empty<RecallOption>();
            return _session.CurrentOptions();
        }

        public void BeginQuestioning()
        {
            if (Phase != InvestigationPhase.Briefing)
                throw new InvalidOperationException("Questioning already started.");
            Phase = InvestigationPhase.Questioning;
        }

        public RecallOutcome Answer(TokenId choice)
        {
            EnsureQuestioning();
            var outcome = _session.Choose(choice);
            if (outcome == RecallOutcome.Completed)
                Phase = InvestigationPhase.Closed;
            return outcome;
        }

        void EnsureQuestioning()
        {
            if (Phase == InvestigationPhase.Briefing)
                throw new InvalidOperationException("Study the clues before answering.");
            if (Phase == InvestigationPhase.Closed)
                throw new InvalidOperationException("The case is already closed.");
        }
    }
}
