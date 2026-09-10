using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Nixin.Graph.Core;
using Nixin.Memory.Core;

namespace Game.Core.Rules
{
    /// <summary>
    /// One graph-path session: walk hidden route on a junction graph with lives and scoring.
    /// </summary>
    public sealed class GraphWalkRun
    {
        readonly IReadOnlyList<Prompt> _prompts;
        readonly MemoryTrace _trace = new MemoryTrace();
        readonly AttemptBudget _budget;
        readonly List<GraphNodeId> _walked = new List<GraphNodeId>();
        readonly List<GraphNodeId> _revealed = new List<GraphNodeId>();
        readonly HashSet<GraphNodeId> _sessionKnown = new HashSet<GraphNodeId>();
        readonly HashSet<GraphNodeId> _scouted = new HashSet<GraphNodeId>();
        readonly HashSet<GraphNodeId> _memorized = new HashSet<GraphNodeId>();

        RecallSession _session;

        public GraphWalkRun(GraphPath path, GameConfig config = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Config = config ?? GameConfig.Default;
            _prompts = GraphPrompts.FromPath(path, allowStepBack: true);
            _budget = new AttemptBudget(Config.LivesPerRun, Config.RunsPerSession);
            _sessionKnown.Add(Path.Start);
            StartWalk();
        }

        public GraphPath Path { get; }
        public GameConfig Config { get; }

        public int LivesLeft => _budget.LivesLeft;
        public int RunNumber => _budget.RunNumber;
        public int MistakesMade => _budget.MistakesMade;
        public int FurthestStep => _budget.FurthestStep;

        public bool IsLevelCompleted { get; private set; }
        public bool IsSessionOver { get; private set; }
        public bool IsAwaitingNextWalk { get; private set; }
        public bool IsOver => IsLevelCompleted || IsSessionOver;

        public int Step => _session.Step;
        public int TotalSteps => Path.StepCount;
        public GraphNodeId CurrentNode => Path.Nodes[Step];

        public IReadOnlyList<GraphNodeId> WalkedNodes => _walked;
        public IReadOnlyList<GraphNodeId> RevealedNodes => _revealed;
        public GraphNodeId? LastRevealed { get; private set; }

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

        public IReadOnlyList<GraphNodeId> Options()
        {
            if (IsOver || IsAwaitingNextWalk)
                return Array.Empty<GraphNodeId>();

            var choices = _session.CurrentPrompt.Choices;
            var options = new GraphNodeId[choices.Count];
            for (var i = 0; i < choices.Count; i++)
                options[i] = GraphPrompts.ToNode(choices[i]);

            return options;
        }

        public bool IsOption(GraphNodeId node)
        {
            if (IsOver || IsAwaitingNextWalk)
                return false;

            var token = GraphPrompts.ToToken(node);
            var choices = _session.CurrentPrompt.Choices;
            for (var i = 0; i < choices.Count; i++)
            {
                if (choices[i] == token)
                    return true;
            }

            return false;
        }

        public WalkOutcome Choose(GraphNodeId node)
        {
            if (IsLevelCompleted)
                throw new InvalidOperationException("The level is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The session is already over.");
            if (IsAwaitingNextWalk)
                throw new InvalidOperationException("This walk is over; start the next walk first.");
            if (!IsOption(node))
                throw new ArgumentException($"Node {node.Value} is not a valid choice from {CurrentNode.Value}.", nameof(node));

            var prompt = _session.CurrentPrompt;
            var token = GraphPrompts.ToToken(node);

            if (prompt.IsCorrect(token))
                return StepForward(node);

            return StepOnMistake(prompt, token);
        }

        public void BeginNextWalk()
        {
            if (IsLevelCompleted)
                throw new InvalidOperationException("The level is already complete.");
            if (IsSessionOver)
                throw new InvalidOperationException("The session is already over.");
            if (!IsAwaitingNextWalk)
                throw new InvalidOperationException("The current walk is still in progress.");

            _budget.BeginNextRun();
            StartWalk();
        }

        WalkOutcome StepForward(GraphNodeId node)
        {
            var outcome = _session.Choose(GraphPrompts.ToToken(node));
            LastRevealed = null;
            _walked.Add(node);
            _budget.RecordProgress(_session.Step);
            CreditStep(node);

            if (outcome != RecallOutcome.Completed)
                return WalkOutcome.Advanced;

            IsLevelCompleted = true;
            return WalkOutcome.LevelCompleted;
        }

        WalkOutcome StepOnMistake(Prompt prompt, TokenId chosen)
        {
            var correct = GraphPrompts.ToNode(prompt.CorrectTokens[0]);
            _session.Choose(chosen);

            LastRevealed = correct;
            _revealed.Add(correct);
            _walked.Add(correct);
            _budget.RecordProgress(_session.Step);
            _sessionKnown.Add(correct);

            var walkOver = _budget.SpendLife();

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

        void CreditStep(GraphNodeId node)
        {
            var known = _sessionKnown.Contains(node);

            if (_memorized.Contains(node))
            {
                _sessionKnown.Add(node);
                return;
            }

            if (_scouted.Contains(node))
            {
                if (known)
                {
                    _scouted.Remove(node);
                    _memorized.Add(node);
                }

                _sessionKnown.Add(node);
                return;
            }

            if (known)
                _memorized.Add(node);
            else
                _scouted.Add(node);

            _sessionKnown.Add(node);
        }

        void StartWalk()
        {
            _session = new RecallSession(_prompts, FailPolicy.RevealAndContinue, _trace);
            _walked.Clear();
            _revealed.Clear();
            _walked.Add(Path.Start);
            LastRevealed = null;
            IsAwaitingNextWalk = false;
        }
    }
}
