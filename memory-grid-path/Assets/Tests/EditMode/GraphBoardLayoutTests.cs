using Game.Unity.Data;
using Game.Unity.Graph;
using NUnit.Framework;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GraphBoardLayoutTests
    {
        Texture2D _texture;
        GraphLevelDefinition _level;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(16, 9);
            _level = GraphLevelDefinition.CreateSampleRuntime();
            _level.SetRuntimeData(
                _level.DisplayName,
                _texture,
                _level.Nodes,
                _level.Edges,
                _level.StartNodeId,
                _level.GoalNodeId);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_texture);
            Object.DestroyImmediate(_level);
        }

        [Test]
        public void BoardMatchesThePhotoAspect()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);

            Assert.That(layout.WorldWidth / layout.WorldDepth, Is.EqualTo(16f / 9f).Within(0.001f));
        }

        [Test]
        public void CenterNodeSitsAtItsAuthoredPhotoPosition()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            GraphNodeEntry n3 = default;
            for (var i = 0; i < _level.Nodes.Count; i++)
            {
                if (_level.Nodes[i].Id == "n3")
                    n3 = _level.Nodes[i];
            }

            var expected = GraphImageFit.NormalizedToWorld(
                n3.NormalizedPosition,
                layout.WorldWidth,
                layout.WorldDepth,
                layout.Origin);

            Assert.That(layout.WorldPosition(new GraphNodeId("n3")), Is.EqualTo(expected));
        }

        [Test]
        public void BottomLeftNormalizedCoordIsThePhotoCorner()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            var world = GraphImageFit.NormalizedToWorld(new Vector2(0f, 0f), layout.WorldWidth, layout.WorldDepth, layout.Origin);

            Assert.That(world.x, Is.EqualTo(-layout.WorldWidth * 0.5f).Within(0.001f));
            Assert.That(world.z, Is.EqualTo(-layout.WorldDepth * 0.5f).Within(0.001f));
        }

        [Test]
        public void ClickingAnOutgoingEdgePicksThatOption()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");
            var options = new[] { n1, n2 };
            var midpoint = (layout.WorldPosition(current) + layout.WorldPosition(n1)) * 0.5f;

            Assert.That(
                layout.TryPickOption(midpoint, current, options, 0.2f, 0.3f, out var picked),
                Is.True);
            Assert.That(picked, Is.EqualTo(n1));
        }

        [Test]
        public void ClickingTheCurrentNodeDoesNotPickAnOption()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");

            Assert.That(
                layout.TryPickOption(layout.WorldPosition(current), current, new[] { n1, n2 }, 1.15f, 0.85f, out _),
                Is.False);
        }

        [Test]
        public void ANodeHitWinsOverTheEdge()
        {
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");

            Assert.That(
                layout.TryPickOption(layout.WorldPosition(n2), current, new[] { n1, n2 }, 0.45f, 0.3f, out var picked),
                Is.True);
            Assert.That(picked, Is.EqualTo(n2));
        }

        [Test]
        public void ClickingABowedEdgePicksThatOption()
        {
            GraphEdgePathTests.BendEdge(_level, "n0", "n1", new Vector2(0.2f, 0.85f));
            var layout = new GraphBoardLayout(_level, Vector3.zero);
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");
            var bow = GraphImageFit.NormalizedToWorld(
                new Vector2(0.2f, 0.85f),
                layout.WorldWidth,
                layout.WorldDepth,
                layout.Origin);

            Assert.That(
                layout.TryPickOption(bow, current, new[] { n1, n2 }, 0.15f, 0.35f, out var picked),
                Is.True);
            Assert.That(picked, Is.EqualTo(n1));
        }

        [Test]
        public void BoardUsesAuthoredImageAspectInsteadOfGpuTextureSize()
        {
            var gpu = new Texture2D(32, 32);
            try
            {
                var snapshot = _level.CaptureSnapshot();
                snapshot.Background = gpu;
                snapshot.ImageAspect = 16f / 9f;
                _level.ApplySnapshot(snapshot);

                var layout = new GraphBoardLayout(_level, Vector3.zero);

                Assert.That(GraphLevelDefinition.AspectOf(gpu), Is.EqualTo(1f).Within(0.001f));
                Assert.That(_level.ImageAspect, Is.EqualTo(16f / 9f).Within(0.001f));
                Assert.That(layout.WorldWidth / layout.WorldDepth, Is.EqualTo(16f / 9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(gpu);
            }
        }
    }
}
