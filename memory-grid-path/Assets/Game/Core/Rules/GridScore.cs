using System;
using Game.Core.Domain;

namespace Game.Core.Rules
{
    /// <summary>How a session paid out: unique scout cells, unique memory cells, and the walk budget.</summary>
    public readonly struct SessionScoreInput
    {
        public SessionScoreInput(
            int scoutCells,
            int memoryCells,
            int mistakes,
            int runsUsed,
            int totalSteps,
            int furthestStep,
            bool completed)
        {
            ScoutCells = scoutCells;
            MemoryCells = memoryCells;
            Mistakes = mistakes;
            RunsUsed = runsUsed;
            TotalSteps = totalSteps;
            FurthestStep = furthestStep;
            Completed = completed;
        }

        public int ScoutCells { get; }
        public int MemoryCells { get; }
        public int Mistakes { get; }
        public int RunsUsed { get; }
        public int TotalSteps { get; }
        public int FurthestStep { get; }
        public bool Completed { get; }
    }

    /// <summary>
    /// Turns a session into points and a memory grade. Scout guesses pay less than recalled
    /// steps; finishing late with many mistakes is a poor grade even if the path is complete.
    /// </summary>
    public static class GridScore
    {
        public const int FailedGradeCap = 35;

        public static int Calculate(GameConfig config, SessionScoreInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            Validate(input);

            var score = input.ScoutCells * config.ScoutPoints
                + input.MemoryCells * config.MemoryPoints
                - input.Mistakes * config.MistakePenalty;

            if (input.Completed)
            {
                var runsLeft = Math.Max(0, config.RunsPerSession - input.RunsUsed);
                score += config.CompletionBonus + runsLeft * config.UnusedRunBonus;
            }

            return Math.Max(0, score);
        }

        public static int MemoryGrade(GameConfig config, SessionScoreInput input)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            Validate(input);

            if (!input.Completed)
            {
                var raw = (int)Math.Round(30.0 * input.FurthestStep / input.TotalSteps - 3 * input.Mistakes);
                return Clamp(raw, 0, FailedGradeCap);
            }

            var memoryShare = (double)input.MemoryCells / input.TotalSteps;
            var unused = Math.Max(0, config.RunsPerSession - input.RunsUsed);
            var unusedShare = (double)unused / config.RunsPerSession;
            var grade = 40 + 40 * memoryShare + 20 * unusedShare - 5 * input.Mistakes;
            return Clamp((int)Math.Round(grade), 0, 100);
        }

        static void Validate(SessionScoreInput input)
        {
            if (input.ScoutCells < 0)
                throw new ArgumentOutOfRangeException(nameof(input), "ScoutCells cannot be negative.");
            if (input.MemoryCells < 0)
                throw new ArgumentOutOfRangeException(nameof(input), "MemoryCells cannot be negative.");
            if (input.Mistakes < 0)
                throw new ArgumentOutOfRangeException(nameof(input), "Mistakes cannot be negative.");
            if (input.RunsUsed < 1)
                throw new ArgumentOutOfRangeException(nameof(input), "RunsUsed must be at least 1.");
            if (input.TotalSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(input), "TotalSteps must be at least 1.");
            if (input.FurthestStep < 0)
                throw new ArgumentOutOfRangeException(nameof(input), "FurthestStep cannot be negative.");
        }

        static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
