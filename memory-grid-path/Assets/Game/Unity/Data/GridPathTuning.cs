using System;
using Game.Core.Domain;
using UnityEngine;

namespace Game.Unity.Data
{
    [Serializable]
    public struct LevelRow
    {
        public int Side;
        public int Width;
        public int Height;
        public int MinTurns;
        public int Lives;
        public int Runs;
        public int Lighthouses;
        public int Glimpse;
        public int Beacon;
        public int BlockedHints;
        public int LengthSlack;

        public static LevelRow FromSpec(LevelSpec spec) => new LevelRow
        {
            Side = spec.Side,
            Width = spec.Width,
            Height = spec.Height,
            MinTurns = spec.MinTurns,
            Lives = spec.Lives,
            Runs = spec.Runs,
            Lighthouses = spec.Lighthouses,
            Glimpse = spec.Glimpse,
            Beacon = spec.Beacon,
            BlockedHints = spec.BlockedHints,
            LengthSlack = spec.LengthSlack
        };

        public LevelSpec ToSpec() =>
            new LevelSpec(
                Side,
                MinTurns,
                Lives,
                Lighthouses,
                Glimpse,
                Beacon,
                LengthSlack,
                Runs < 1 ? 5 : Runs,
                Width,
                Height,
                BlockedHints);
    }

    /// <summary>
    /// Designer-facing copy of scoring, skip, and the 25-level table. Duplicate the asset to
    /// try a tuning without touching Core defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "GridPathTuning", menuName = "Nixin Studio/Memory Grid Path/Tuning")]
    public sealed class GridPathTuning : ScriptableObject
    {
        [Header("Scoring")]
        [SerializeField] int _scoutPoints = 25;
        [SerializeField] int _memoryPoints = 100;
        [SerializeField] int _mistakePenalty = 50;
        [SerializeField] int _completionBonus = 400;
        [SerializeField] int _unusedRunBonus = 120;

        [Header("Skip window")]
        [SerializeField] bool _skipEnabled = true;
        [SerializeField] int _skipEligibleAfterLevel = 8;
        [SerializeField] int _skipStreakLength = 3;
        [SerializeField] int _skipWindow = 3;
        [SerializeField] int _skipGradeThreshold = 80;
        [SerializeField] int _skipRewardPoints = 250;

        [Header("Level ladder")]
        [SerializeField] LevelRow[] _levels = Array.Empty<LevelRow>();

        public GameConfig CreateGameConfig() =>
            new GameConfig(
                scoutPoints: _scoutPoints,
                memoryPoints: _memoryPoints,
                mistakePenalty: _mistakePenalty,
                completionBonus: _completionBonus,
                unusedRunBonus: _unusedRunBonus,
                skipEnabled: _skipEnabled,
                skipEligibleAfterLevel: _skipEligibleAfterLevel,
                skipStreakLength: _skipStreakLength,
                skipWindow: _skipWindow,
                skipGradeThreshold: _skipGradeThreshold,
                skipRewardPoints: _skipRewardPoints);

        public LevelCatalog CreateCatalog()
        {
            if (_levels == null || _levels.Length == 0)
                return new LevelCatalog();

            var specs = new LevelSpec[_levels.Length];
            for (var i = 0; i < _levels.Length; i++)
                specs[i] = _levels[i].ToSpec();

            return new LevelCatalog(LevelCatalog.FromSpecs(specs));
        }

        public void ApplyNixinDefaults()
        {
            var defaults = GameConfig.Default;
            _scoutPoints = defaults.ScoutPoints;
            _memoryPoints = defaults.MemoryPoints;
            _mistakePenalty = defaults.MistakePenalty;
            _completionBonus = defaults.CompletionBonus;
            _unusedRunBonus = defaults.UnusedRunBonus;
            _skipEnabled = defaults.SkipEnabled;
            _skipEligibleAfterLevel = defaults.SkipEligibleAfterLevel;
            _skipStreakLength = defaults.SkipStreakLength;
            _skipWindow = defaults.SkipWindow;
            _skipGradeThreshold = defaults.SkipGradeThreshold;
            _skipRewardPoints = defaults.SkipRewardPoints;

            var source = LevelCatalog.NixinDefaultSpecs;
            _levels = new LevelRow[source.Count];
            for (var i = 0; i < source.Count; i++)
                _levels[i] = LevelRow.FromSpec(source[i]);
        }

        void Reset() => ApplyNixinDefaults();
    }
}
