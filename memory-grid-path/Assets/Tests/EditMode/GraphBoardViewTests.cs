using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.Data;
using Game.Unity.Graph;
using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GraphBoardViewTests
    {
        GameObject _host;
        GraphBoardView _board;
        FakeGraphNodeViewFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("GraphBoardHost");
            _board = _host.AddComponent<GraphBoardView>();
            _factory = new FakeGraphNodeViewFactory();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void BackgroundQuadMatchesThePhotoAspect()
        {
            var texture = new Texture2D(16, 9);
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                level.SetRuntimeData(
                    level.DisplayName,
                    texture,
                    level.Nodes,
                    level.Edges,
                    level.StartNodeId,
                    level.GoalNodeId);

                _board.Build(level, _factory);

                var photo = _host.transform.Find(GraphBackgroundView.ObjectName);
                Assert.That(photo, Is.Not.Null);
                Assert.That(photo.localScale.x / photo.localScale.y, Is.EqualTo(16f / 9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void HidingAllEdgesTurnsOffEveryGraphLine()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                GraphBoardPresenter.Refresh(_board, run, false);
                _board.SetAllEdgesVisible(false);

                foreach (var edge in _board.Edges)
                    Assert.That(edge.Visible, Is.False, edge.NodeA.Value + "-" + edge.NodeB.Value);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void AtTheStartOnlyEdgesFromTheStartNodeAreVisible()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                GraphBoardPresenter.Refresh(_board, run, false);

                AssertVisibleEdges(
                    _board,
                    run.CurrentNode,
                    run.WalkedNodes,
                    PathOptionFilter.VisibleGraphOptions(run.WalkedNodes, run.Options()),
                    showAll: false);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void AfterAStepOnlyTheForwardJunctionFansOut()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                var next = run.Path.Nodes[1];
                run.Choose(next);
                GraphBoardPresenter.Refresh(_board, run, false);

                AssertVisibleEdges(
                    _board,
                    run.CurrentNode,
                    run.WalkedNodes,
                    PathOptionFilter.VisibleGraphOptions(run.WalkedNodes, run.Options()),
                    showAll: false);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void CurrentParentEdgeIsNotShownAsANextMove()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                var next = run.Path.Nodes[1];
                run.Choose(next);
                GraphBoardPresenter.Refresh(_board, run, false);

                Assert.That(EdgeBetween(_board, run.Path.Start, next).Visible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void NodeUvOriginSitsOnThePhotoMeshCorner()
        {
            var texture = new Texture2D(16, 9);
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                level.SetRuntimeData(
                    level.DisplayName,
                    texture,
                    level.Nodes,
                    level.Edges,
                    level.StartNodeId,
                    level.GoalNodeId);

                _board.Build(level, _factory);

                var photo = _host.transform.Find(GraphBackgroundView.ObjectName);
                var expected = GraphImageFit.NormalizedToWorld(
                    Vector2.zero,
                    _board.Layout.WorldWidth,
                    _board.Layout.WorldDepth,
                    _board.Layout.Origin);
                var meshCorner = photo.TransformPoint(new Vector3(-0.5f, -0.5f, 0f));

                Assert.That(meshCorner.x, Is.EqualTo(expected.x).Within(0.01f));
                Assert.That(meshCorner.z, Is.EqualTo(expected.z).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void GraphCameraShowsTheFullPhotoOnAWideGameView()
        {
            var texture = new Texture2D(9, 16);
            var level = GraphLevelDefinition.CreateSampleRuntime();
            var cameraGo = new GameObject("GraphCam", typeof(Camera));
            var camera = cameraGo.GetComponent<Camera>();
            camera.aspect = 16f / 9f;
            try
            {
                level.SetRuntimeData(
                    level.DisplayName,
                    texture,
                    level.Nodes,
                    level.Edges,
                    level.StartNodeId,
                    level.GoalNodeId);

                _board.Build(level, _factory);
                new GraphNodeCircleViewFactory().ApplyEnvironment(camera, _board.Layout, _host.transform);

                var visibleWidth = camera.orthographicSize * 2f * camera.aspect;
                var visibleDepth = camera.orthographicSize * 2f;
                Assert.That(_board.Layout.WorldWidth / _board.Layout.WorldDepth, Is.EqualTo(9f / 16f).Within(0.001f));
                Assert.That(visibleWidth, Is.GreaterThanOrEqualTo(_board.Layout.WorldWidth - 0.001f));
                Assert.That(visibleDepth, Is.GreaterThanOrEqualTo(_board.Layout.WorldDepth - 0.001f));
                Assert.That(
                    camera.orthographicSize,
                    Is.EqualTo(BoardCamera.ContainOrthographicSize(
                        _board.Layout.WorldWidth,
                        _board.Layout.WorldDepth,
                        camera.aspect)).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(cameraGo);
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void OnlyWalkedAndNextMoveNodesAreVisible()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                GraphBoardPresenter.Refresh(_board, run, false);

                AssertVisibleNodes(_board, run, showAll: false);

                var next = run.Path.Nodes[1];
                run.Choose(next);
                GraphBoardPresenter.Refresh(_board, run, false);

                AssertVisibleNodes(_board, run, showAll: false);
                Assert.That(((FakeGraphNodeView)_board.NodeAt(run.Path.Goal)).Visible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void WalkingDrawsATranslucentTrailAndDotsOnCoveredNodes()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                var next = run.Path.Nodes[1];
                run.Choose(next);
                GraphBoardPresenter.Refresh(_board, run, false);

                var overlay = _board.Overlay;
                Assert.That(overlay, Is.Not.Null);
                Assert.That(overlay.Trail.enabled, Is.True);
                Assert.That(overlay.Trail.positionCount, Is.EqualTo(2));
                Assert.That(overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(2));
                Assert.That(((FakeGraphNodeView)_board.NodeAt(run.Path.Start)).Visible, Is.False);
                Assert.That(((FakeGraphNodeView)_board.NodeAt(next)).Visible, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ACurvedWalkedEdgeOverlaysTheAuthoredSpline()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                var probe = GraphBoardPresenter.CreateRun(level, seed: 3);
                var from = probe.Path.Nodes[0].Value;
                var to = probe.Path.Nodes[1].Value;
                GraphEdgePathTests.BendEdge(level, from, to, new Vector2(0.2f, 0.85f));
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                var next = run.Path.Nodes[1];
                run.Choose(next);
                GraphBoardPresenter.Refresh(_board, run, false);

                var overlay = _board.Overlay;
                Assert.That(overlay.Trail.positionCount, Is.GreaterThan(2));
                Assert.That(overlay.transform.Find(GridPathOverlay.DotsName).childCount, Is.EqualTo(2));

                var edge = EdgeBetween(_board, run.Path.Start, next);
                Assert.That(edge.GetComponent<LineRenderer>().positionCount, Is.GreaterThan(2));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void CompletingTheGraphCelebratesAlongTheFullPath()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            try
            {
                _board.Build(level, _factory);
                var run = GraphBoardPresenter.CreateRun(level, seed: 3);
                for (var i = 1; i < run.Path.Nodes.Count; i++)
                    run.Choose(run.Path.Nodes[i]);

                GraphBoardPresenter.Refresh(_board, run, false);

                Assert.That(run.IsLevelCompleted, Is.True);
                Assert.That(_board.Overlay.IsCelebrating, Is.True);
                Assert.That(_board.Overlay.Trail.positionCount, Is.EqualTo(run.Path.Nodes.Count));
                Assert.That(
                    _board.Overlay.transform.Find(GridPathOverlay.DotsName).childCount,
                    Is.EqualTo(run.Path.Nodes.Count));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ScoutGraphEnvironmentPlacesOceanAroundThePhoto()
        {
            var level = GraphLevelDefinition.CreateSampleRuntime();
            var host = new GameObject("GraphOceanHost");
            try
            {
                var layout = new GraphBoardLayout(level, Vector3.zero);
                var factory = new GraphNodeCircleViewFactory { OceanBackdrop = true };
                factory.ApplyEnvironment(null, layout, host.transform);

                var ocean = host.transform.Find(PatchworkOceanBackdrop.OceanName);
                Assert.That(ocean, Is.Not.Null);
                Assert.That(ocean.position.y, Is.LessThan(0f));
                var worldWidth = ocean.localScale.x * 10f;
                Assert.That(worldWidth, Is.GreaterThan(layout.WorldWidth));
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(level);
            }
        }

        static void AssertVisibleNodes(GraphBoardView board, GraphWalkRun run, bool showAll)
        {
            var options = PathOptionFilter.VisibleGraphOptions(run.WalkedNodes, run.Options());
            foreach (var pair in board.Nodes)
            {
                var expected = showAll || GraphNodeReveal.IsCircleVisible(pair.Key, run.CurrentNode, options);
                var view = (FakeGraphNodeView)pair.Value;
                Assert.That(view.Visible, Is.EqualTo(expected), pair.Key.Value);
            }
        }

        static void AssertVisibleEdges(
            GraphBoardView board,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options,
            bool showAll)
        {
            foreach (var edge in board.Edges)
            {
                var expected = showAll || GraphEdgeReveal.IsVisible(edge.NodeA, edge.NodeB, current, walked, options);
                Assert.That(edge.Visible, Is.EqualTo(expected), edge.NodeA.Value + "-" + edge.NodeB.Value);
            }
        }

        static GraphEdgeLine EdgeBetween(GraphBoardView board, GraphNodeId a, GraphNodeId b)
        {
            foreach (var edge in board.Edges)
            {
                if ((edge.NodeA == a && edge.NodeB == b) || (edge.NodeA == b && edge.NodeB == a))
                    return edge;
            }

            throw new System.InvalidOperationException("Missing edge " + a.Value + "-" + b.Value);
        }

        sealed class FakeGraphNodeView : IGraphNodeView
        {
            public FakeGraphNodeView(GraphNodeId nodeId) => NodeId = nodeId;

            public GraphNodeId NodeId { get; }
            public GraphNodeVisualState State { get; private set; } = GraphNodeVisualState.Idle;
            public bool Destroyed { get; private set; }
            public bool Visible { get; private set; } = true;

            public void SetState(GraphNodeVisualState state) => State = state;

            public void SetVisible(bool visible) => Visible = visible;

            public void Destroy() => Destroyed = true;
        }

        sealed class FakeGraphNodeViewFactory : IGraphNodeViewFactory
        {
            public string ThemeId => "fake";

            public IGraphNodeView CreateNode(GraphNodeId nodeId, Vector3 worldPosition, Transform parent) =>
                new FakeGraphNodeView(nodeId);

            public void ApplyEnvironment(Camera camera, GraphBoardLayout layout, Transform parent)
            {
            }
        }
    }
}
