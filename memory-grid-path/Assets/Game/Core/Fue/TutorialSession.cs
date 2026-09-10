using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Memory.Core;

namespace Game.Core.Fue
{
    /// <summary>
    /// Career-independent tutorial: a scripted discovery walk that builds a path, then a
    /// real memory walk on that path. Does not touch <see cref="Game.Core.State.PlayerProgress"/>.
    /// </summary>
    public sealed class TutorialSession
    {
        readonly AttemptBudget _discoveryBudget = new AttemptBudget(TutorialSpec.LivesPerRun, TutorialSpec.RunsPerSession);
        readonly List<GridCoord> _prefix = new List<GridCoord>();
        readonly List<GridCoord> _wrongPicks = new List<GridCoord>();
        readonly HashSet<GridCoord> _memoryCorrect = new HashSet<GridCoord>();
        readonly HashSet<GridCoord> _memoryWrong = new HashSet<GridCoord>();

        int _discoveryStep;
        GridWalkRun _memory;

        public TutorialSession()
        {
            _prefix.Add(TutorialSpec.Start);
            Beat = TutorialBeat.Intro;
        }

        public GridSize Size => TutorialSpec.Size;
        public GridCoord Start => TutorialSpec.Start;
        public GridCoord Goal => TutorialSpec.Goal;
        public TutorialBeat Beat { get; private set; }
        public GridWalkRun MemoryRun => _memory;
        public bool IsDiscovery => _memory == null && !IsAwaitingMemory;
        public bool IsAwaitingMemory { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsSessionOver { get; private set; }
        public GridCoord? LastRevealed { get; private set; }

        public int LivesPerRun => TutorialSpec.LivesPerRun;
        public int RunsPerSession => TutorialSpec.RunsPerSession;

        public int LivesLeft => _memory != null ? _memory.LivesLeft : _discoveryBudget.LivesLeft;

        public int HudRunNumber => _memory != null ? 2 : 1;

        public int Step => _memory != null ? _memory.Step : _prefix.Count - 1;

        public int TotalSteps => _memory != null ? _memory.TotalSteps : 0;

        public GridCoord CurrentCell => _memory != null ? _memory.CurrentCell : _prefix[_prefix.Count - 1];

        public IReadOnlyList<GridCoord> WalkedCells => _memory != null ? _memory.WalkedCells : _prefix;

        public IReadOnlyList<GridCoord> PathPrefix => _prefix;

        public bool IsIntro => Beat == TutorialBeat.Intro;

        public bool IsPlaying =>
            !IsIntro && !IsCompleted && !IsSessionOver && !IsAwaitingMemory && (_memory == null || !_memory.IsAwaitingNextWalk);

        public IReadOnlyList<GridCoord> Options()
        {
            if (!IsPlaying)
                return Array.Empty<GridCoord>();
            if (_memory != null)
                return _memory.Options();
            return GridNeighbors.Of(Size, CurrentCell);
        }

        public IReadOnlyList<GridCoord> VisibleOptions()
        {
            var visible = PathOptionFilter.VisibleGridOptions(WalkedCells, Options());
            if (_memory != null)
                return visible;
            return WithoutGoal(visible);
        }

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
            Beat = TutorialBeat.PromptChoice;
        }

        public WalkOutcome Choose(GridCoord cell)
        {
            if (Beat == TutorialBeat.Intro)
                throw new InvalidOperationException("Dismiss the intro first.");
            if (IsCompleted)
                throw new InvalidOperationException("The tutorial is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The tutorial session is already over.");
            if (IsAwaitingMemory)
                throw new InvalidOperationException("Start the memory walk first.");
            if (!IsOption(cell))
                throw new ArgumentException($"Tile {cell} is not a neighbour of {CurrentCell}.", nameof(cell));

            if (_memory != null)
                return ChooseMemory(cell);

            return ChooseDiscovery(cell);
        }

        public GridPath BeginMemoryWalk(int seed)
        {
            if (!IsAwaitingMemory)
                throw new InvalidOperationException("Discovery is still in progress.");

            Exception last = null;
            for (var attempt = 0; attempt < 16; attempt++)
            {
                try
                {
                    var random = new XorShiftRandom(unchecked(seed + attempt * 7919));
                    var path = TutorialPathCompleter.Complete(Size, _prefix, random);
                    var config = GameConfig.Default.WithBudget(TutorialSpec.LivesPerRun, 1);
                    _memory = new GridWalkRun(path, config);
                    IsAwaitingMemory = false;
                    LastRevealed = null;
                    return path;
                }
                catch (Exception exception)
                {
                    last = exception;
                }
            }

            throw new InvalidOperationException("Could not complete the tutorial path.", last);
        }

