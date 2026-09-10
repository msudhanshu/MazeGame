using System;
using System.Collections.Generic;
using Game.Core.Domain;

namespace Game.Core.State
{
    /// <summary>
    /// What carries over between sessions: unlocks, per-level bests, career score, and skip charges.
    /// Local save stands in for a login until a backend exists.
    /// </summary>
    public sealed class PlayerProgress
    {
        readonly Dictionary<int, int> _bestScores = new Dictionary<int, int>();
        bool _skipGrantNotice;

        public PlayerProgress(
            int highestUnlockedLevel = 1,
            IReadOnlyDictionary<int, int> bestScores = null,
            int careerScore = 0,
            int skipCharges = 0,
            int highGradeStreak = 0,
            int highestClearedLevel = -1)
        {
            if (highestUnlockedLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(highestUnlockedLevel), "Level 1 is always unlocked.");
            if (careerScore < 0)
                throw new ArgumentOutOfRangeException(nameof(careerScore));
            if (skipCharges < 0)
                throw new ArgumentOutOfRangeException(nameof(skipCharges));
            if (highGradeStreak < 0)
                throw new ArgumentOutOfRangeException(nameof(highGradeStreak));

            HighestUnlockedLevel = highestUnlockedLevel;
            CareerScore = careerScore;
            SkipCharges = skipCharges;
            HighGradeStreak = highGradeStreak;
            HighestClearedLevel = highestClearedLevel >= 0
                ? highestClearedLevel
                : Math.Max(0, highestUnlockedLevel - 1);

            if (bestScores == null)
                return;

            foreach (var entry in bestScores)
                _bestScores[entry.Key] = entry.Value;
        }

        public void Reset()
        {
            HighestUnlockedLevel = 1;
            HighestClearedLevel = 0;
            CareerScore = 0;
            SkipCharges = 0;
            HighGradeStreak = 0;
            _skipGrantNotice = false;
            _bestScores.Clear();
        }

        public int HighestUnlockedLevel { get; private set; }
        public int HighestClearedLevel { get; private set; }
        public int CareerScore { get; private set; }
        public int SkipCharges { get; private set; }
        public int HighGradeStreak { get; private set; }

        public IReadOnlyDictionary<int, int> BestScores => _bestScores;

        public bool IsUnlocked(int levelNumber) => levelNumber >= 1 && levelNumber <= HighestUnlockedLevel;

        public int BestScoreFor(int levelNumber) =>
            _bestScores.TryGetValue(levelNumber, out var score) ? score : 0;

        /// <summary>True once after a skip window is granted, then clears.</summary>
        public bool ConsumeSkipGrantNotice()
        {
            var granted = _skipGrantNotice;
            _skipGrantNotice = false;
            return granted;
        }

        public void RecordResult(
            int levelNumber,
            int score,
            bool completed,
            int levelCount,
            int grade = 0,
            GameConfig config = null)
        {
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            if (score < 0)
                throw new ArgumentOutOfRangeException(nameof(score));
            if (levelCount < 1)
                throw new ArgumentOutOfRangeException(nameof(levelCount));
            if (grade < 0 || grade > 100)
                throw new ArgumentOutOfRangeException(nameof(grade));

            CareerScore += score;

            if (score > BestScoreFor(levelNumber))
                _bestScores[levelNumber] = score;

            if (completed)
                HighestClearedLevel = Math.Max(HighestClearedLevel, levelNumber);

            if (completed && levelNumber >= HighestUnlockedLevel && levelNumber < levelCount)
                HighestUnlockedLevel = levelNumber + 1;

            ApplySkipStreak(levelNumber, completed, grade, config);
        }

        public void SkipLevel(int levelNumber, int reward, int levelCount)
        {
            if (levelNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            if (reward < 0)
                throw new ArgumentOutOfRangeException(nameof(reward));
            if (levelCount < 1)
                throw new ArgumentOutOfRangeException(nameof(levelCount));
            if (SkipCharges < 1)
                throw new InvalidOperationException("No skip charges remain.");

            SkipCharges--;
            HighGradeStreak = 0;
            CareerScore += reward;
            HighestClearedLevel = Math.Max(HighestClearedLevel, levelNumber);

            if (reward > BestScoreFor(levelNumber))
                _bestScores[levelNumber] = reward;

            if (levelNumber >= HighestUnlockedLevel && levelNumber < levelCount)
                HighestUnlockedLevel = levelNumber + 1;
        }

        void ApplySkipStreak(int levelNumber, bool completed, int grade, GameConfig config)
        {
            if (config == null || !config.SkipEnabled)
                return;

            if (!completed || grade < config.SkipGradeThreshold || levelNumber <= config.SkipEligibleAfterLevel)
            {
                HighGradeStreak = 0;
                return;
            }

            if (SkipCharges > 0)
            {
                HighGradeStreak = 0;
                return;
            }

            HighGradeStreak++;
            if (HighGradeStreak < config.SkipStreakLength)
                return;

            SkipCharges = config.SkipWindow;
            HighGradeStreak = 0;
            _skipGrantNotice = true;
        }
    }
}
