using Game.Core.State;

namespace Game.Core.Fue
{
    /// <summary>
    /// First Scout radar-tour coach: hold the scan control to pause, release to continue.
    /// </summary>
    public sealed class ScoutScanFueSession
    {
        public ScoutScanFueBeat Beat { get; private set; } = ScoutScanFueBeat.PromptHold;

        public bool IsActive => Beat != ScoutScanFueBeat.Completed;
        public bool IsComplete => Beat == ScoutScanFueBeat.Completed;

        public static ScoutScanFueSession TryStart(GameModeId mode, int levelNumber, bool alreadySeen)
        {
            if (!ScoutScanFueSpec.ShouldStart(mode, levelNumber, alreadySeen))
                return null;

            return new ScoutScanFueSession();
        }

        public void ObserveHold(bool held)
        {
            if (Beat == ScoutScanFueBeat.PromptHold && held)
                Beat = ScoutScanFueBeat.PromptRelease;

            if (Beat == ScoutScanFueBeat.PromptRelease && !held)
                Beat = ScoutScanFueBeat.Completed;
        }
    }
}
