using System.Collections.Generic;
using Game.Unity.Data;
using Game.Unity.Graph;
using NUnit.Framework;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GraphEdgePathTests
    {
        [Test]
        public void StraightEdgesStayTwoWorldPoints()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                var layout = new GraphBoardLayout(level, Vector3.zero);
                var points = layout.EdgeWorldPoints(new GraphNodeId("n0"), new GraphNodeId("n1"));

                Assert.That(points.Length, Is.EqualTo(2));
                Assert.That(points[0], Is.EqualTo(layout.WorldPosition(new GraphNodeId("n0"))));
                Assert.That(points[1], Is.EqualTo(layout.WorldPosition(new GraphNodeId("n1"))));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void AControlPointBendsTheSampledWorldPath()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                BendEdge(level, "n0", "n1", new Vector2(0.2f, 0.85f));
                var layout = new GraphBoardLayout(level, Vector3.zero);
                var points = layout.EdgeWorldPoints(new GraphNodeId("n0"), new GraphNodeId("n1"));
                var controlWorld = GraphImageFit.NormalizedToWorld(
                    new Vector2(0.2f, 0.85f),
                    layout.WorldWidth,
                    layout.WorldDepth,
                    layout.Origin);

                Assert.That(points.Length, Is.GreaterThan(2));
                Assert.That(MinSqrDistance(points, controlWorld), Is.LessThan(0.05f));

                var chordMid = (layout.WorldPosition(new GraphNodeId("n0")) + layout.WorldPosition(new GraphNodeId("n1"))) * 0.5f;
                Assert.That(MinSqrDistance(points, chordMid), Is.GreaterThan(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void SnapshotCloneDoesNotShareControlPointLists()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                BendEdge(level, "n0", "n1", new Vector2(0.2f, 0.8f));
                var draft = level.CaptureSnapshot();
                var saved = draft.Clone();

                Assert.That(GraphLevelSnapshot.AreEqual(draft, saved), Is.True);

                var edge = draft.Edges[0];
                edge.ControlPoints[0] = new GraphEdgeControlPoint
                {
                    NormalizedPosition = new Vector2(0.22f, 0.9f),
                    TangentDegrees = 20f,
                    TangentLength = 0.2f
                };
                draft.Edges[0] = edge;

                Assert.That(GraphLevelSnapshot.AreEqual(draft, saved), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        public static void BendEdge(GraphLevelDefinition level, string nodeA, string nodeB, Vector2 control)
        {
            var edges = new List<GraphEdgeEntry>(level.Edges.Count);
            for (var i = 0; i < level.Edges.Count; i++)
            {
                var edge = level.Edges[i].Clone();
                if ((edge.NodeA == nodeA && edge.NodeB == nodeB) ||
                    (edge.NodeA == nodeB && edge.NodeB == nodeA))
                {
                    edge.ControlPoints = new List<GraphEdgeControlPoint>
                    {
                        new GraphEdgeControlPoint
                        {
                            NormalizedPosition = control,
                            TangentDegrees = 0f,
                            TangentLength = 0.12f
                        }
                    };
                }

                edges.Add(edge);
            }

            level.SetRuntimeData(
                level.DisplayName,
                level.Background,
                level.Nodes,
                edges,
                level.StartNodeId,
                level.GoalNodeId);
        }

        static float MinSqrDistance(Vector3[] points, Vector3 target)
        {
            var best = float.MaxValue;
            for (var i = 0; i < points.Length; i++)
            {
                var delta = points[i] - target;
                delta.y = 0f;
                var dist = delta.sqrMagnitude;
                if (dist < best)
                    best = dist;
            }

            return best;
        }
    }
}
