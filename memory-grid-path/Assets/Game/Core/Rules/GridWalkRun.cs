using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Nixin.Grid.Core;
using Nixin.Memory.Core;

namespace Game.Core.Rules
{
    /// <summary>
    /// One session on one level: the player walks the hidden path, loses a life for every wrong
    /// tile, and starts the walk over when the lives run out. The path itself never changes
    /// during the session, which is what makes memorising it worthwhile.
    /// </summary>
    public sealed class GridWalkRun
    {
        readonly IReadOnlyList<Prompt> _prompts;
        readonly MemoryTrace _trace = new MemoryTrace();
        readonly AttemptBudget _budget;
        readonly List<GridCoord> _walked = new List<GridCoord>();
        readonly List<GridCoord> _revealed = new List<GridCoord>();
        readonly List<GridCoord> _lighthouses = new List<GridCoord>();
        readonly List<PathPickup> _unusedPickups = new List<PathPickup>();
        readonly HashSet<GridCoord> _lighthouseSet = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _sessionKnown = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _scouted = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _memorized = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _priorWalked = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _priorFailed = new HashSet<GridCoord>();

        readonly HashSet<GridCoord> _blocked = new HashSet<GridCoord>();
        readonly List<GridCoord> _blockedList = new List<GridCoord>();

        RecallSession _session;
        IReadOnlyList<GridCoord> _glimpse;

        public GridWalkRun(GridPath path, GameConfig config = null, PathAids aids = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Config = config ?? GameConfig.Default;
            Aids = aids ?? PathAids.None;

            for (var i = 0; i < Aids.BlockedHints.Count; i++)
            {
                _blocked.Add(Aids.BlockedHints[i]);
                _blockedList.Add(Aids.BlockedHints[i]);
            }

            _prompts = GridPrompts.FromPath(path, allowStepBack: true, _blocked);
            _budget = new AttemptBudget(Config.LivesPerRun, Config.RunsPerSession);

            for (var i = 0; i < Aids.Lighthouses.Count; i++)
                AddLighthouse(Aids.Lighthouses[i]);

            for (var i = 0; i < Aids.Pickups.Count; i++)
                _unusedPickups.Add(Aids.Pickups[i]);

            _sessionKnown.Add(Path.Start);
            for (var i = 0; i < _lighthouses.Count; i++)
                _sessionKnown.Add(_lighthouses[i]);

            StartWalk();
        }

        public GridPath Path { get; }
        public GameConfig Config { get; }
        public PathAids Aids { get; }

        public int LivesLeft => _budget.LivesLeft;
        public int RunNumber => _budget.RunNumber;
        public int MistakesMade => _budget.MistakesMade;

        /// <summary>Furthest step index reached in any walk so far. Drives the score.</summary>
        public int FurthestStep => _budget.FurthestStep;

        public bool IsLevelCompleted { get; private set; }
        public bool IsSessionOver { get; private set; }

        /// <summary>Last life of this walk is spent; the correct tile is still showing until BeginNextWalk.</summary>
        public bool IsAwaitingNextWalk { get; private set; }

        public bool IsOver => IsLevelCompleted || IsSessionOver;

        /// <summary>Steps taken in the current walk.</summary>
        public int Step => _session.Step;

        public int TotalSteps => Path.StepCount;

        public GridCoord CurrentCell => Path.Cells[Step];

        /// <summary>Tiles walked in the current walk, starting at the path's start cell.</summary>
        public IReadOnlyList<GridCoord> WalkedCells => _walked;

        /// <summary>Tiles uncovered by mistakes in the current walk. Cleared when a walk restarts.</summary>
        public IReadOnlyList<GridCoord> RevealedCells => _revealed;

        /// <summary>Session-persistent whites. They stay lit across walk restarts.</summary>
        public IReadOnlyList<GridCoord> LighthouseCells => _lighthouses;

        /// <summary>On-path pickups that have not been collected yet this session.</summary>
        public IReadOnlyList<PathPickup> UnusedPickups => _unusedPickups;

