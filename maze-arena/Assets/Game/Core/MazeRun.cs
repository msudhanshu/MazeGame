using System;
using Nixin.Maze.Core;

namespace Game.Core
{
    public sealed class MazeRun
    {
        readonly CampaignCatalog _campaign;
        bool _hasAttempt;

        public MazeRun(CampaignCatalog campaign)
        {
            _campaign = campaign ?? throw new ArgumentNullException(nameof(campaign));
        }

        public int LevelIndex { get; private set; }
        public int AttemptSeed { get; private set; }
        public float TimeRemaining { get; private set; }
        public MazeRunPhase Phase { get; private set; }
        public int LevelCount => _campaign.Count;
        public CampaignLevel CurrentLevel => _campaign.Get(LevelIndex);
        public MazeSpec CurrentSpec => CurrentLevel.ToSpec();

        public static MazeRun Start(CampaignCatalog campaign, int seed)
        {
            var run = new MazeRun(campaign);
            run.BeginAttempt(seed);
            return run;
        }

        public void BeginAttempt(int seed)
        {
            if (Phase == MazeRunPhase.Complete)
                throw new InvalidOperationException("Campaign is complete. Call RestartCampaign to play again.");
            if (_hasAttempt && seed == AttemptSeed)
                throw new ArgumentException("A new attempt needs a different seed.", nameof(seed));

            AttemptSeed = seed;
            TimeRemaining = MazeTimeBudget.SecondsFor(CurrentSpec);
            Phase = MazeRunPhase.Playing;
            _hasAttempt = true;
        }

        public void Tick(float dt)
        {
            if (Phase != MazeRunPhase.Playing)
                return;
            if (dt < 0f)
                throw new ArgumentOutOfRangeException(nameof(dt));

            TimeRemaining -= dt;
            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                Phase = MazeRunPhase.Failed;
            }
        }

        public void ReachExit()
        {
            if (Phase != MazeRunPhase.Playing)
                return;

            if (LevelIndex >= _campaign.Count - 1)
            {
                Phase = MazeRunPhase.Complete;
                return;
            }

            LevelIndex++;
            Phase = MazeRunPhase.Won;
        }

        public void RestartCampaign(int seed)
        {
            LevelIndex = 0;
            Phase = MazeRunPhase.Playing;
            _hasAttempt = false;
            BeginAttempt(seed);
        }
    }
}
