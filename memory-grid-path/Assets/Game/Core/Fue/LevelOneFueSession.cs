using Game.Core.State;

namespace Game.Core.Fue
{
    /// <summary>
    /// Lightweight first Tile Arena level coach. Does not alter gameplay rules.
    /// </summary>
    public sealed class LevelOneFueSession
    {
        public LevelOneFueBeat Beat { get; private set; } = LevelOneFueBeat.PromptMove;

        public bool IsActive => Beat != LevelOneFueBeat.Completed;
        public bool IsComplete => Beat == LevelOneFueBeat.Completed;

        public static LevelOneFueSession TryStart(GameModeId mode, int levelNumber, bool alreadySeen)
        {
            if (!LevelOneFueSpec.ShouldStart(mode, levelNumber, alreadySeen))
                return null;

            return new LevelOneFueSession();
        }

        public void OnCorrectStep()
        {
            if (IsComplete)
                return;

            Beat = LevelOneFueBeat.Completed;
        }

        public void OnWrongStep()
        {
            if (IsComplete || Beat != LevelOneFueBeat.PromptMove)
                return;

            Beat = LevelOneFueBeat.HealthHint;
        }
    }
}
