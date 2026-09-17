using System;
using Game.Core.Domain;
using Game.Core.Rules;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.State
{
    /// <summary>
    /// Ties the level ladder, the path generator, the active session, and saved progress
    /// together so the Unity layer only has to render and forward taps.
    /// </summary>
    public sealed class GridPathGame
    {
        readonly LevelPathFactory _paths;

        public GridPathGame(LevelCatalog catalog = null, GameConfig config = null, PlayerProgress progress = null)
        {
            Catalog = catalog ?? new LevelCatalog();
            Config = config ?? GameConfig.Default;
            Progress = progress ?? new PlayerProgress();
            _paths = new LevelPathFactory(Catalog);
        }

        public LevelCatalog Catalog { get; }
        public GameConfig Config { get; }
        public PlayerProgress Progress { get; }

        public LevelDefinition CurrentLevel { get; private set; }
        public GridWalkRun Run { get; private set; }

        public bool IsPlaying => Run != null && !Run.IsOver && !Run.IsAwaitingNextWalk;

        public bool CanSkip =>
            Config.SkipEnabled
            && Progress.SkipCharges > 0
            && CurrentLevel != null
            && Run == null;

        /// <summary>The level to offer next, which is the current one until it has been cleared.</summary>
        public int NextLevelNumber
        {
            get
            {
                if (CurrentLevel == null)
                    return 1;

                var next = CurrentLevel.Number + 1;
                if (!Catalog.Contains(next))
                    return CurrentLevel.Number;

                if (Progress.HighestUnlockedLevel >= next)
                    return next;

                return CurrentLevel.Number;
            }
        }

        public void StartLevel(int levelNumber, int seed)
        {
            SelectLevel(levelNumber);
            StartPlay(seed);
        }

        public void SelectLevel(int levelNumber)
        {
            if (!Progress.IsUnlocked(levelNumber))
                throw new InvalidOperationException($"Level {levelNumber} is locked; the player has reached level {Progress.HighestUnlockedLevel}.");

            CurrentLevel = Catalog.Get(levelNumber);
            Run = null;
        }

        public void StartPlay(int seed)
        {
            if (CurrentLevel == null)
                throw new InvalidOperationException("No level is selected.");

            const int attempts = 24;
            Exception last = null;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                try
                {
                    var path = _paths.Create(CurrentLevel.Number, seed + attempt);
                    var aids = PathAidPlacer.Place(
                        path,
                        CurrentLevel,
                        new XorShiftRandom(unchecked(seed + attempt + 7919)));
                    var blocked = OffPathHintPlacer.Place(
                        path,
                        CurrentLevel,
                        new XorShiftRandom(unchecked(seed + attempt + 12011)));
                    aids = new PathAids(aids.Lighthouses, aids.Pickups, blocked);
                    var config = Config.WithBudget(CurrentLevel.LivesPerRun, CurrentLevel.RunsPerSession);
                    Run = new GridWalkRun(path, config, aids);
                    return;
                }
                catch (Exception exception)
                {
                    last = exception;
                }
            }

            throw new InvalidOperationException(
                $"Could not start level {CurrentLevel.Number} after {attempts} path attempts.", last);
        }

        public void SkipCurrentLevel()
        {
            if (!CanSkip)
                throw new InvalidOperationException("This level cannot be skipped.");

            Progress.SkipLevel(CurrentLevel.Number, Config.SkipRewardPoints, Catalog.Count);
        }

        public WalkOutcome Choose(GridCoord cell)
        {
            if (Run == null)
                throw new InvalidOperationException("No level is in progress.");

            var outcome = Run.Choose(cell);
            if (outcome == WalkOutcome.LevelCompleted || outcome == WalkOutcome.SessionOver)
                Progress.RecordResult(
                    CurrentLevel.Number,
                    Run.Score,
                    Run.IsLevelCompleted,
                    Catalog.Count,
                    Run.MemoryGrade,
                    Config,
                    Run.MistakesMade);

            return outcome;
        }

        public void BeginNextWalk()
        {
            if (Run == null)
                throw new InvalidOperationException("No level is in progress.");

            Run.BeginNextWalk();
        }
    }
}
