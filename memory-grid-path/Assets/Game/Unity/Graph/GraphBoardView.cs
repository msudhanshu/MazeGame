using System.Collections.Generic;
using Game.Core.Domain;
using Game.Unity.Data;
using Game.Unity.View;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public sealed class GraphBoardView : MonoBehaviour
    {
        readonly Dictionary<GraphNodeId, IGraphNodeView> _nodes = new Dictionary<GraphNodeId, IGraphNodeView>();
        readonly List<GraphEdgeLine> _edges = new List<GraphEdgeLine>();
        GridPathOverlay _overlay;

        public GraphBoardLayout Layout { get; private set; }
        public bool IsBuilt { get; private set; }
        public GridPathOverlay Overlay => _overlay;

        public IReadOnlyDictionary<GraphNodeId, IGraphNodeView> Nodes => _nodes;
        public IReadOnlyList<GraphEdgeLine> Edges => _edges;

        public void Build(GraphLevelDefinition level, IGraphNodeViewFactory factory, Vector3 origin = default)
        {
            Clear();

            Layout = new GraphBoardLayout(level, origin);
            for (var i = 0; i < level.Nodes.Count; i++)
            {
                var entry = level.Nodes[i];
                var id = new GraphNodeId(entry.Id);
                _nodes[id] = factory.CreateNode(id, Layout.WorldPosition(id), transform);
            }

            var edges = GraphEdgeView.Build(level, Layout, transform);
            for (var i = 0; i < edges.Length; i++)
                _edges.Add(edges[i]);

            GraphBackgroundView.Build(level, Layout, transform);
            _overlay = GridPathOverlay.Ensure(transform);
            IsBuilt = true;
        }

        public IGraphNodeView NodeAt(GraphNodeId nodeId) =>
            _nodes.TryGetValue(nodeId, out var node) ? node : null;

        public void SetState(GraphNodeId nodeId, GraphNodeVisualState state) => NodeAt(nodeId)?.SetState(state);

        public void SetAll(GraphNodeVisualState state)
        {
            foreach (var node in _nodes.Values)
                node.SetState(state);
        }

        public void SetEdgesVisible(
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options,
            bool showAll)
        {
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                edge.SetVisible(showAll || GraphEdgeReveal.IsVisible(edge.NodeA, edge.NodeB, current, walked, options));
            }
        }

        public void SetNodesVisible(
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options,
            bool showAll)
        {
            _ = walked;
            foreach (var pair in _nodes)
                pair.Value.SetVisible(showAll || GraphNodeReveal.IsCircleVisible(pair.Key, current, options));
        }

        public void SetAllEdgesVisible(bool visible)
        {
            for (var i = 0; i < _edges.Count; i++)
                _edges[i].SetVisible(visible);
        }

        public void SetAllNodesVisible(bool visible)
        {
            foreach (var node in _nodes.Values)
                node.SetVisible(visible);
        }

        public void SetEdgeVisual(GraphNodeId a, GraphNodeId b, GraphEdgeVisualState visual)
        {
            for (var i = 0; i < _edges.Count; i++)
            {
                if (!_edges[i].Connects(a, b))
                    continue;
                _edges[i].SetVisible(true);
                _edges[i].SetVisual(visual);
                return;
            }
        }

        public void PaintOptionEdges(GraphNodeId current, IReadOnlyList<GraphNodeId> options)
        {
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                var option = OptionOnEdge(edge, current, options);
                edge.SetVisual(option ? GraphEdgeVisualState.Candidate : GraphEdgeVisualState.Idle);
            }
        }

        static bool OptionOnEdge(GraphEdgeLine edge, GraphNodeId current, IReadOnlyList<GraphNodeId> options)
        {
            if (options == null)
                return false;
            GraphNodeId other;
            if (edge.NodeA == current)
                other = edge.NodeB;
            else if (edge.NodeB == current)
                other = edge.NodeA;
            else
                return false;

            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] == other)
                    return true;
            }

            return false;
        }

        public Vector3 WorldPosition(GraphNodeId nodeId) => Layout.WorldPosition(nodeId);

        public Vector3[] EdgeWorldPoints(GraphNodeId from, GraphNodeId to) =>
            Layout.EdgeWorldPoints(from, to);

        public Vector3[] RouteWorldPoints(IReadOnlyList<GraphNodeId> nodes, float yLift = 0f) =>
            Layout.RouteWorldPoints(nodes, yLift);

        public void Clear()
        {
            foreach (var node in _nodes.Values)
                node.Destroy();

            _nodes.Clear();
            _edges.Clear();
            if (_overlay)
                _overlay.ResetVisuals();
            _overlay = null;

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            IsBuilt = false;
        }

        void OnDestroy() => Clear();
    }
}
