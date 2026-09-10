using Game.Core.Domain;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class GraphEdgeSplineTests
    {
        [Test]
        public void Two_knots_with_chord_tangents_stay_on_the_straight_line()
        {
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 1f, 0f, null);

            GraphEdgeSpline.Evaluate(knots, 0f, out var x0, out var y0);
            GraphEdgeSpline.Evaluate(knots, 0.5f, out var xMid, out var yMid);
            GraphEdgeSpline.Evaluate(knots, 1f, out var x1, out var y1);

            Assert.That(x0, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(y0, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(xMid, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(yMid, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(x1, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(y1, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void Opposing_vertical_tangents_bow_the_midpoint_off_the_chord()
        {
            var a = new GraphSplineKnot(0f, 0f, 90f, 1f);
            var b = new GraphSplineKnot(1f, 0f, -90f, 1f);
            var knots = new[] { a, b };

            GraphEdgeSpline.Evaluate(knots, 0.5f, out var x, out var y);

            Assert.That(x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(y, Is.GreaterThan(0.2f));
        }

        [Test]
        public void A_control_knot_is_the_mid_parameter_of_the_whole_spline()
        {
            var control = new GraphSplineKnot(0.5f, 0.4f, 0f, 0.3f);
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 1f, 0f, new[] { control });

            GraphEdgeSpline.Evaluate(knots, 0.5f, out var x, out var y);

            Assert.That(x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(y, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(knots.Length, Is.EqualTo(3));
        }

        [Test]
        public void Closest_point_on_a_bowed_edge_is_nearer_the_control_than_the_chord()
        {
            var control = new GraphSplineKnot(0.5f, 0.5f, 0f, 0.25f);
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 1f, 0f, new[] { control });

            Assert.That(
                GraphEdgeSpline.TryClosestOnSpline(
                    knots,
                    0.5f,
                    0.5f,
                    16,
                    out var segment,
                    out _,
                    out var cx,
                    out var cy,
                    out var sqr),
                Is.True);

            Assert.That(segment, Is.EqualTo(0).Or.EqualTo(1));
            Assert.That(cx, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(cy, Is.EqualTo(0.5f).Within(0.05f));
            Assert.That(sqr, Is.LessThan(0.01f));
        }

        [Test]
        public void Inserting_on_a_straight_edge_targets_the_only_segment()
        {
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 1f, 0f, null);

            Assert.That(
                GraphEdgeSpline.TryClosestOnSpline(knots, 0.4f, 0.02f, 12, out var segment, out var t, out _, out _, out _),
                Is.True);
            Assert.That(segment, Is.EqualTo(0));
            Assert.That(t, Is.GreaterThan(0.2f));
            Assert.That(t, Is.LessThan(0.6f));
        }

        [Test]
        public void Knot_on_a_straight_segment_keeps_the_line_direction()
        {
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 2f, 0f, null);
            var inserted = GraphEdgeSpline.KnotOnSegment(knots[0], knots[1], 0.5f);

            Assert.That(inserted.X, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(inserted.Y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(inserted.TangentDegrees, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void Sample_writes_endpoints_and_the_requested_count()
        {
            var knots = GraphEdgeSpline.BuildKnots(0f, 0f, 1f, 1f, null);
            var count = GraphEdgeSpline.SampleCount(knots.Length, 4);
            var xs = new float[count];
            var ys = new float[count];

            Assert.That(GraphEdgeSpline.Sample(knots, 4, xs, ys), Is.EqualTo(5));
            Assert.That(xs[0], Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ys[0], Is.EqualTo(0f).Within(0.0001f));
            Assert.That(xs[4], Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ys[4], Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