        WalkOutcome ChooseDiscovery(GridCoord cell)
        {
            var visible = VisibleOptions();
            if (!PathOptionFilter.Contains(visible, cell))
                throw new ArgumentException($"Tile {cell} is not a visible option.", nameof(cell));

            LastRevealed = null;

            if (_discoveryStep == 0 || !TryPickForcedCorrect(visible, cell, out var correct))
            {
                _prefix.Add(cell);
                _discoveryStep++;
                Beat = TutorialBeat.Lucky;
                return DiscoveryOutcomeAfterMove(WalkOutcome.Advanced);
            }

            _wrongPicks.Add(cell);
            _memoryWrong.Add(cell);
            _memoryCorrect.Add(correct);
            _prefix.Add(correct);
            LastRevealed = correct;
            var walkOver = _discoveryBudget.SpendLife();
            _discoveryStep++;

            if (!walkOver)
            {
                Beat = TutorialBeat.UnluckyPartial;
                return DiscoveryOutcomeAfterMove(WalkOutcome.WrongRevealed);
            }

            IsAwaitingMemory = true;
            Beat = TutorialBeat.UnluckyRunOver;
            return WalkOutcome.RunFailed;
        }

        WalkOutcome ChooseMemory(GridCoord cell)
        {
            var outcome = _memory.Choose(cell);
            LastRevealed = _memory.LastRevealed;

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

            if (outcome == WalkOutcome.Advanced)
            {
                Beat = _memoryCorrect.Contains(cell) ? TutorialBeat.Remembered : TutorialBeat.Advanced;
                return outcome;
            }

            Beat = _memoryWrong.Contains(cell) ? TutorialBeat.RepeatedMistake : TutorialBeat.Advanced;
            return outcome;
        }

        WalkOutcome DiscoveryOutcomeAfterMove(WalkOutcome outcome)
        {
            if (VisibleOptions().Count == 0 && !IsAwaitingMemory)
            {
                IsAwaitingMemory = true;
                Beat = TutorialBeat.UnluckyRunOver;
                return WalkOutcome.RunFailed;
            }

            return outcome;
        }

        bool TryPickForcedCorrect(IReadOnlyList<GridCoord> visible, GridCoord chosen, out GridCoord correct)
        {
            correct = default;
            GridCoord? best = null;
            var bestScore = int.MinValue;
            for (var i = 0; i < visible.Count; i++)
            {
                var option = visible[i];
                if (option == chosen || option == TutorialSpec.Goal)
                    continue;

                var score = ScoreForcedCorrect(option);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = option;
                }
            }

            if (!best.HasValue)
                return false;

            correct = best.Value;
            return true;
        }

        int ScoreForcedCorrect(GridCoord option)
        {
            var reach = CanReachGoal(option) ? 1000 : 0;
            return reach + CountOpenExits(option) * 20 - option.ManhattanDistanceTo(TutorialSpec.Goal);
        }

        int CountOpenExits(GridCoord cell)
        {
            var neighbours = GridNeighbors.Of(Size, cell);
            var count = 0;
            for (var i = 0; i < neighbours.Count; i++)
            {
                var neighbour = neighbours[i];
                if (!_prefix.Contains(neighbour) && neighbour != TutorialSpec.Goal)
                    count++;
            }

            return count;
        }

        bool CanReachGoal(GridCoord from)
        {
            var goal = TutorialSpec.Goal;
            if (from == goal)
                return true;

            var seen = new HashSet<GridCoord>(_prefix) { from };
            var queue = new Queue<GridCoord>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var neighbours = GridNeighbors.Of(Size, cell);
                for (var i = 0; i < neighbours.Count; i++)
                {
                    var next = neighbours[i];
                    if (next == goal)
                        return true;
                    if (!seen.Add(next))
                        continue;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        static IReadOnlyList<GridCoord> WithoutGoal(IReadOnlyList<GridCoord> visible)
        {
            var filtered = new List<GridCoord>(visible.Count);
            for (var i = 0; i < visible.Count; i++)
            {
                if (visible[i] != TutorialSpec.Goal)
                    filtered.Add(visible[i]);
            }

            return filtered;
        }
    }
}
