using Game.Core.State;

namespace Game.Core.Fue
{
    /// <summary>
    /// First Graph Arena level coach: finger on the next node until the goal.
    /// Does not alter gameplay rules.
    /// </summary>
    public sealed class GraphLevelOneFueSession
    {
        public GraphLevelOneFueBeat Beat { get; private set; } = GraphLevelOneFueBeat.PromptTap;

        public bool IsActive => Beat != GraphLevelOneFueBeat.Completed;
        public bool IsComplete => Beat == GraphLevelOneFueBeat.Completed;

        public static GraphLevelOneFueSession TryStart(GameModeId mode, int levelNumber, bool alreadySeen)
        {
            if (!GraphLevelOneFueSpec.ShouldStart(mode, levelNumber, alreadySeen))
                return null;

            return new GraphLevelOneFueSession();
        }

        public void OnCorrectStep(bool reachedGoal)
        {
            if (IsComplete)
                return;

            Beat = reachedGoal ? GraphLevelOneFueBeat.Completed : GraphLevelOneFueBeat.PromptTap;
        }

        public void OnWrongStep()
        {
            if (IsComplete)
                return;

            Beat = GraphLevelOneFueBeat.HealthHint;
        }
    }
}
