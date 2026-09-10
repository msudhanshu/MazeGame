using System;
using System.Collections.Generic;

namespace Game.Core.Domain
{
    /// <summary>
    /// One Hermite knot on an authored graph edge. Endpoints are the two nodes;
    /// intermediate knots are designer-placed curve handles.
    /// </summary>
    public readonly struct GraphSplineKnot
    {
        public GraphSplineKnot(float x, float y, float tangentDegrees, float tangentLength)
        {
            X = x;
            Y = y;
            TangentDegrees = tangentDegrees;
            TangentLength = tangentLength;
        }

        public float X { get; }
        public float Y { get; }
        public float TangentDegrees { get; }
        public float TangentLength { get; }
    }

    /// <summary>
    /// Piecewise cubic Hermite spline through graph-edge knots. A single tangent
    /// (angle + length) at each intermediate knot keeps the curve C1-smooth.
    /// </summary>
    public static class GraphEdgeSpline
    {
        public const int DefaultSamplesPerSegment = 12;

        public static GraphSplineKnot[] BuildKnots(
            float ax,
            float ay,
            float bx,
            float by,
            IReadOnlyList<GraphSplineKnot> controlPoints)
        {
            var controlCount = controlPoints == null ? 0 : controlPoints.Count;
            var knots = new GraphSplineKnot[controlCount + 2];
            var nextX = controlCount > 0 ? controlPoints[0].X : bx;
            var nextY = controlCount > 0 ? controlPoints[0].Y : by;
            knots[0] = Endpoint(ax, ay, nextX, nextY);

            for (var i = 0; i < controlCount; i++)
                knots[i + 1] = controlPoints[i];

            var prevX = controlCount > 0 ? controlPoints[controlCount - 1].X : ax;
            var prevY = controlCount > 0 ? controlPoints[controlCount - 1].Y : ay;
            knots[controlCount + 1] = EndpointToward(bx, by, prevX, prevY);
            return knots;
        }

        public static GraphSplineKnot Endpoint(float x, float y, float towardX, float towardY) =>
            new GraphSplineKnot(x, y, Degrees(x, y, towardX, towardY), ChordLength(x, y, towardX, towardY));

        public static GraphSplineKnot EndpointToward(float x, float y, float fromX, float fromY) =>
            new GraphSplineKnot(x, y, Degrees(fromX, fromY, x, y), ChordLength(fromX, fromY, x, y));

        public static float Degrees(float fromX, float fromY, float toX, float toY)
        {
            var dx = toX - fromX;
            var dy = toY - fromY;
            if (dx * dx + dy * dy < 0.00000001f)
                return 0f;

            return (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
        }

        public static float ChordLength(float ax, float ay, float bx, float by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            return Math.Max(0.0001f, (float)Math.Sqrt(dx * dx + dy * dy));
        }

        public static int SampleCount(int knotCount, int samplesPerSegment)
        {
            if (knotCount < 1)
                return 0;
            if (knotCount == 1)
                return 1;

            return (knotCount - 1) * Math.Max(1, samplesPerSegment) + 1;
        }

        public static void Evaluate(IReadOnlyList<GraphSplineKnot> knots, float t, out float x, out float y)
        {
            if (knots == null || knots.Count == 0)
            {
                x = 0f;
                y = 0f;
                return;
            }

            if (knots.Count == 1)
            {
                x = knots[0].X;
                y = knots[0].Y;
                return;
            }

            t = Clamp01(t);
            var segments = knots.Count - 1;
            var scaled = t * segments;
            var index = (int)scaled;
            float localT;
            if (index >= segments)
            {
                index = segments - 1;
                localT = 1f;
            }
            else
            {
                localT = scaled - index;
            }

            EvaluateSegment(knots[index], knots[index + 1], localT, out x, out y);
        }

        public static void EvaluateSegment(
            GraphSplineKnot a,
            GraphSplineKnot b,
            float t,
            out float x,
            out float y)
        {
            Hermite(a, b, Clamp01(t), out x, out y, out _, out _);
        }

        public static GraphSplineKnot KnotOnSegment(GraphSplineKnot a, GraphSplineKnot b, float t)
        {
            Hermite(a, b, Clamp01(t), out var x, out var y, out var dx, out var dy);
            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 0.0001f)
            {
                length = 0.5f * (ChordLength(a.X, a.Y, x, y) + ChordLength(x, y, b.X, b.Y));
                return new GraphSplineKnot(x, y, Degrees(a.X, a.Y, b.X, b.Y), Math.Max(0.0001f, length));
            }

            var degrees = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
            var adjacent = 0.5f * (ChordLength(a.X, a.Y, x, y) + ChordLength(x, y, b.X, b.Y));
            return new GraphSplineKnot(x, y, degrees, Math.Max(0.0001f, adjacent));
        }

