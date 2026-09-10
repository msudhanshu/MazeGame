namespace Game.Unity.View
{
    /// <summary>
    /// What a tile is currently saying to the player. Themes decide how each state looks;
    /// the play loop only ever talks in these terms.
    /// </summary>
    public enum TileVisualState
    {
        /// <summary>Ordinary floor tile with the path still hidden underneath.</summary>
        Idle,

        Start,
        Goal,

        /// <summary>A neighbouring tile the player may pick right now.</summary>
        Candidate,

        /// <summary>Confirmed part of the path in the current walk.</summary>
        Walked,

        /// <summary>Uncovered by a mistake; stays lit until the walk restarts.</summary>
        Revealed,

        /// <summary>A session-persistent white on the path. Same glow as a reveal for now.</summary>
        Lighthouse,

        /// <summary>An unused on-path pickup. Distinct pulse until collected.</summary>
        Pickup,

        /// <summary>The tile that was just picked and turned out to be off the path.</summary>
        Wrong,

        /// <summary>Off-path tile greyed out from the start; never a valid choice.</summary>
        Blocked
    }
}
