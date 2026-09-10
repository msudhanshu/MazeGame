using Game.Unity.Graph;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class GraphImageFitTests
    {
        [Test]
        public void FittedRectLetterboxesANarrowImageInAWideCanvas()
        {
            var canvas = new Rect(0f, 10f, 800f, 360f);
            var fitted = GraphImageFit.FittedRect(canvas, 1f);

            Assert.That(fitted.height, Is.EqualTo(360f).Within(0.001f));
            Assert.That(fitted.width, Is.EqualTo(360f).Within(0.001f));
            Assert.That(fitted.x, Is.EqualTo(220f).Within(0.001f));
            Assert.That(fitted.y, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void FittedRectLetterboxesAWideImageInATallCanvas()
        {
            var canvas = new Rect(0f, 0f, 360f, 640f);
            var fitted = GraphImageFit.FittedRect(canvas, 16f / 9f);

            Assert.That(fitted.width, Is.EqualTo(360f).Within(0.001f));
            Assert.That(fitted.height, Is.EqualTo(360f * 9f / 16f).Within(0.001f));
            Assert.That(fitted.x, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GuiCornersOnThePhotoMapToImageNormalizedCoords()
        {
            var canvas = new Rect(0f, 0f, 800f, 360f);
            var fitted = GraphImageFit.FittedRect(canvas, 1f);
            var bottomLeft = GraphImageFit.GuiToNormalized(fitted, new Vector2(fitted.xMin, fitted.yMax));
            var topRight = GraphImageFit.GuiToNormalized(fitted, new Vector2(fitted.xMax, fitted.yMin));

            Assert.That(bottomLeft.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(bottomLeft.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(topRight.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(topRight.y, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void NormalizedRoundTripsThroughGui()
        {
            var fitted = new Rect(80f, 20f, 640f, 360f);
            var uv = new Vector2(0.25f, 0.8f);

            var gui = GraphImageFit.NormalizedToGui(fitted, uv);
            var back = GraphImageFit.GuiToNormalized(fitted, gui);

            Assert.That(back.x, Is.EqualTo(uv.x).Within(0.001f));
            Assert.That(back.y, Is.EqualTo(uv.y).Within(0.001f));
        }

        [Test]
        public void WorldSizeKeepsThePhotoAspect()
        {
            var texture = new Texture2D(16, 9);
            try
            {
                GraphImageFit.WorldSize(12f, texture, out var width, out var depth);

                Assert.That(width, Is.EqualTo(12f).Within(0.001f));
                Assert.That(width / depth, Is.EqualTo(16f / 9f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EditorPhotoCornerLandsOnTheSameWorldPointAsPlay()
        {
            var canvas = new Rect(0f, 0f, 800f, 360f);
            var fitted = GraphImageFit.FittedRect(canvas, 16f / 9f);
            GraphImageFit.WorldSize(12f, null, out var squareWidth, out var squareDepth);
            var width = 12f;
            var depth = 12f / (16f / 9f);

            var uv = GraphImageFit.GuiToNormalized(fitted, new Vector2(fitted.xMin, fitted.yMax));
            var world = GraphImageFit.NormalizedToWorld(uv, width, depth, Vector3.zero);

            Assert.That(uv.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(world.x, Is.EqualTo(-width * 0.5f).Within(0.001f));
            Assert.That(world.z, Is.EqualTo(-depth * 0.5f).Within(0.001f));
            Assert.That(squareWidth / squareDepth, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void WorldSizeUsesTheAuthoredAspectWhenTheGpuTextureDiffers()
        {
            GraphImageFit.WorldSize(12f, 16f / 9f, out var width, out var depth);

            Assert.That(width, Is.EqualTo(12f).Within(0.001f));
            Assert.That(width / depth, Is.EqualTo(16f / 9f).Within(0.001f));
        }
    }
}