        /// <summary>Off-path tiles greyed out from the start; never offered as choices.</summary>
        public IReadOnlyList<GridCoord> BlockedCells => _blockedList;

        /// <summary>The tile shown after the most recent mistake, or null after a correct step.</summary>
        public GridCoord? LastRevealed { get; private set; }

        public int ScoutCells => _scouted.Count;
        public int MemoryCells => _memorized.Count;

        public SessionScoreInput ScoreInput => new SessionScoreInput(
            ScoutCells,
            MemoryCells,
            MistakesMade,
            RunNumber,
            TotalSteps,
            FurthestStep,
            IsLevelCompleted);

        public int Score => GridScore.Calculate(Config, ScoreInput);

        public int MemoryGrade => GridScore.MemoryGrade(Config, ScoreInput);

        /// <summary>Neighbouring tiles the player may pick from, exactly one of which continues the path.</summary>
        public IReadOnlyList<GridCoord> Options()
        {
            if (IsOver)
                return Array.Empty<GridCoord>();
            if (IsAwaitingNextWalk)
                return Array.Empty<GridCoord>();

            var choices = _session.CurrentPrompt.Choices;
            var options = new GridCoord[choices.Count];
            for (var i = 0; i < choices.Count; i++)
                options[i] = GridPrompts.ToCoord(choices[i]);

            return options;
        }

        public bool IsOption(GridCoord cell)
        {
            if (IsOver)
                return false;
            if (IsAwaitingNextWalk)
                return false;

            var token = GridPrompts.ToToken(cell);
            var choices = _session.CurrentPrompt.Choices;
            for (var i = 0; i < choices.Count; i++)
            {
                if (choices[i] == token)
                    return true;
            }

            return false;
        }

        public bool IsLighthouse(GridCoord cell) => _lighthouseSet.Contains(cell);

        public bool IsBlocked(GridCoord cell) => _blocked.Contains(cell);

        /// <summary>
        /// True when this cell was walked correctly in an earlier walk of this session.
        /// Used for "you forgot" feedback when the player misses it later.
        /// </summary>
        public bool WasCoveredInPriorWalk(GridCoord cell) => _priorWalked.Contains(cell);

        /// <summary>
        /// True when this cell was revealed by a mistake in an earlier walk.
        /// Used for "you remembered" feedback when the player gets it right later.
        /// </summary>
        public bool WasFailedInPriorWalk(GridCoord cell) => _priorFailed.Contains(cell);

        /// <summary>
        /// Full path cells from the most recent glimpse, or null if none is pending.
        /// Unity flashes them then calls this again to clear the flag.
        /// </summary>
        public IReadOnlyList<GridCoord> ConsumeGlimpse()
        {
            var cells = _glimpse;
            _glimpse = null;
            return cells;
        }

        public WalkOutcome Choose(GridCoord cell)
        {
            if (IsLevelCompleted)
                throw new InvalidOperationException("The level is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The session is already over.");
            if (IsAwaitingNextWalk)
                throw new InvalidOperationException("This walk is over; start the next walk first.");
            if (!IsOption(cell))
                throw new ArgumentException($"Tile {cell} is not adjacent to {CurrentCell}.", nameof(cell));

            var prompt = _session.CurrentPrompt;
            var token = GridPrompts.ToToken(cell);

            if (prompt.IsCorrect(token))
                return StepForward(cell);

            return StepOnMistake(prompt, token);
        }

        WalkOutcome StepForward(GridCoord cell)
        {
            var outcome = _session.Choose(GridPrompts.ToToken(cell));
            LastRevealed = null;
            _walked.Add(cell);
            _budget.RecordProgress(_session.Step);
            CreditStep(cell);
            CollectPickup(cell);

            if (outcome != RecallOutcome.Completed)
                return WalkOutcome.Advanced;

            IsLevelCompleted = true;
            return WalkOutcome.LevelCompleted;
        }

