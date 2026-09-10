using Game.Unity.Input;
using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class BoardMoveTests
    {
        Camera _camera;
        BoardLayout _layout;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TestCamera", typeof(Camera));
            _camera = go.GetComponent<Camera>();
            _camera.transform.SetPositionAndRotation(new Vector3(0f, 10f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _camera.orthographic = true;
            _camera.orthographicSize = 6f;
            _layout = new BoardLayout(new GridSize(5, 5), tileSize: 0.9f, gap: 0.1f, origin: Vector3.zero);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void TapOnANeighbouringTileSelectsThatTile()
        {
            var neighbour = new GridCoord(3, 2);
            var screen = (Vector2)_camera.WorldToScreenPoint(_layout.WorldPosition(neighbour));

            var resolved = BoardMove.TryResolve(
                StrokeResult.Tap, screen, _camera, _layout, new GridCoord(2, 2), out var target);

            Assert.That(resolved, Is.True);
            Assert.That(target, Is.EqualTo(neighbour));
        }

        [Test]
        public void SwipeSelectsTheNeighbourInThatDirection()
        {
            var current = new GridCoord(2, 2);

            Assert.That(ResolveSwipe(current, new GridCoord(1, 0)), Is.EqualTo(new GridCoord(3, 2)));
            Assert.That(ResolveSwipe(current, new GridCoord(-1, 0)), Is.EqualTo(new GridCoord(1, 2)));
            Assert.That(ResolveSwipe(current, new GridCoord(0, 1)), Is.EqualTo(new GridCoord(2, 3)));
            Assert.That(ResolveSwipe(current, new GridCoord(0, -1)), Is.EqualTo(new GridCoord(2, 1)));
        }

        [Test]
        public void EmptyStrokeDoesNotMove()
        {
            var moved = BoardMove.TryResolve(
                StrokeResult.None, Vector2.zero, _camera, _layout, new GridCoord(2, 2), out _);

            Assert.That(moved, Is.False);
        }

        [Test]
        public void TapWithoutACameraCannotPickATile()
        {
            var moved = BoardMove.TryResolve(
                StrokeResult.Tap, Vector2.zero, null, _layout, new GridCoord(2, 2), out _);

            Assert.That(moved, Is.False);
        }

        GridCoord ResolveSwipe(GridCoord current, GridCoord offset)
        {
            Assert.That(
                BoardMove.TryResolve(StrokeResult.Swipe(offset), Vector2.zero, _camera, _layout, current, out var target),
                Is.True);
            return target;
        }
    }
}