        public static int Sample(
            IReadOnlyList<GraphSplineKnot> knots,
            int samplesPerSegment,
            float[] xs,
            float[] ys)
        {
            var count = SampleCount(knots == null ? 0 : knots.Count, samplesPerSegment);
            if (count == 0 || xs == null || ys == null)
                return 0;

            if (count == 1)
            {
                xs[0] = knots[0].X;
                ys[0] = knots[0].Y;
                return 1;
            }

            var denom = count - 1;
            var written = Math.Min(count, Math.Min(xs.Length, ys.Length));
            for (var i = 0; i < written; i++)
                Evaluate(knots, i / (float)denom, out xs[i], out ys[i]);

            return written;
        }

        public static bool TryClosestOnSpline(
            IReadOnlyList<GraphSplineKnot> knots,
            float x,
            float y,
            int samplesPerSegment,
            out int segmentIndex,
            out float t,
            out float closestX,
            out float closestY,
            out float sqrDistance)
        {
            segmentIndex = 0;
            t = 0f;
            closestX = 0f;
            closestY = 0f;
            sqrDistance = float.MaxValue;
            if (knots == null || knots.Count < 2)
                return false;

            var per = Math.Max(1, samplesPerSegment);
            var best = float.MaxValue;
            for (var s = 0; s < knots.Count - 1; s++)
            {
                EvaluateSegment(knots[s], knots[s + 1], 0f, out var prevX, out var prevY);
                for (var i = 1; i <= per; i++)
                {
                    var local = i / (float)per;
                    EvaluateSegment(knots[s], knots[s + 1], local, out var px, out var py);
                    ProjectOnSegment(x, y, prevX, prevY, px, py, out var qx, out var qy, out var segT);
                    var dx = qx - x;
                    var dy = qy - y;
                    var dist = dx * dx + dy * dy;
                    if (dist < best)
                    {
                        best = dist;
                        segmentIndex = s;
                        t = ((i - 1) + segT) / per;
                        closestX = qx;
                        closestY = qy;
                    }

                    prevX = px;
                    prevY = py;
                }
            }

            sqrDistance = best;
            return true;
        }

        public static void ProjectOnSegment(
            float px,
            float py,
            float ax,
            float ay,
            float bx,
            float by,
            out float qx,
            out float qy,
            out float t)
        {
            var abx = bx - ax;
            var aby = by - ay;
            var lengthSq = abx * abx + aby * aby;
            if (lengthSq < 0.00000001f)
            {
                qx = ax;
                qy = ay;
                t = 0f;
                return;
            }

            t = Clamp01(((px - ax) * abx + (py - ay) * aby) / lengthSq);
            qx = ax + abx * t;
            qy = ay + aby * t;
        }

        static void Hermite(
            GraphSplineKnot a,
            GraphSplineKnot b,
            float t,
            out float x,
            out float y,
            out float dx,
            out float dy)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            var h00 = 2f * t3 - 3f * t2 + 1f;
            var h10 = t3 - 2f * t2 + t;
            var h01 = -2f * t3 + 3f * t2;
            var h11 = t3 - t2;
            Tangent(a, out var amx, out var amy);
            Tangent(b, out var bmx, out var bmy);
            x = h00 * a.X + h10 * amx + h01 * b.X + h11 * bmx;
            y = h00 * a.Y + h10 * amy + h01 * b.Y + h11 * bmy;

            var h00d = 6f * t2 - 6f * t;
            var h10d = 3f * t2 - 4f * t + 1f;
            var h01d = -6f * t2 + 6f * t;
            var h11d = 3f * t2 - 2f * t;
            dx = h00d * a.X + h10d * amx + h01d * b.X + h11d * bmx;
            dy = h00d * a.Y + h10d * amy + h01d * b.Y + h11d * bmy;
        }

        static void Tangent(GraphSplineKnot knot, out float mx, out float my)
        {
            var radians = knot.TangentDegrees * (float)(Math.PI / 180.0);
            var length = knot.TangentLength > 0.0001f ? knot.TangentLength : 0.0001f;
            mx = (float)Math.Cos(radians) * length;
            my = (float)Math.Sin(radians) * length;
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }
    }
}
