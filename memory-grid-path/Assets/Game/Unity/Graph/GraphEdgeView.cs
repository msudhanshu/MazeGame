using Game.Unity.Data;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public static class GraphEdgeView
    {
        public static GraphEdgeLine[] Build(GraphLevelDefinition level, GraphBoardLayout layout, Transform parent)
        {
            var root = new GameObject("Graph Edges");
            root.transform.SetParent(parent, false);

            var material = CreateLineMaterial();
            var edges = new GraphEdgeLine[level.Edges.Count];
            for (var i = 0; i < level.Edges.Count; i++)
            {
                var edge = level.Edges[i];
                var a = new GraphNodeId(edge.NodeA);
                var b = new GraphNodeId(edge.NodeB);
                var points = layout.EdgeWorldPoints(a, b);
                edges[i] = CreateEdge(root.transform, a, b, points, material);
            }

            return edges;
        }

        static GraphEdgeLine CreateEdge(
            Transform parent,
            GraphNodeId a,
            GraphNodeId b,
            Vector3[] worldPoints,
            Material material)
        {
            var go = new GameObject("Edge " + a.Value + "-" + b.Value);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            var points = worldPoints == null || worldPoints.Length < 2
                ? new[] { Vector3.zero, Vector3.zero }
                : worldPoints;
            line.positionCount = points.Length;
            line.useWorldSpace = true;
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.material = material;
            line.startColor = new Color(0.55f, 0.62f, 0.72f, 0.85f);
            line.endColor = line.startColor;
            for (var i = 0; i < points.Length; i++)
                line.SetPosition(i, points[i] + Vector3.up * 0.02f);

            var view = go.AddComponent<GraphEdgeLine>();
            view.Initialise(a, b, points);
            view.SetVisible(false);
            return view;
        }

        static Material CreateLineMaterial()
        {
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            return shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/Internal-Colored"));
        }
    }
}
