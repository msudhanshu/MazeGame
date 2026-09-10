using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Input
{
    /// <summary>
    /// Turns a classified stroke into a board cell. A tap picks the tile under the pointer;
    /// a swipe steps one cell from where the walker currently stands.
    /// </summary>
    public static class BoardMove
    {
        public static bool TryResolve(
            StrokeResult stroke,
            Vector2 screenPosition,
            Camera camera,
            BoardLayout layout,
            GridCoord current,
            out GridCoord target)
        {
            target = default;

            switch (stroke.Kind)
            {
                case StrokeKind.Tap:
                    if (camera == null)
                        return false;
                    return layout.TryCoordUnderRay(camera.ScreenPointToRay(screenPosition), out target);

                case StrokeKind.Swipe:
                    target = current.Offset(stroke.Offset.X, stroke.Offset.Y);
                    return true;

                default:
                    return false;
            }
        }
    }
}
