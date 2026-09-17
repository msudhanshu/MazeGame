using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Data
{
    [Serializable]
    public sealed class ExtractedGraphFile
    {
        public string displayName;
        public int imageWidth;
        public int imageHeight;
        public ExtractedGraphNode[] nodes;
        public ExtractedGraphEdge[] edges;
    }

    [Serializable]
    public sealed class ExtractedGraphNode
    {
        public string id;
        public float x;
        public float y;
        public string kind;
    }

    [Serializable]
    public sealed class ExtractedGraphEdge
    {
        public string nodeA;
        public string nodeB;
        public ExtractedGraphControlPoint[] controlPoints;
    }

    [Serializable]
    public sealed class ExtractedGraphControlPoint
    {
        public float x;
        public float y;
        public float tangentDegrees;
        public float tangentLength;
    }

    /// <summary>
    /// JSON from tools/path-graph-extract → GraphLevelSnapshot (keeps Background).
    /// </summary>
    public static class ExtractedGraphJson
    {
        public static bool TryParse(string json, out ExtractedGraphFile file, out string error)
        {
            file = null;
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "JSON is empty.";
                return false;
            }

            file = JsonUtility.FromJson<ExtractedGraphFile>(json);
            if (file == null || file.nodes == null || file.nodes.Length == 0)
            {
                error = "JSON is missing nodes.";
                file = null;
                return false;
            }

            if (file.edges == null)
                file.edges = Array.Empty<ExtractedGraphEdge>();

            return true;
        }

        public static void Apply(GraphLevelSnapshot snapshot, ExtractedGraphFile file)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (!string.IsNullOrWhiteSpace(file.displayName))
                snapshot.DisplayName = file.displayName;

            if (file.imageWidth > 0 && file.imageHeight > 0)
                snapshot.ImageAspect = file.imageWidth / (float)file.imageHeight;

            snapshot.Nodes = new List<GraphNodeEntry>(file.nodes.Length);
            for (var i = 0; i < file.nodes.Length; i++)
            {
                var node = file.nodes[i];
                if (IsIntermediate(node.kind))
                    continue;

                snapshot.Nodes.Add(new GraphNodeEntry
                {
                    Id = string.IsNullOrWhiteSpace(node.id) ? "n" + i : node.id,
                    NormalizedPosition = new Vector2(node.x, node.y)
                });
            }

            var edges = file.edges ?? Array.Empty<ExtractedGraphEdge>();
            snapshot.Edges = new List<GraphEdgeEntry>(edges.Length);
            for (var i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                var points = new List<GraphEdgeControlPoint>();
                if (edge.controlPoints != null)
                {
                    for (var p = 0; p < edge.controlPoints.Length; p++)
                    {
                        var point = edge.controlPoints[p];
                        points.Add(new GraphEdgeControlPoint
                        {
                            NormalizedPosition = new Vector2(point.x, point.y),
                            TangentDegrees = point.tangentDegrees,
                            TangentLength = point.tangentLength
                        });
                    }
                }

                snapshot.Edges.Add(new GraphEdgeEntry
                {
                    NodeA = edge.nodeA,
                    NodeB = edge.nodeB,
                    ControlPoints = points
                });
            }

            AssignPlaceholders(snapshot);
            snapshot.NormalizeIds();
        }

        static void AssignPlaceholders(GraphLevelSnapshot snapshot)
        {
            if (snapshot.Nodes.Count == 0)
            {
                snapshot.StartNodeId = "n0";
                snapshot.GoalNodeId = "n0";
                return;
            }

            if (snapshot.Nodes.Count == 1)
            {
                snapshot.StartNodeId = snapshot.Nodes[0].Id;
                snapshot.GoalNodeId = snapshot.Nodes[0].Id;
                return;
            }

            var degree = new Dictionary<string, int>(snapshot.Nodes.Count);
            for (var i = 0; i < snapshot.Nodes.Count; i++)
                degree[snapshot.Nodes[i].Id] = 0;

            for (var i = 0; i < snapshot.Edges.Count; i++)
            {
                var edge = snapshot.Edges[i];
                if (degree.ContainsKey(edge.NodeA))
                    degree[edge.NodeA]++;
                if (degree.ContainsKey(edge.NodeB))
                    degree[edge.NodeB]++;
            }

            var playable = new List<GraphNodeEntry>();
            for (var i = 0; i < snapshot.Nodes.Count; i++)
            {
                if (degree[snapshot.Nodes[i].Id] >= 2)
                    playable.Add(snapshot.Nodes[i]);
            }

            var pool = playable.Count >= 2 ? playable : snapshot.Nodes;
            var start = pool[0];
            var goal = pool[pool.Count - 1];
            var best = -1f;
            for (var i = 0; i < pool.Count; i++)
            {
                for (var j = i + 1; j < pool.Count; j++)
                {
                    var delta = pool[i].NormalizedPosition - pool[j].NormalizedPosition;
                    var dist = delta.sqrMagnitude;
                    if (dist > best)
                    {
                        best = dist;
                        start = pool[i];
                        goal = pool[j];
                    }
                }
            }

            snapshot.StartNodeId = start.Id;
            snapshot.GoalNodeId = goal.Id;
        }

        static bool IsIntermediate(string kind)
        {
            return !string.IsNullOrEmpty(kind)
                && string.Equals(kind, "intermediate", StringComparison.OrdinalIgnoreCase);
        }
    }
}
