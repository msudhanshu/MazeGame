namespace Game.Unity.TestLab
{
    /// <summary>
    /// Scenarios exercised in the Arena Test Lab scene. Press 1–4 in Play Mode to switch.
    /// </summary>
    public enum TestLabScenario
    {
        /// <summary>Classic coloured dance floor with gaps — default path-finding look.</summary>
        ClassicColorPath = 1,

        /// <summary>One shared image across the board; each tile shows its coordinate label.</summary>
        MosaicCoordinateDebug = 2,

        /// <summary>Random texture per tile, seamless floor, camera follows the walker.</summary>
        PatchworkFollowWalker = 3,

        /// <summary>Graph junction arena on a background image.</summary>
        GraphNodeArena = 4
    }
}
