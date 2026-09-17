using System.Collections;
using System.Collections.Generic;
using Game.Unity.Graph;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>Fades the walked path and avatar out, then the walk restarts at the origin.</summary>
    public static class WalkedTrailFade
    {
        public const float FadeSeconds = 0.8f;

        public static IEnumerator CrashOut(
            WalkerView walker,
            GridBoardView board,
            GridPathOverlay overlay,
            IReadOnlyList<GridCoord> walked,
            float seconds)
        {
            overlay?.ClearChoices();
            StripGridHighlights(board);
            var duration = Mathf.Max(0.35f, seconds > 0.01f ? seconds : FadeSeconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var fade = 1f - Mathf.Clamp01(elapsed / duration);
                walker?.SetBodyAlpha(fade);
                overlay?.SetFade(fade);
                yield return null;
            }

            walker?.SetBodyAlpha(0f);
            overlay?.ResetVisuals();
            RestoreIdle(board, walked);
        }

        public static IEnumerator CrashOutGraph(
            WalkerView walker,
            GraphBoardView board,
            IReadOnlyList<GraphNodeId> walked,
            float seconds)
        {
            _ = walked;
            if (board != null)
            {
                board.Overlay?.ClearChoices();
                board.SetAllEdgesVisible(false);
                board.SetAllNodesVisible(false);
            }

            var duration = Mathf.Max(0.35f, seconds > 0.01f ? seconds : FadeSeconds);
            var elapsed = 0f;
            var overlay = board != null ? board.Overlay : null;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var fade = 1f - Mathf.Clamp01(elapsed / duration);
                walker?.SetBodyAlpha(fade);
                overlay?.SetFade(fade);
                yield return null;
            }

            walker?.SetBodyAlpha(0f);
            overlay?.ResetVisuals();
            if (board != null)
            {
                board.SetAll(GraphNodeVisualState.Idle);
                board.SetAllEdgesVisible(false);
            }
        }

        static void StripGridHighlights(GridBoardView board)
        {
            if (board == null || !board.IsBuilt)
                return;

            foreach (var tile in board.Tiles.Values)
            {
                if (tile == null)
                    continue;
                if (tile.State == TileVisualState.Candidate
                    || tile.State == TileVisualState.Revealed
                    || tile.State == TileVisualState.Wrong
                    || tile.State == TileVisualState.WrongIntense
                    || tile.State == TileVisualState.Walked
                    || tile.State == TileVisualState.Start)
                    tile.SetState(TileVisualState.Idle);
            }
        }

        static void RestoreIdle(GridBoardView board, IReadOnlyList<GridCoord> walked)
        {
            if (board == null || walked == null)
                return;
            for (var i = 0; i < walked.Count; i++)
                board.TileAt(walked[i])?.SetState(TileVisualState.Idle);
        }
    }
}
