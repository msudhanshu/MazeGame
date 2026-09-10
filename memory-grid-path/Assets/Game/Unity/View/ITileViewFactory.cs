using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Builds the tiles for one theme and dresses the scene around them. A second theme is a
    /// second implementation of this interface; nothing else has to change.
    /// </summary>
    public interface ITileViewFactory
    {
        string ThemeId { get; }

        ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent);

        /// <summary>Camera framing, background, lighting, and post-processing for this theme.</summary>
        void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent);
    }
}
