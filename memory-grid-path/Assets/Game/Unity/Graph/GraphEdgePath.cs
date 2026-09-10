using System.Collections.Generic;
using Game.Core.Domain;
using Game.Unity.Data;
using UnityEngine;

namespace Game.Unity.Graph
{
    /// <summary>
    /// Samples an authored graph edge in photo UVs, editor GUI, or play-mode world space.
    /// Straight edges (no control points) stay two-point segments.
    /// </summary>
    public static class GraphEdgePath
    {
        public static GraphSplineKnot[] ToKnots(GraphEdgeEntry edge, Vector2 nodeA, Vector2 nodeB)
        {
            GraphSplineKnot[] controls = null;
            if (edge.HasCurve)
            {
                controls = new GraphSplineKnot[edge.ControlPoints.Count];
                for (var i = 0; i < edge.ControlPoints.Count; i++)
                {
                    var point = edge.ControlPoints[i];
                    controls[i] = new GraphSplineKnot(
                        point.NormalizedPosition.x,
                        point.NormalizedPosition.y,
                        point.TangentDegrees,
                        point.TangentLength);
                }
            }

            return GraphEdgeSpline.BuildKnots(nodeA.x, nodeA.y, nodeB.x, nodeB.y, controls);
        }

        public static Vector2[] GuiSamples(GraphEdgeEntry edge, Vector2 nodeA, Vector2 nodeB, Rect imageRect, int samplesPerSegment = GraphEdgeSpline.DefaultSamplesPerSegment)
        {
            if (!edge.HasCurve)
            {
                return new[]
                {
                    GraphImageFit.NormalizedToGui(imageRect, nodeA),
                    GraphImageFit.NormalizedToGui(imageRect, nodeB)
                };
            }

            SampleNormalized(edge, nodeA, nodeB, samplesPerSegment, out var xs, out var ys);
            var points = new Vector2[xs.Length];
            for (var i = 0; i < xs.Length; i++)
                points[i] = GraphImageFit.NormalizedToGui(imageRect, new Vector2(xs[i], ys[i]));
            return points;
        }

        public static Vector3[] WorldSamples(
            GraphEdgeEntry edge,
            Vector2 nodeA,
            Vector2 nodeB,
            float worldWidth,
            float worldDepth,
            Vector3 origin,
            int samplesPerSegment = GraphEdgeSpline.DefaultSamplesPerSegment)
        {
            if (!edge.HasCurve)
            {
                return new[]
                {
                    GraphImageFit.NormalizedToWorld(nodeA, worldWidth, worldDepth, origin),
                    GraphImageFit.NormalizedToWorld(nodeB, worldWidth, worldDepth, origin)
                };
            }

            SampleNormalized(edge, nodeA, nodeB, samplesPerSegment, out var xs, out var ys);
            var points = new Vector3[xs.Length];
            for (var i = 0; i < xs.Length; i++)
                points[i] = GraphImageFit.NormalizedToWorld(new Vector2(xs[i], ys[i]), worldWidth, worldDepth, origin);
            return points;
        }

        public static float SqrDistanceToGui(
            GraphEdgeEntry edge,
            Vector2 nodeA,
            Vector2 nodeB,
            Rect imageRect,
            Vector2 mouse)
        {
            var samples = GuiSamples(edge, nodeA, nodeB, imageRect);
            var best = float.MaxValue;
            for (var i = 1; i < samples.Length; i++)
            {
                GraphEdgeSpline.ProjectOnSegment(
                    mouse.x,
                    mouse.y,
                    samples[i - 1].x,
                    samples[i - 1].y,
                    samples[i].x,
                    samples[i].y,
                    out var qx,
                    out var qy,
                    out _);
                var dx = qx - mouse.x;
                var dy = qy - mouse.y;
                var dist = dx * dx + dy * dy;
                if (dist < best)
                    best = dist;
            }

            return best;
        }

        public static GraphEdgeControlPoint ControlPointFromKnot(GraphSplineKnot knot) =>
            new GraphEdgeControlPoint
            {
                NormalizedPosition = new Vector2(knot.X, knot.Y),
                TangentDegrees = knot.TangentDegrees,
                TangentLength = knot.TangentLength
            };

        static void SampleNormalized(
            GraphEdgeEntry edge,
            Vector2 nodeA,
            Vector2 nodeB,
            int samplesPerSegment,
            out float[] xs,
            out float[] ys)
        {
            var knots = ToKnots(edge, nodeA, nodeB);
            var count = GraphEdgeSpline.SampleCount(knots.Length, samplesPerSegment);
            xs = new float[count];
            ys = new float[count];
            GraphEdgeSpline.Sample(knots, samplesPerSegment, xs, ys);
        }
    }
}
