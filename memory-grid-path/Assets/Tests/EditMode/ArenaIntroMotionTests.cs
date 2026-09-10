using Game.Unity.View;
using NUnit.Framework;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class ArenaIntroMotionTests
    {
        [Test]
        public void AssembleStartsAtZeroAndSettlesAtOne()
        {
            Assert.That(ArenaIntroMotion.AssembleScale(0f), Is.EqualTo(0f));
            Assert.That(ArenaIntroMotion.AssembleScale(1f), Is.EqualTo(1f).Within(0.02f));
            Assert.That(ArenaIntroMotion.AssembleScale(0.5f), Is.GreaterThan(0.4f));
            Assert.That(ArenaIntroMotion.AssembleScale(0.5f), Is.LessThan(1.2f));
        }

        [Test]
        public void GraphBrightnessPunchesThenSettles()
        {
            Assert.That(ArenaIntroMotion.GraphBrightness(0f), Is.EqualTo(0f));
            Assert.That(ArenaIntroMotion.GraphBrightness(0.25f), Is.LessThan(ArenaIntroMotion.GraphBrightness(0.52f)));
            Assert.That(ArenaIntroMotion.GraphBrightness(0.52f), Is.EqualTo(ArenaIntroMotion.GraphPeakBrightness).Within(0.02f));
            Assert.That(ArenaIntroMotion.GraphBrightness(1f), Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void GraphScaleAcceleratesIntoABounceThenSettles()
        {
            Assert.That(ArenaIntroMotion.GraphScale(0f), Is.EqualTo(ArenaIntroMotion.GraphStartScale).Within(0.01f));
            var early = ArenaIntroMotion.GraphScale(0.26f) - ArenaIntroMotion.GraphStartScale;
            var late = ArenaIntroMotion.GraphPeakScale - ArenaIntroMotion.GraphScale(0.26f);
            Assert.That(late, Is.GreaterThan(early));
            Assert.That(ArenaIntroMotion.GraphScale(0.52f), Is.EqualTo(ArenaIntroMotion.GraphPeakScale).Within(0.02f));
            Assert.That(ArenaIntroMotion.GraphScale(0.74f), Is.EqualTo(ArenaIntroMotion.GraphUndershootScale).Within(0.03f));
            Assert.That(ArenaIntroMotion.GraphScale(1f), Is.EqualTo(1f).Within(0.02f));
        }

        [Test]
        public void WaveIndexSpreadsFromOrigin()
        {
            var origin = new GridCoord(0, 0);
            Assert.That(ArenaIntroMotion.WaveIndex(origin, origin), Is.EqualTo(0));
            Assert.That(ArenaIntroMotion.WaveIndex(new GridCoord(2, 1), origin), Is.EqualTo(3));
        }

        [Test]
        public void StaggerCapsLongBoards()
        {
            var many = ArenaIntroMotion.GridTotalSeconds(80);
            Assert.That(many, Is.LessThanOrEqualTo(ArenaIntroMotion.GridMaxTotalSeconds + 0.02f));
            Assert.That(ArenaIntroMotion.StaggerForCount(4), Is.EqualTo(ArenaIntroMotion.GridStaggerSeconds).Within(0.001f));
        }

        [Test]
        public void ScaleAroundKeepsTheOriginFixed()
        {
            var origin = new Vector3(2f, 0f, 3f);
            var point = new Vector3(4f, 0f, 3f);
            Assert.That(ArenaIntroMotion.ScaleAround(origin, origin, 1.5f), Is.EqualTo(origin));
            Assert.That(
                ArenaIntroMotion.ScaleAround(point, origin, 0.5f),
                Is.EqualTo(new Vector3(3f, 0f, 3f)));
        }
    }
}
