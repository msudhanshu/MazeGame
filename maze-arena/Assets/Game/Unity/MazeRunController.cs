using Game.Core;
using Game.Unity.Memory;
using Nixin.Maze;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Drives a campaign attempt: rebuilds the arena from Core spec + seed, ticks the timer,
    /// and detects the exit cell.
    /// </summary>
    public sealed class MazeRunController : MonoBehaviour
    {
        public MazeArena Arena;
        public MemoryArenaMemory Memory;
        public MazeWalker Walker;
        public MazeHud Hud;

        int _nextSeed = 1;

        public MazeRun Run { get; private set; }

        public void StartCampaign(int seed)
        {
            _nextSeed = seed == 0 ? 1 : seed;
            ResetMemoryGenreHistory();
            Run = MazeRun.Start(CampaignCatalog.Default(), _nextSeed);
            ApplyAttempt();
        }

        public void Retry()
        {
            if (Run == null || Run.Phase != MazeRunPhase.Failed)
                return;
            ResetMemoryGenreHistory();
            Run.BeginAttempt(NextSeed());
            ApplyAttempt();
        }

        public void PlayAgain()
        {
            if (Run == null)
                return;
            ResetMemoryGenreHistory();
            Run.RestartCampaign(NextSeed());
            ApplyAttempt();
        }

        void Update()
        {
            if (Run == null)
                return;

            if (Run.Phase == MazeRunPhase.Playing)
            {
                Run.Tick(Time.deltaTime);
                if (Run.Phase == MazeRunPhase.Playing
                    && Walker != null
                    && Arena != null
                    && Arena.Layout != null
                    && Arena.Dimensions != null
                    && MazeGeometry.TryCellFromWorld(
                        Walker.transform.position,
                        Arena.Dimensions,
                        Arena.Layout.Size,
                        out var cell)
                    && cell == Arena.Layout.Exit.Cell)
                {
                    Run.ReachExit();
                }
            }

            if (Run.Phase == MazeRunPhase.Won)
            {
                Run.BeginAttempt(NextSeed());
                ApplyAttempt();
            }
        }

        void ApplyAttempt()
        {
            if (Run == null || Arena == null)
                return;

            var spec = Run.CurrentSpec;
            Arena.Width = spec.Size.Width;
            Arena.Height = spec.Size.Height;
            Arena.Difficulty = spec.Difficulty;
            Arena.BraidFactor = spec.BraidFactor;
            Arena.Openings = spec.Openings;
            Arena.PaintPhotos = false;
            Arena.Rebuild(Run.AttemptSeed);

            if (Memory != null)
                Memory.Decorate(Run.AttemptSeed);

            if (Walker != null)
            {
                Walker.PlaceAtEntry();
                Walker.SetWalking(true);
            }

            if (Hud != null)
                Hud.SyncFromRun(Run);
        }

        void ResetMemoryGenreHistory()
        {
            if (Memory != null)
                Memory.ResetGenreHistory();
        }

        int NextSeed()
        {
            unchecked
            {
                _nextSeed = _nextSeed * 1103515245 + 12345;
            }

            if (Run != null && _nextSeed == Run.AttemptSeed)
                _nextSeed++;
            if (_nextSeed == 0)
                _nextSeed = 1;
            return _nextSeed;
        }
    }
}
