using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class BoardCameraTests
    {
        Camera _camera;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("TopDownCamera", typeof(Camera));
            _camera = go.GetComponent<Camera>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_camera.gameObject);
        }

        [Test]
        public void FramesTheBoardOrthographicAndLookingStraightDown()
        {
            var layout = new BoardLayout(new GridSize(5, 5), 0.9f, 0.1f, Vector3.zero);

            BoardCamera.FrameTopDown(_camera, layout, aspect: 16f / 9f);

            Assert.That(_camera.orthographic, Is.True);
            Assert.That(BoardCamera.IsTopDown(_camera), Is.True);
            Assert.That(_camera.transform.position.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_camera.transform.position.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_camera.transform.position.y, Is.GreaterThan(0f));
            Assert.That(Vector3.Dot(_camera.transform.up, Vector3.forward), Is.GreaterThan(0.99f));
        }

        [Test]
        public void OrthographicSizeCoversTheWholeBoard()
        {
            var layout = new BoardLayout(new GridSize(8, 8), 0.9f, 0.1f, Vector3.zero);
            var aspect = 9f / 16f;

            var size = BoardCamera.OrthographicSize(layout, aspect);

            Assert.That(size * 2f, Is.GreaterThanOrEqualTo(layout.Depth));
            Assert.That(size * 2f * aspect, Is.GreaterThanOrEqualTo(layout.Width - 0.001f));
        }

        [Test]
        public void PortraitViewFillsTheBoardWidth()
        {
            var layout = new BoardLayout(new GridSize(5, 5), 1f, 0f, Vector3.zero);
            var aspect = 9f / 16f;

            var size = BoardCamera.OrthographicSize(layout, aspect);

            Assert.That(size * 2f * aspect, Is.EqualTo(layout.Width).Within(0.01f));
        }

        [Test]
        public void FarClipReachesTheDistantRoomFloor()
        {
            BoardCamera.FrameTopDown(
                _camera,
                new BoardLayout(new GridSize(3, 3), 1f, 0f, Vector3.zero),
                aspect: 9f / 16f);

            var floorDistance = BoardCamera.Height + ArenaEnvironment.RoomFloorDepth;
            Assert.That(_camera.farClipPlane, Is.GreaterThan(floorDistance));
        }

        [Test]
        public void ATiltedPerspectiveCameraIsNotTopDown()
        {
            _camera.orthographic = false;
            _camera.transform.position = new Vector3(0f, 10f, -8f);
            _camera.transform.LookAt(Vector3.zero);

            Assert.That(BoardCamera.IsTopDown(_camera), Is.False);
        }

        [Test]
        public void FollowModeCentersOnFocusAndZoomsIn()
        {
            var layout = new BoardLayout(new GridSize(5, 5), 1f, 0f, Vector3.zero);
            var focus = layout.WorldPosition(new GridCoord(2, 2));

            BoardCamera.FrameTopDown(_camera, layout, aspect: 1f);
            var wideSize = _camera.orthographicSize;

            BoardCamera.FrameFollow(_camera, focus, orthographicSize: 1.8f);

            Assert.That(_camera.orthographic, Is.True);
            Assert.That(BoardCamera.IsTopDown(_camera), Is.True);
            Assert.That(_camera.transform.position.x, Is.EqualTo(focus.x).Within(0.0001f));
            Assert.That(_camera.transform.position.z, Is.EqualTo(focus.z).Within(0.0001f));
            Assert.That(_camera.orthographicSize, Is.LessThan(wideSize));
            Assert.That(_camera.orthographicSize, Is.EqualTo(1.8f).Within(0.0001f));
        }

        [Test]
        public void HeadingFollowStaysTopDownAndYawsScreenUp()
        {
            var focus = new Vector3(2f, 0f, 3f);
            BoardCamera.FrameFollow(_camera, focus, orthographicSize: 0.65f, headingYaw: 90f);

            Assert.That(BoardCamera.IsTopDown(_camera), Is.True);
            Assert.That(_camera.transform.position.x, Is.EqualTo(focus.x).Within(0.0001f));
            Assert.That(_camera.transform.position.z, Is.EqualTo(focus.z).Within(0.0001f));
            Assert.That(Vector3.Dot(_camera.transform.up, Vector3.right), Is.GreaterThan(0.99f));
            Assert.That(ScoutRotationMove.HeadingYaw(_camera), Is.EqualTo(90f).Within(0.1f));
        }

        [Test]
        public void SmoothFollowLocksYawToHeadingWithoutLag()
        {
            BoardCamera.FrameFollow(_camera, Vector3.zero, orthographicSize: 0.65f, headingYaw: 0f);
            BoardCamera.SmoothFollowWalker(
                _camera,
                Vector3.zero,
                orthographicSize: 0.65f,
                smoothing: 10f,
                headingYaw: 90f,
                yawSmoothing: 0f);

            Assert.That(ScoutRotationMove.HeadingYaw(_camera), Is.EqualTo(90f).Within(0.1f));
            Assert.That(Vector3.Dot(_camera.transform.up, Vector3.right), Is.GreaterThan(0.99f));
        }

        [Test]
        public void FitWidthPinsWorldWidthToTheScreen()
        {
            var aspect = 9f / 16f;
            BoardCamera.FrameTopDownFitWidth(_camera, Vector3.zero, width: 12f, aspect);

            Assert.That(_camera.orthographic, Is.True);
            Assert.That(_camera.orthographicSize * 2f * aspect, Is.EqualTo(12f).Within(0.001f));
            Assert.That(
                BoardCamera.FitWidthOrthographicSize(12f, aspect),
                Is.EqualTo(_camera.orthographicSize).Within(0.001f));
        }

        [Test]
        public void TopHudInsetShiftsTheBoardBelowTheChrome()
        {
            var layout = new BoardLayout(new GridSize(8, 14), 1f, 0f, Vector3.zero);
            var aspect = 9f / 16f;
            var inset = 0.2f;

            var size = BoardCamera.OrthographicSize(layout, aspect, inset);
            Assert.That(size, Is.GreaterThan(BoardCamera.OrthographicSize(layout, aspect)));

            BoardCamera.FrameTopDown(_camera, layout, aspect, inset);

            var topEdge = layout.Origin.z + layout.Depth * 0.5f;
            var topViewport = BoardCamera.ViewportY(topEdge, _camera.transform.position.z, _camera.orthographicSize);
            Assert.That(topViewport, Is.LessThanOrEqualTo(1f - inset + 0.001f));
            Assert.That(
                _camera.transform.position.z,
                Is.EqualTo(BoardCamera.TopHudFocusOffset(inset, size)).Within(0.0001f));
        }

        [Test]
        public void ZeroHudInsetKeepsTheBoardCentered()
        {
            var layout = new BoardLayout(new GridSize(5, 5), 1f, 0f, Vector3.zero);
            BoardCamera.FrameTopDown(_camera, layout, aspect: 9f / 16f, topViewportInset: 0f);
            Assert.That(_camera.transform.position.z, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void NearlyMatchesOverviewWhenAlreadyFramed()
        {
            BoardCamera.FrameTopDownForBounds(_camera, Vector3.zero, width: 12f, depth: 24f, aspect: 16f / 9f);
            var target = new Vector3(0f, BoardCamera.Height, 0f);
            Assert.That(
                BoardCamera.NearlyMatchesOverview(_camera, target, _camera.orthographicSize),
                Is.True);

            _camera.orthographicSize *= 0.5f;
            Assert.That(
                BoardCamera.NearlyMatchesOverview(_camera, target, BoardCamera.ContainOrthographicSize(12f, 24f, 16f / 9f)),
                Is.False);
        }

        [Test]
        public void CoverFillsTheScreenAndCropsAMismatchedPhoto()
        {
            var aspect = 16f / 9f;
            BoardCamera.FrameTopDownCover(_camera, Vector3.zero, width: 12f, depth: 24f, aspect);

            Assert.That(_camera.orthographicSize * 2f * aspect, Is.EqualTo(12f).Within(0.001f));
            Assert.That(_camera.orthographicSize * 2f, Is.LessThan(24f - 0.001f));
            Assert.That(
                _camera.orthographicSize,
                Is.EqualTo(BoardCamera.CoverOrthographicSize(12f, 24f, aspect)).Within(0.001f));
        }

        [Test]
        public void ContainShowsTheWholePhotoOnAMismatchedScreen()
        {
            var aspect = 16f / 9f;
            BoardCamera.FrameTopDownForBounds(_camera, Vector3.zero, width: 12f, depth: 24f, aspect);

            Assert.That(_camera.orthographicSize * 2f, Is.GreaterThanOrEqualTo(24f - 0.001f));
            Assert.That(_camera.orthographicSize * 2f * aspect, Is.GreaterThanOrEqualTo(12f - 0.001f));
            Assert.That(
                _camera.orthographicSize,
                Is.EqualTo(BoardCamera.ContainOrthographicSize(12f, 24f, aspect)).Within(0.001f));
        }

        [Test]
        public void ContainBottomAlignsAShortPhoto()
        {
            var aspect = 9f / 16f;
            BoardCamera.FrameTopDownForBounds(
                _camera,
                Vector3.zero,
                width: 12f,
                depth: 6f,
                aspect,
                bottomAlign: true);

            Assert.That(_camera.orthographicSize * 2f * aspect, Is.GreaterThanOrEqualTo(12f - 0.001f));
            Assert.That(_camera.orthographicSize, Is.GreaterThan(3f));
            Assert.That(
                BoardCamera.ViewportY(-3f, _camera.transform.position.z, _camera.orthographicSize),
                Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                BoardCamera.ViewportY(3f, _camera.transform.position.z, _camera.orthographicSize),
                Is.LessThan(1f - 0.01f));
        }

        [Test]
        public void ContainStaysCenteredWhenBottomAlignIsOff()
        {
            var aspect = 9f / 16f;
            BoardCamera.FrameTopDownForBounds(
                _camera,
                Vector3.zero,
                width: 12f,
                depth: 6f,
                aspect,
                bottomAlign: false);

            Assert.That(_camera.transform.position.z, Is.EqualTo(0f).Within(0.001f));
            Assert.That(
                BoardCamera.ViewportY(-3f, _camera.transform.position.z, _camera.orthographicSize),
                Is.GreaterThan(0.01f));
        }

        [Test]
        public void ContainHudCentersAShortPhotoBetweenChromeAndBottom()
        {
            var aspect = 9f / 16f;
            const float inset = 0.2f;
            BoardCamera.FrameTopDownForBounds(
                _camera,
                Vector3.zero,
                width: 12f,
                depth: 6f,
                aspect,
                bottomAlign: false,
                topViewportInset: inset);

            Assert.That(
                _camera.transform.position.z,
                Is.EqualTo(BoardCamera.TopHudFocusOffset(inset, _camera.orthographicSize)).Within(0.001f));
            Assert.That(
                BoardCamera.ViewportY(0f, _camera.transform.position.z, _camera.orthographicSize),
                Is.EqualTo((1f - inset) * 0.5f).Within(0.001f));
            Assert.That(
                BoardCamera.ViewportY(-3f, _camera.transform.position.z, _camera.orthographicSize),
                Is.GreaterThan(0.01f));
            Assert.That(
                BoardCamera.ViewportY(3f, _camera.transform.position.z, _camera.orthographicSize),
                Is.LessThanOrEqualTo(1f - inset + 0.001f));
        }
    }
}
