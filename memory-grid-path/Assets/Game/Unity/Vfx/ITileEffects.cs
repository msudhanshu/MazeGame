using System.Collections.Generic;
using Game.Unity.View;

namespace Game.Unity.Vfx
{
    /// <summary>
    /// Step feedback, kept separate from the tile look so a theme can change how a mistake
    /// reads without changing what a mistake is.
    /// </summary>
    public interface ITileEffects
    {
        void PlayCorrect(ITileView tile);

        /// <param name="wrong">The tile the player picked.</param>
        /// <param name="revealed">The tile that was actually on the path.</param>
        void PlayMistake(ITileView wrong, ITileView revealed);

        void PlayRunFailed();

        /// <summary>
        /// Called when the entire walk/session ends (out of lives / game over).
        /// Should be the biggest, most noticeable failure sound.
        /// </summary>
        void PlaySessionFailed();

        void PlayLevelCompleted();

        /// <summary>Skeleton hook for a later VFX pass: flash the whole path, then restore.</summary>
        void PlayPathGlimpse(IReadOnlyList<ITileView> pathTiles);
    }
}
