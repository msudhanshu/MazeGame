using System;
using System.Collections.Generic;
using Game.Core.Domain;
using UnityEngine;

namespace Game.Unity.Data
{
    /// <summary>In-memory editable copy of a <see cref="GraphLevelDefinition"/>.</summary>
    [Serializable]
    public sealed class GraphLevelSnapshot
    {
        public string DisplayName = "Graph Level";
        public Texture2D Background;
        public float ImageAspect;
        public float WorldWidth = 12f;
        public List<GraphNodeEntry> Nodes = new List<GraphNodeEntry>();
        public List<GraphEdgeEntry> Edges = new List<GraphEdgeEntry>();
        public string StartNodeId = "n0";
        public string GoalNodeId;
        public int MinPathLength = 4;
        public int MaxPathLength = 12;
        public int MinTurns;
        public int MaxTurns = 8;
        public int LivesPerRun = 3;
        public int RunsPerSession = 5;
        public PathPreviewKind PreviewKind = PathPreviewKind.CameraFlash;
        public float PreviewSeconds;

        public string ResolvedGoalNodeId =>
            string.IsNullOrEmpty(GoalNodeId)
                ? Nodes.Count > 0 ? Nodes[Nodes.Count - 1].Id : "n0"
                : GoalNodeId;

        public float ResolvedImageAspect =>
            ImageAspect > 0.01f ? ImageAspect : GraphLevelDefinition.AspectOf(Background);

        public GraphLevelSnapshot Clone()
        {
            var clone = new GraphLevelSnapshot
            {
                DisplayName = DisplayName,
                Background = Background,
                ImageAspect = ImageAspect,
                WorldWidth = WorldWidth,
                StartNodeId = StartNodeId,
                GoalNodeId = GoalNodeId,
                MinPathLength = MinPathLength,
                MaxPathLength = MaxPathLength,
                MinTurns = MinTurns,
                MaxTurns = MaxTurns,
                LivesPerRun = LivesPerRun,
                RunsPerSession = RunsPerSession,
                PreviewKind = PreviewKind,
                PreviewSeconds = PreviewSeconds
            };

            for (var i = 0; i < Nodes.Count; i++)
                clone.Nodes.Add(Nodes[i]);
            for (var i = 0; i < Edges.Count; i++)
                clone.Edges.Add(Edges[i].Clone());

            return clone;
        }

        public void NormalizeIds()
        {
            var start = StartNodeId;
            var goal = ResolvedGoalNodeId;
            Nodes = GraphLevelDefinition.DeduplicateNodesForEditor(Nodes, Edges, ref start, ref goal, out var dedupedEdges);
            Edges = dedupedEdges;
            StartNodeId = start;
            GoalNodeId = goal;
        }

        public void WriteTo(GraphLevelDefinition asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            NormalizeIds();
            asset.ApplySnapshot(Clone());
        }

        public static bool AreEqual(GraphLevelSnapshot a, GraphLevelSnapshot b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a == null || b == null)
                return false;

            if (a.DisplayName != b.DisplayName ||
                a.Background != b.Background ||
                !Mathf.Approximately(a.ImageAspect, b.ImageAspect) ||
                a.WorldWidth != b.WorldWidth ||
                a.StartNodeId != b.StartNodeId ||
                a.ResolvedGoalNodeId != b.ResolvedGoalNodeId ||
                a.MinPathLength != b.MinPathLength ||
                a.MaxPathLength != b.MaxPathLength ||
                a.MinTurns != b.MinTurns ||
                a.MaxTurns != b.MaxTurns ||
                a.LivesPerRun != b.LivesPerRun ||
                a.RunsPerSession != b.RunsPerSession ||
                a.PreviewKind != b.PreviewKind ||
                !Mathf.Approximately(a.PreviewSeconds, b.PreviewSeconds) ||
                a.Nodes.Count != b.Nodes.Count ||
                a.Edges.Count != b.Edges.Count)
                return false;

            for (var i = 0; i < a.Nodes.Count; i++)
            {
                if (a.Nodes[i].Id != b.Nodes[i].Id ||
                    a.Nodes[i].NormalizedPosition != b.Nodes[i].NormalizedPosition)
                    return false;
            }

            for (var i = 0; i < a.Edges.Count; i++)
            {
                if (!GraphEdgeEntry.ContentEquals(a.Edges[i], b.Edges[i]))
                    return false;
            }

            return true;
        }
    }
}