        WalkOutcome StepOnMistake(Prompt prompt, TokenId chosen)
        {
            // Read the answer before choosing, because choosing moves past this prompt.
            var correct = GridPrompts.ToCoord(prompt.CorrectTokens[0]);
            _session.Choose(chosen);

            LastRevealed = correct;
            _revealed.Add(correct);
            _walked.Add(correct);
            _budget.RecordProgress(_session.Step);
            _sessionKnown.Add(correct);

            var walkOver = _budget.SpendLife();

            // Uncovering the final tile still finishes the path, even on the last life.
            if (_session.IsCompleted)
            {
                IsLevelCompleted = true;
                return WalkOutcome.LevelCompleted;
            }

            if (!walkOver)
                return WalkOutcome.WrongRevealed;

            if (!_budget.HasRunsLeft)
            {
                IsSessionOver = true;
                return WalkOutcome.SessionOver;
            }

            IsAwaitingNextWalk = true;
            return WalkOutcome.RunFailed;
        }

        public void BeginNextWalk()
        {
            if (IsLevelCompleted)
                throw new InvalidOperationException("The level is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The session is already over.");
            if (!IsAwaitingNextWalk)
                throw new InvalidOperationException("The current walk is still in progress.");

            RememberWalkForNextRun();
            _budget.BeginNextRun();
            StartWalk();
        }

        void RememberWalkForNextRun()
        {
            for (var i = 0; i < _walked.Count; i++)
                _priorWalked.Add(_walked[i]);

            for (var i = 0; i < _revealed.Count; i++)
                _priorFailed.Add(_revealed[i]);
        }

        void CollectPickup(GridCoord cell)
        {
            var index = IndexOfUnusedPickup(cell);
            if (index < 0)
                return;

            var pickup = _unusedPickups[index];
            _unusedPickups.RemoveAt(index);

            if (pickup.Kind == PathPickupKind.Glimpse)
            {
                _glimpse = Path.Cells;
                for (var i = 0; i < Path.Cells.Count; i++)
                    _sessionKnown.Add(Path.Cells[i]);
                return;
            }

            LightBeaconAhead();
        }

        int IndexOfUnusedPickup(GridCoord cell)
        {
            for (var i = 0; i < _unusedPickups.Count; i++)
            {
                if (_unusedPickups[i].Cell == cell)
                    return i;
            }

            return -1;
        }

        void LightBeaconAhead()
        {
            for (var i = Path.Cells.Count - 2; i > Step; i--)
            {
                if (TryAddLighthouse(Path.Cells[i]))
                    return;
            }

            for (var i = Path.Cells.Count - 2; i >= 1; i--)
            {
                if (TryAddLighthouse(Path.Cells[i]))
                    return;
            }
        }

        bool TryAddLighthouse(GridCoord cell)
        {
            if (cell == Path.Start || cell == Path.Goal)
                return false;
            if (_lighthouseSet.Contains(cell))
                return false;

            AddLighthouse(cell);
            return true;
        }

        void AddLighthouse(GridCoord cell)
        {
            if (!_lighthouseSet.Add(cell))
                return;

            _lighthouses.Add(cell);
            _sessionKnown.Add(cell);
        }

        void CreditStep(GridCoord cell)
        {
            var known = _sessionKnown.Contains(cell);

            if (_memorized.Contains(cell))
            {
                _sessionKnown.Add(cell);
                return;
            }

            if (_scouted.Contains(cell))
            {
                if (known)
                {
                    _scouted.Remove(cell);
                    _memorized.Add(cell);
                }

                _sessionKnown.Add(cell);
                return;
            }

            if (known)
                _memorized.Add(cell);
            else
                _scouted.Add(cell);

            _sessionKnown.Add(cell);
        }

        void StartWalk()
        {
            _session = new RecallSession(_prompts, FailPolicy.RevealAndContinue, _trace);
            _walked.Clear();
            _revealed.Clear();
            _walked.Add(Path.Start);
            LastRevealed = null;
            IsAwaitingNextWalk = false;
            _glimpse = null;
        }
    }
}
