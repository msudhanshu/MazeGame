using Nixin.Grid.Core;

namespace Game.Unity.View
{
    /// <summary>
    /// One tile on the board. Swapping this implementation is how the board changes look
    /// without touching the rules or the play loop.
    /// </summary>
    public interface ITileView
    {
        GridCoord Coord { get; }
        TileVisualState State { get; }

        void SetState(TileVisualState state);

        /// <summary>A one-shot flash on top of the current state, used for step feedback.</summary>
        void Flash(TileVisualState state, float seconds);

        void Destroy();
    }
}
