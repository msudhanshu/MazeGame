using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using NUnit.Framework;
using Nixin.Game.Core;
using Nixin.Graph.Core;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    public class GraphPromptsTests
    {
        static GraphPath SamplePath()
        {
            var a = new GraphNodeId("a");
            var b = new GraphNodeId("b");
            var c = new GraphNodeId("c");
            var d = new GraphNodeId("d");
            var topology = new GraphTopology(
                new[] { a, b, c, d },
                new[] { (a, b), (a, c), (b, d), (c, d) });

            return new GraphPath(topology, new[] { a, b, d });
        }

        [Test]
        public void FromPath_offers_neighbors_as_choices()
        {
            var path = SamplePath();
            var prompts = GraphPrompts.FromPath(path);

            Assert.That(prompts.Count, Is.EqualTo(path.StepCount));
            Assert.That(prompts[0].Choices.Count, Is.GreaterThanOrEqualTo(2));
        }
    }

    public class GraphWalkRunTests
    {
        [Test]
        public void Choose_correct_node_advances()
        {
            var a = new GraphNodeId("a");
            var b = new GraphNodeId("b");
            var c = new GraphNodeId("c");
            var d = new GraphNodeId("d");
            var topology = new GraphTopology(new[] { a, b, c, d }, new[] { (a, b), (a, c), (b, d), (c, d) });
            var path = new GraphPath(topology, new[] { a, b, d });
            var run = new GraphWalkRun(path);

            Assert.That(run.IsOption(b), Is.True);
            var outcome = run.Choose(b);
            Assert.That(outcome, Is.EqualTo(WalkOutcome.Advanced));
            Assert.That(run.CurrentNode, Is.EqualTo(b));
        }

        [Test]
        public void Generated_path_is_playable()
        {
            var a = new GraphNodeId("n0");
            var b = new GraphNodeId("n1");
            var c = new GraphNodeId("n2");
            var d = new GraphNodeId("n3");
            var e = new GraphNodeId("n4");
            var f = new GraphNodeId("n5");
            var topology = new GraphTopology(
                new[] { a, b, c, d, e, f },
                new[]
                {
                    (a, b), (a, c),
                    (b, d), (c, d),
                    (d, e), (d, f),
                    (e, f)
                });

            var path = GraphLevelRuntime.CreatePath(
                topology,
                a,
                f,
                new PathShapeSpec(4, 6, 0, 4),
                seed: 3);

            var run = new GraphWalkRun(path);
            for (var step = 0; step < path.StepCount; step++)
            {
                var next = path.Nodes[step + 1];
                Assert.That(run.IsOption(next), Is.True);
                run.Choose(next);
            }

            Assert.That(run.IsLevelCompleted, Is.True);
        }
    }

    public class GraphEdgeRevealTests
    {
        static readonly GraphNodeId A = new GraphNodeId("a");
        static readonly GraphNodeId B = new GraphNodeId("b");
        static readonly GraphNodeId C = new GraphNodeId("c");
        static readonly GraphNodeId D = new GraphNodeId("d");

        [Test]
        public void Edges_from_the_current_node_are_visible()
        {
            var walked = new[] { A };
            var options = new[] { B, C };

            Assert.That(GraphEdgeReveal.IsVisible(A, B, A, walked, options), Is.True);
            Assert.That(GraphEdgeReveal.IsVisible(A, C, A, walked, options), Is.True);
            Assert.That(GraphEdgeReveal.IsVisible(B, D, A, walked, options), Is.False);
        }

        [Test]
        public void Walked_route_stays_visible_after_leaving_a_node()
        {
            var walked = new[] { A, B, D };
            var options = new[] { C };

            Assert.That(GraphEdgeReveal.IsVisible(A, B, D, walked, options), Is.True);
            Assert.That(GraphEdgeReveal.IsVisible(B, D, D, walked, options), Is.False);
            Assert.That(GraphEdgeReveal.IsVisible(A, C, D, walked, options), Is.False);
            Assert.That(GraphEdgeReveal.IsVisible(C, D, D, walked, options), Is.True);
        }

        [Test]
        public void An_unrelated_edge_stays_hidden()
        {
            Assert.That(GraphEdgeReveal.IsVisible(B, D, A, new[] { A }, new[] { C }), Is.False);
        }

        [Test]
        public void Immediate_parent_edge_is_not_shown_as_a_forward_option()
        {
            var walked = new[] { A, B, D };
            var options = new[] { C };

            Assert.That(GraphEdgeReveal.IsVisible(B, D, D, walked, options), Is.False);
        }
    }

    public class GraphNodeRevealTests
    {
        static readonly GraphNodeId A = new GraphNodeId("a");
        static readonly GraphNodeId B = new GraphNodeId("b");
        static readonly GraphNodeId C = new GraphNodeId("c");
        static readonly GraphNodeId D = new GraphNodeId("d");

        [Test]
        public void Start_and_its_next_moves_are_visible()
        {
            var walked = new[] { A };
            var options = new[] { B, C };

            Assert.That(GraphNodeReveal.IsVisible(A, A, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(B, A, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(C, A, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(D, A, walked, options), Is.False);
        }

        [Test]
        public void Covered_nodes_stay_visible_after_moving_on()
        {
            var walked = new[] { A, B };
            var options = new[] { D };

            Assert.That(GraphNodeReveal.IsVisible(A, B, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(B, B, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(D, B, walked, options), Is.True);
            Assert.That(GraphNodeReveal.IsVisible(C, B, walked, options), Is.False);
        }

        [Test]
        public void Past_nodes_are_not_drawn_as_circles()
        {
            var options = new[] { D };

            Assert.That(GraphNodeReveal.IsCircleVisible(A, B, options), Is.False);
            Assert.That(GraphNodeReveal.IsCircleVisible(B, B, options), Is.False);
            Assert.That(GraphNodeReveal.IsCircleVisible(D, B, options), Is.True);
            Assert.That(GraphNodeReveal.IsCircleVisible(C, B, options), Is.False);
        }

        [Test]
        public void Current_node_is_not_drawn_as_a_circle()
        {
            var options = new[] { B, C };

            Assert.That(GraphNodeReveal.IsCircleVisible(A, A, options), Is.False);
            Assert.That(GraphNodeReveal.IsCircleVisible(B, A, options), Is.True);
            Assert.That(GraphNodeReveal.IsCircleVisible(C, A, options), Is.True);
        }
    }
}
