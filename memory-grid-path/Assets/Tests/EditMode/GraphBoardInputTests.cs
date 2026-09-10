using Game.Unity.Data;
using Game.Unity.Graph;
using NUnit.Framework;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GraphBoardInputTests
    {
        Camera _camera;
        Texture2D _texture;
        GraphLevelDefinition _level;
        GraphBoardLayout _layout;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestCamera", typeof(Camera));
            _camera = go.GetComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _camera.orthographic = true;
            _camera.orthographicSize = 6f;

            _texture = new Texture2D(16, 9);
            _level = GraphLevelDefinition.CreateSampleRuntime();
            _level.SetRuntimeData(
                _level.DisplayName,
                _texture,
                _level.Nodes,
                _level.Edges,
                _level.StartNodeId,
                _level.GoalNodeId);

            _layout = new GraphBoardLayout(_level, Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            if (_camera != null)
                Object.DestroyImmediate(_camera.gameObject);
            if (_texture != null)
                Object.DestroyImmediate(_texture);
            if (_level != null)
                Object.DestroyImmediate(_level);
        }

        [Test]
        public void TappingNodeScreenPointPicksOptionUnderRay()
        {
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");
            var options = new[] { n1, n2 };

            var screenPoint = _camera.WorldToScreenPoint(_layout.WorldPosition(n1));
            var ray = _camera.ScreenPointToRay(screenPoint);

            var picked = _layout.TryPickOptionUnderRay(
                ray,
                current,
                options,
                GraphBoardInput.NodePickRadius,
                GraphBoardInput.EdgePickRadius,
                out var target);

            Assert.That(picked, Is.True);
            Assert.That(target, Is.EqualTo(n1));
        }

        [Test]
        public void TappingEdgeScreenPointPicksOptionUnderRay()
        {
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");
            var options = new[] { n1, n2 };

            var edgeMidpoint = (_layout.WorldPosition(current) + _layout.WorldPosition(n2)) * 0.5f;
            var screenPoint = _camera.WorldToScreenPoint(edgeMidpoint);
            var ray = _camera.ScreenPointToRay(screenPoint);

            var picked = _layout.TryPickOptionUnderRay(
                ray,
                current,
                options,
                GraphBoardInput.NodePickRadius,
                GraphBoardInput.EdgePickRadius,
                out var target);

            Assert.That(picked, Is.True);
            Assert.That(target, Is.EqualTo(n2));
        }

        [Test]
        public void TappingFarAwayFromGraphReturnsFalse()
        {
            var current = new GraphNodeId("n0");
            var options = new[] { new GraphNodeId("n1") };

            var farPoint = _layout.WorldPosition(current) + new Vector3(50f, 0f, 50f);
            var screenPoint = _camera.WorldToScreenPoint(farPoint);
            var ray = _camera.ScreenPointToRay(screenPoint);

            var picked = _layout.TryPickOptionUnderRay(
                ray,
                current,
                options,
                GraphBoardInput.NodePickRadius,
                GraphBoardInput.EdgePickRadius,
                out _);

            Assert.That(picked, Is.False);
        }

        [Test]
        public void DirectionPickSelectsTheOptionAlongThatAxis()
        {
            var current = new GraphNodeId("n0");
            var n1 = new GraphNodeId("n1");
            var n2 = new GraphNodeId("n2");
            var options = new[] { n1, n2 };

            Assert.That(
                GraphBoardInput.TryPickOptionInDirection(_layout, current, options, Vector3.forward, out var up),
                Is.True);
            Assert.That(up, Is.EqualTo(n1));

            Assert.That(
                GraphBoardInput.TryPickOptionInDirection(_layout, current, options, Vector3.back, out var down),
                Is.True);
            Assert.That(down, Is.EqualTo(n2));
        }

        [Test]
        public void DirectionPickIgnoresOptionsBehindTheOrigin()
        {
            var current = new GraphNodeId("n0");
            var options = new[] { new GraphNodeId("n1"), new GraphNodeId("n2") };

            Assert.That(
                GraphBoardInput.TryPickOptionInDirection(_layout, current, options, Vector3.left, out _),
                Is.False);
        }
    }
}
