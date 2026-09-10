using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.Data;
using Game.Unity.View;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public static class GraphBoardPresenter
    {
        public static void Refresh(
            GraphBoardView board,
            GraphWalkRun run,
            bool showPath,
            System.Collections.Generic.IReadOnlyList<Nixin.Graph.Core.GraphNodeId> visibleOptions = null)
        {
            if (board == null || !board.IsBuilt || run == null)
                return;

            var options = visibleOptions ?? PathOptionFilter.VisibleGraphOptions(run.WalkedNodes, run.Options());
            board.SetAll(GraphNodeVisualState.Idle);

            if (!run.IsOver)
            {
                foreach (var option in options)
                    board.SetState(option, GraphNodeVisualState.Candidate);
            }

            foreach (var walked in run.WalkedNodes)
                board.SetState(walked, GraphNodeVisualState.Walked);

            foreach (var revealed in run.RevealedNodes)
                board.SetState(revealed, GraphNodeVisualState.Revealed);

            if (showPath)
            {
                foreach (var node in run.Path.Nodes)
                {
                    if (node != run.Path.Start && node != run.Path.Goal)
                        board.SetState(node, GraphNodeVisualState.Revealed);
                }
            }

            board.SetState(run.Path.Goal, run.IsLevelCompleted ? GraphNodeVisualState.Walked : GraphNodeVisualState.Goal);

            if (run.Step == 0)
                board.SetState(run.Path.Start, GraphNodeVisualState.Start);

            board.SetEdgesVisible(run.CurrentNode, run.WalkedNodes, options, showPath);
            board.SetNodesVisible(run.CurrentNode, run.WalkedNodes, options, showPath);
            RefreshOverlay(board, run, showPath);
        }

        static void RefreshOverlay(GraphBoardView board, GraphWalkRun run, bool showPath)
        {
            var overlay = board.Overlay;
            if (overlay == null)
                return;

            overlay.gameObject.SetActive(true);

            var nodes = showPath || run.IsLevelCompleted ? run.Path.Nodes : run.WalkedNodes;
            var trail = board.RouteWorldPoints(nodes, GridPathOverlay.Lift);
            var markers = new Vector3[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
                markers[i] = board.WorldPosition(nodes[i]) + Vector3.up * GridPathOverlay.Lift;

            overlay.RefreshWorld(trail, markers, 0.1f, run.IsLevelCompleted);
        }

        public static GraphWalkRun CreateRun(GraphLevelDefinition level, int seed)
        {
            var topology = level.ToTopology();
            var path = GraphLevelRuntime.CreatePath(topology, level.StartNode, level.GoalNode, level.Shape, seed);
            return new GraphWalkRun(path, level.ToGameConfig());
        }
    }
}
