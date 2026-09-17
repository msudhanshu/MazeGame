using System.Collections.Generic;
using Game.Unity.Data;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    /// <summary>Maps authored node positions to world space on the XZ plane.</summary>
    public sealed class GraphBoardLayout
    {
        public const float StandingPickRadius = 0.05f;

        readonly Dictionary<GraphNodeId, Vector3> _positions = new Dictionary<GraphNodeId, Vector3>();
        readonly List<GraphEdgePolyline> _edges = new List<GraphEdgePolyline>();

        public GraphBoardLayout(GraphLevelDefinition level, Vector3 origin)
        {
            if (level == null)
                throw new System.ArgumentNullException(nameof(level));

            Origin = origin;
            GraphImageFit.WorldSize(level.WorldWidth, level.ImageAspect, out var width, out var depth);
            WorldWidth = width;
            WorldDepth = depth;

            for (var i = 0; i < level.Nodes.Count; i++)
            {
                var entry = level.Nodes[i];
                var id = new GraphNodeId(entry.Id);
                _positions[id] = GraphImageFit.NormalizedToWorld(
                    entry.NormalizedPosition,
                    WorldWidth,
                    WorldDepth,
                    origin);
            }

            for (var i = 0; i < level.Edges.Count; i++)
            {
                var edge = level.Edges[i];
                var a = new GraphNodeId(edge.NodeA);
                var b = new GraphNodeId(edge.NodeB);
                if (!_positions.ContainsKey(a) || !_positions.ContainsKey(b))
                    continue;

                Vector2 posA = default;
                Vector2 posB = default;
                for (var n = 0; n < level.Nodes.Count; n++)
                {
                    if (level.Nodes[n].Id == edge.NodeA)
                        posA = level.Nodes[n].NormalizedPosition;
                    if (level.Nodes[n].Id == edge.NodeB)
                        posB = level.Nodes[n].NormalizedPosition;
                }

                _edges.Add(new GraphEdgePolyline(
                    a,
                    b,
                    GraphEdgePath.WorldSamples(edge, posA, posB, WorldWidth, WorldDepth, origin)));
            }
        }

        public Vector3 Origin { get; }
        public float WorldWidth { get; }
        public float WorldDepth { get; }

        public Vector3 WorldPosition(GraphNodeId nodeId)
        {
            if (!_positions.TryGetValue(nodeId, out var position))
                throw new System.ArgumentException($"Unknown node '{nodeId.Value}'.", nameof(nodeId));

            return position;
        }

        public bool TryNodeAt(Vector3 worldPosition, float pickRadius, out GraphNodeId nodeId)
        {
            nodeId = default;
            var bestDistance = pickRadius * pickRadius;
            var found = false;

            foreach (var pair in _positions)
            {
                var delta = pair.Value - worldPosition;
                delta.y = 0f;
                var distance = delta.sqrMagnitude;
                if (distance > bestDistance)
                    continue;

                bestDistance = distance;
                nodeId = pair.Key;
                found = true;
            }

            return found;
        }

        public bool TryEdgePoints(GraphNodeId from, GraphNodeId to, out Vector3[] points)
        {
            points = null;
            for (var i = 0; i < _edges.Count; i++)
            {
                var edge = _edges[i];
                if (edge.A == from && edge.B == to)
                {
                    points = CopyPoints(edge.Points);
                    return points != null && points.Length >= 2;
                }

                if (edge.A == to && edge.B == from)
                {
                    points = CopyPoints(edge.Points);
                    Reverse(points);
                    return points != null && points.Length >= 2;
                }
            }

            return false;
        }

        public Vector3[] EdgeWorldPoints(GraphNodeId from, GraphNodeId to)
        {
            if (TryEdgePoints(from, to, out var points))
                return points;

            return new[] { WorldPosition(from), WorldPosition(to) };
        }

        public Vector3[] RouteWorldPoints(IReadOnlyList<GraphNodeId> nodes, float yLift = 0f)
        {
            if (nodes == null || nodes.Count == 0)
                return System.Array.Empty<Vector3>();

            if (nodes.Count == 1)
                return new[] { WorldPosition(nodes[0]) + Vector3.up * yLift };

            var points = new List<Vector3>(nodes.Count * 8);
            for (var i = 1; i < nodes.Count; i++)
            {
                var edge = EdgeWorldPoints(nodes[i - 1], nodes[i]);
                var start = points.Count == 0 ? 0 : 1;
                for (var j = start; j < edge.Length; j++)
                    points.Add(edge[j] + Vector3.up * yLift);
            }

            return points.ToArray();
        }

        public bool TryNodeUnderRay(Ray ray, float pickRadius, out GraphNodeId nodeId)
        {
            nodeId = default;
            var plane = new Plane(Vector3.up, Origin);
            if (!plane.Raycast(ray, out var distance))
                return false;

            return TryNodeAt(ray.GetPoint(distance), pickRadius, out nodeId);
        }

        public bool TryPickOptionUnderRay(
            Ray ray,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            float nodePickRadius,
            float edgePickRadius,
            out GraphNodeId target)
        {
            target = default;
            var plane = new Plane(Vector3.up, Origin);
            if (!plane.Raycast(ray, out var distance))
                return false;

            return TryPickOption(ray.GetPoint(distance), current, options, nodePickRadius, edgePickRadius, out target);
        }

        public bool TryPickOption(
            Vector3 worldPoint,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options,
            float nodePickRadius,
            float edgePickRadius,
            out GraphNodeId target)
        {
            target = default;
            if (options == null || options.Count == 0)
                return false;

            var point = worldPoint;
            point.y = Origin.y;

            Vector3 currentWorld;
            try
            {
                currentWorld = WorldPosition(current);
            }
            catch (System.ArgumentException)
            {
                return false;
            }

            currentWorld.y = Origin.y;
            var standing = StandingPickRadius * StandingPickRadius;
            var toStanding = currentWorld - point;
            toStanding.y = 0f;
            if (toStanding.sqrMagnitude <= standing)
                return false;

            var bestNode = nodePickRadius * nodePickRadius;
            var foundNode = false;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var delta = WorldPosition(option) - point;
                delta.y = 0f;
                var distance = delta.sqrMagnitude;
                if (distance > bestNode)
                    continue;

                bestNode = distance;
                target = option;
                foundNode = true;
            }

            if (foundNode)
                return true;

            var bestEdge = edgePickRadius * edgePickRadius;
            var foundEdge = false;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var path = EdgeWorldPoints(current, option);
                var distance = SqrDistancePointToPolyline(point, path);
                if (distance > bestEdge)
                    continue;

                bestEdge = distance;
                target = option;
                foundEdge = true;
            }

            return foundEdge;
        }

        static float SqrDistancePointToPolyline(Vector3 point, Vector3[] path)
        {
            if (path == null || path.Length < 2)
                return float.MaxValue;

            var best = float.MaxValue;
            for (var i = 1; i < path.Length; i++)
            {
                var dist = SqrDistancePointToSegment(point, path[i - 1], path[i]);
                if (dist < best)
                    best = dist;
            }

            return best;
        }

        static float SqrDistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            ab.y = 0f;
            var ap = point - a;
            ap.y = 0f;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 0.0000001f)
                return ap.sqrMagnitude;

            var t = Mathf.Clamp01(Vector3.Dot(ap, ab) / lengthSq);
            var closest = a + ab * t;
            var delta = point - closest;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        static Vector3[] CopyPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return System.Array.Empty<Vector3>();

            var copy = new Vector3[points.Length];
            for (var i = 0; i < points.Length; i++)
                copy[i] = points[i];
            return copy;
        }

        static void Reverse(Vector3[] points)
        {
            if (points == null)
                return;

            var left = 0;
            var right = points.Length - 1;
            while (left < right)
            {
                var swap = points[left];
                points[left] = points[right];
                points[right] = swap;
                left++;
                right--;
            }
        }

        readonly struct GraphEdgePolyline
        {
            public GraphEdgePolyline(GraphNodeId a, GraphNodeId b, Vector3[] points)
            {
                A = a;
                B = b;
                Points = points;
            }

            public GraphNodeId A { get; }
            public GraphNodeId B { get; }
            public Vector3[] Points { get; }
        }
    }
}
