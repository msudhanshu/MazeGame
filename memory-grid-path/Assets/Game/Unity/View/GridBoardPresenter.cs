using Game.Core.Domain;
using Game.Core.Rules;

namespace Game.Unity.View
{
    /// <summary>
    /// Paints grid tiles from a walk. Walked and revealed cells are applied last so a
    /// correction always reads as white, even when that cell is also a candidate or the goal.
    /// </summary>
    public static class GridBoardPresenter
    {
        public static void Refresh(
            GridBoardView board,
            GridWalkRun run,
            bool showPath = false,
            System.Collections.Generic.IReadOnlyList<Nixin.Grid.Core.GridCoord> visibleOptions = null,
            bool showChoicePaths = false)
        {
            if (board == null || !board.IsBuilt || run == null)
                return;

            visibleOptions ??= PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options());
            board.SetAll(TileVisualState.Idle);

            foreach (var blocked in run.BlockedCells)
                board.SetState(blocked, TileVisualState.Blocked);

            if (!run.IsOver)
            {
                foreach (var option in visibleOptions)
                    board.SetState(option, TileVisualState.Candidate);
            }

            foreach (var lighthouse in run.LighthouseCells)
                board.SetState(lighthouse, TileVisualState.Lighthouse);

            foreach (var pickup in run.UnusedPickups)
                board.SetState(pickup.Cell, TileVisualState.Pickup);

            if (!run.IsLevelCompleted)
                board.SetState(run.Path.Goal, TileVisualState.Goal);

            foreach (var walked in run.WalkedCells)
                board.SetState(walked, TileVisualState.Walked);

            foreach (var revealed in run.RevealedCells)
                board.SetState(revealed, TileVisualState.Walked);

            if (showPath)
            {
                foreach (var cell in run.Path.Cells)
                {
                    if (!cell.Equals(run.Path.Start) && !cell.Equals(run.Path.Goal))
                        board.SetState(cell, TileVisualState.Walked);
                }
            }

            if (run.LastRevealed.HasValue)
                board.SetState(run.LastRevealed.Value, TileVisualState.Walked);

            if (run.Step == 0)
                board.SetState(run.Path.Start, TileVisualState.Start);

            board.Overlay?.Refresh(run, board.Layout, showPath);
            if (showChoicePaths && !run.IsOver && visibleOptions != null && visibleOptions.Count > 0)
            {
                var points = new UnityEngine.Vector3[visibleOptions.Count];
                for (var i = 0; i < visibleOptions.Count; i++)
                    points[i] = board.WorldPosition(visibleOptions[i]) + UnityEngine.Vector3.up * GridPathOverlay.Lift;

                board.Overlay?.ShowChoices(
                    board.WorldPosition(run.CurrentCell) + UnityEngine.Vector3.up * GridPathOverlay.Lift,
                    points,
                    board.Layout.TileSize * 0.09f);
            }
            else
            {
                board.Overlay?.ClearChoices();
            }
        }
    }
}
