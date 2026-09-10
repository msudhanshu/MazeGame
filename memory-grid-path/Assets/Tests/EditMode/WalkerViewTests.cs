using Game.Unity.Audio;
using Game.Unity.View;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class WalkerViewTests
    {
        [Test]
        public void TravelStartsOnTheOriginAndEndsOnTheDestination()
        {
            var from = Vector3.zero;
            var to = new Vector3(2f, 0f, 0f);

            Assert.That(WalkerView.PointOnTravel(from, to, 0f), Is.EqualTo(from));
            Assert.That(WalkerView.PointOnTravel(from, to, 1f), Is.EqualTo(to));

            var mid = WalkerView.PointOnTravel(from, to, 0.5f);
            Assert.That(mid.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(mid.z, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(mid.y, Is.GreaterThan(0f));
        }

        [Test]
        public void TravelAlongAPolylineFollowsTheBend()
        {
            var path = new[]
            {
                Vector3.zero,
                new Vector3(0.5f, 0f, 1f),
                new Vector3(1f, 0f, 0f)
            };

            Assert.That(WalkerView.PointOnTravel(path, 0f), Is.EqualTo(path[0]));
            Assert.That(WalkerView.PointOnTravel(path, 1f).x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(WalkerView.PointOnTravel(path, 1f).z, Is.EqualTo(0f).Within(0.0001f));

            var mid = WalkerView.PointOnTravel(path, 0.5f);
            Assert.That(mid.z, Is.GreaterThan(0.3f));
            Assert.That(mid.y, Is.GreaterThan(0f));
        }

        [Test]
        public void HopToMarksTheWalkerBusyUntilSnapped()
        {
            var walker = WalkerView.Create(null, Vector3.zero, Color.white);
            try
            {
                Assert.That(walker.IsHopping, Is.False);
                walker.HopTo(new Vector3(1f, 0f, 0f));
                Assert.That(walker.IsHopping, Is.True);
                Assert.That(WalkerView.CorrectTravelSeconds, Is.LessThan(WalkerView.MistakeTravelSeconds));
                Assert.That(WalkerView.CorrectTravelSeconds, Is.LessThan(0.3f));

                walker.SnapTo(Vector3.zero);
                Assert.That(walker.IsHopping, Is.False);

                var body = walker.GetComponentInChildren<Renderer>();
                Assert.That(body, Is.Not.Null);
                Assert.That(body.sharedMaterial, Is.Not.Null);
                Assert.That(body.sharedMaterial.shader.name, Does.Contain("UnlitColor"));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }

        [Test]
        public void CreateCanShrinkTheWalker()
        {
            var walker = WalkerView.Create(null, Vector3.zero, Color.white, scale: 0.5f);
            try
            {
                Assert.That(walker.transform.localScale.x, Is.EqualTo(0.5f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }
    }
}
