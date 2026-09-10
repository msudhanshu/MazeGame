using System;

namespace Game.Core.Domain
{
    /// <summary>
    /// Tunable scoring weights, skip rules, and a fallback walk budget. Per-level lives and
    /// session walks come from <see cref="LevelDefinition"/>.
    /// </summary>
    public sealed class GameConfig
    {
        public GameConfig(
            int livesPerRun = 3,
            int runsPerSession = 5,
            int scoutPoints = 25,
            int memoryPoints = 100,
            int mistakePenalty = 50,
            int completionBonus = 400,
            int unusedRunBonus = 120,
            bool skipEnabled = true,
            int skipEligibleAfterLevel = 8,
            int skipStreakLength = 3,
            int skipWindow = 3,
            int skipGradeThreshold = 80,
            int skipRewardPoints = 250)
        {
            if (livesPerRun < 1)
                throw new ArgumentOutOfRangeException(nameof(livesPerRun));
            if (runsPerSession < 1)
                throw new ArgumentOutOfRangeException(nameof(runsPerSession));
            if (scoutPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(scoutPoints));
            if (memoryPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(memoryPoints));
            if (mistakePenalty < 0)
                throw new ArgumentOutOfRangeException(nameof(mistakePenalty));
            if (completionBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(completionBonus));
            if (unusedRunBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(unusedRunBonus));
            if (skipEligibleAfterLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(skipEligibleAfterLevel));
            if (skipStreakLength < 1)
                throw new ArgumentOutOfRangeException(nameof(skipStreakLength));
            if (skipWindow < 1)
                throw new ArgumentOutOfRangeException(nameof(skipWindow));
            if (skipGradeThreshold < 0 || skipGradeThreshold > 100)
                throw new ArgumentOutOfRangeException(nameof(skipGradeThreshold));
            if (skipRewardPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(skipRewardPoints));

            LivesPerRun = livesPerRun;
            RunsPerSession = runsPerSession;
            ScoutPoints = scoutPoints;
            MemoryPoints = memoryPoints;
            MistakePenalty = mistakePenalty;
            CompletionBonus = completionBonus;
            UnusedRunBonus = unusedRunBonus;
            SkipEnabled = skipEnabled;
            SkipEligibleAfterLevel = skipEligibleAfterLevel;
            SkipStreakLength = skipStreakLength;
            SkipWindow = skipWindow;
            SkipGradeThreshold = skipGradeThreshold;
            SkipRewardPoints = skipRewardPoints;
        }

        public static GameConfig Default { get; } = new GameConfig();

        /// <summary>Copy scoring and skip rules onto a per-level walk budget.</summary>
        public GameConfig WithBudget(int livesPerRun, int runsPerSession) =>
            new GameConfig(
                livesPerRun,
                runsPerSession,
                ScoutPoints,
                MemoryPoints,
                MistakePenalty,
                CompletionBonus,
                UnusedRunBonus,
                SkipEnabled,
                SkipEligibleAfterLevel,
                SkipStreakLength,
                SkipWindow,
                SkipGradeThreshold,
                SkipRewardPoints);

        public int LivesPerRun { get; }
        public int RunsPerSession { get; }

        /// <summary>A first-time guess that had not been seen this session.</summary>
        public int ScoutPoints { get; }

        /// <summary>A correct step onto a cell the player had already seen.</summary>
        public int MemoryPoints { get; }

        public int MistakePenalty { get; }
        public int CompletionBonus { get; }
        public int UnusedRunBonus { get; }

        public bool SkipEnabled { get; }
        public int SkipEligibleAfterLevel { get; }
        public int SkipStreakLength { get; }
        public int SkipWindow { get; }
        public int SkipGradeThreshold { get; }
        public int SkipRewardPoints { get; }
    }
}
