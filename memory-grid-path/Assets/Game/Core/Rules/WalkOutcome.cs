namespace Game.Core.Rules
{
    public enum WalkOutcome
    {
        /// <summary>Correct tile; the walker moved forward.</summary>
        Advanced,

        /// <summary>Wrong tile; a life was spent, the correct tile was shown, and the walker moved onto it.</summary>
        WrongRevealed,

        /// <summary>The last life of this walk was spent; the next walk starts over from the beginning.</summary>
        RunFailed,

        /// <summary>The walker reached the goal.</summary>
        LevelCompleted,

        /// <summary>The last life of the last walk was spent; the level is scored as it stands.</summary>
        SessionOver
    }
}
