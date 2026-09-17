using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Nixin.Grid.Core;

namespace Game.Core.Fue
{
    /// <summary>
    /// Career-independent tutorial: watch a slow radar, then tap only the next path tile home.
    /// Does not touch <see cref="Game.Core.State.PlayerProgress"/>.
    /// </summary>
    public sealed class TutorialSession
    {
        static readonly GridCoord[] NoOptions = Array.Empty<GridCoord>();

        readonly GridCoord[] _next = new GridCoord[1];
        readonly GridWalkRun _run;

        public TutorialSession()
        {
            var config = GameConfig.Default.WithBudget(TutorialSpec.LivesPerRun, TutorialSpec.RunsPerSession);
            _run = new GridWalkRun(TutorialSpec.CreatePath(), config);
            Beat = TutorialBeat.Intro;
        }

        public GridSize Size => TutorialSpec.Size;
        public GridCoord Start => TutorialSpec.Start;
        public GridCoord Goal => TutorialSpec.Goal;
        public TutorialBeat Beat { get; private set; }
        public GridWalkRun Run => _run;
        public GridWalkRun MemoryRun => _run;
        public bool IsCompleted { get; private set; }
        public bool IsSessionOver { get; private set; }
        public GridCoord? LastRevealed => _run.LastRevealed;

        public int LivesPerRun => TutorialSpec.LivesPerRun;
        public int RunsPerSession => TutorialSpec.RunsPerSession;
        public int LivesLeft => _run.LivesLeft;
        public int HudRunNumber => 1;
        public int Step => _run.Step;
        public int TotalSteps => _run.TotalSteps;
        public GridCoord CurrentCell => _run.CurrentCell;
        public IReadOnlyList<GridCoord> WalkedCells => _run.WalkedCells;

        public bool IsIntro => Beat == TutorialBeat.Intro;
        public bool IsWatching => Beat == TutorialBeat.Watching;

        public bool IsPlaying =>
            (Beat == TutorialBeat.PromptChoice || Beat == TutorialBeat.Advanced)
            && !IsCompleted
            && !IsSessionOver;

        public IReadOnlyList<GridCoord> Options()
        {
            if (!IsPlaying)
                return NoOptions;
            return NextPathOnly();
        }

        public IReadOnlyList<GridCoord> VisibleOptions() => Options();

        public bool IsOption(GridCoord cell)
        {
            var options = Options();
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] == cell)
                    return true;
            }

            return false;
        }

        public void DismissIntro()
        {
            if (Beat != TutorialBeat.Intro)
                return;
            Beat = TutorialBeat.Watching;
        }

        public void BeginWalk()
        {
            if (Beat != TutorialBeat.Watching)
                return;
            Beat = TutorialBeat.PromptChoice;
        }

        public WalkOutcome Choose(GridCoord cell)
        {
            if (Beat == TutorialBeat.Intro)
                throw new InvalidOperationException("Dismiss the intro first.");
            if (Beat == TutorialBeat.Watching)
                throw new InvalidOperationException("Watch the path first.");
            if (IsCompleted)
                throw new InvalidOperationException("The tutorial is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The tutorial session is already over.");
            if (!IsOption(cell))
                throw new ArgumentException($"Tile {cell} is not the next path tile from {CurrentCell}.", nameof(cell));

            var outcome = _run.Choose(cell);
            if (outcome == WalkOutcome.LevelCompleted)
            {
                IsCompleted = true;
                Beat = TutorialBeat.Completed;
                return outcome;
            }

            if (outcome == WalkOutcome.SessionOver)
            {
                IsSessionOver = true;
                Beat = TutorialBeat.SessionFailed;
                return outcome;
            }

            Beat = TutorialBeat.Advanced;
            return outcome;
        }

        IReadOnlyList<GridCoord> NextPathOnly()
        {
            var next = _run.Step + 1;
            if (next >= _run.Path.Cells.Count)
                return NoOptions;

            _next[0] = _run.Path.Cells[next];
            return _next;
        }
    }
}
