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
        public void TravelSecondsScaleWithSplineLength()
        {
            var shortPath = new[] { Vector3.zero, new Vector3(1f, 0f, 0f) };
            var longPath = new[] { Vector3.zero, new Vector3(3f, 0f, 0f) };
            var bent = new[]
            {
                Vector3.zero,
                new Vector3(0f, 0f, 1f),
                new Vector3(2f, 0f, 1f)
            };
            var chord = new[] { Vector3.zero, new Vector3(2f, 0f, 1f) };

            Assert.That(WalkerView.PolylineLength(longPath), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(
                WalkerView.SecondsForPath(longPath, 0.38f),
                Is.EqualTo(WalkerView.SecondsForPath(shortPath, 0.38f) * 3f).Within(0.001f));
            Assert.That(WalkerView.PolylineLength(bent), Is.GreaterThan(WalkerView.PolylineLength(chord)));
            Assert.That(
                WalkerView.SecondsForPath(bent, ScoutRotationMove.TourHopSeconds),
                Is.GreaterThan(WalkerView.SecondsForPath(chord, ScoutRotationMove.TourHopSeconds)));
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
                Assert.That(walker.MotionPaused, Is.False);

                walker.SetBodyAlpha(0f);
                walker.SnapTo(new Vector3(1f, 0f, 0f), restoreAlpha: false);
                var body = walker.GetComponentInChildren<Renderer>();
                Assert.That(body, Is.Not.Null);
                Assert.That(body.sharedMaterial, Is.Not.Null);
                Assert.That(body.sharedMaterial.color.a, Is.EqualTo(0f).Within(0.01f));
                Assert.That(body.sharedMaterial.shader.name, Does.Contain("Unlit"));
                Assert.That(WalkerView.CrashOutSeconds, Is.GreaterThan(0.8f));
                Assert.That(WalkerView.CrashInSeconds, Is.GreaterThan(0.7f));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }

        [Test]
        public void SpawnEffectUsesThreeFadingPulses()
        {
            Assert.That(WalkerView.SpawnPulses, Is.EqualTo(3));
            Assert.That(WalkerView.DespawnAlphaAt(0f), Is.EqualTo(1f).Within(0.01f));
            Assert.That(WalkerView.DespawnAlphaAt(0.25f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(WalkerView.DespawnAlphaAt(0.4f), Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(WalkerView.DespawnAlphaAt(0.72f), Is.EqualTo(0.22f).Within(0.01f));
            Assert.That(WalkerView.DespawnAlphaAt(1f), Is.EqualTo(0f).Within(0.01f));
            Assert.That(WalkerView.SpawnAlphaAt(0f), Is.EqualTo(0.22f).Within(0.01f));
            Assert.That(WalkerView.SpawnAlphaAt(1f), Is.EqualTo(1f).Within(0.01f));
            Assert.That(WalkerView.DespawnAlphaAt(0f), Is.GreaterThan(WalkerView.DespawnAlphaAt(0.4f)));
            Assert.That(WalkerView.DespawnAlphaAt(0.4f), Is.GreaterThan(WalkerView.DespawnAlphaAt(0.72f)));
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

        [Test]
        public void FaceTravelLooksAlongAForwardHop()
        {
            var walker = WalkerView.Create(null, Vector3.zero, Color.white);
            try
            {
                walker.FaceTravel = true;
                walker.HopTo(new Vector3(0f, 0f, 2f));
                for (var i = 0; i < 40; i++)
                    walker.TickFacing(0.05f);

                Assert.That(Vector3.Dot(walker.transform.forward, Vector3.forward), Is.GreaterThan(0.98f));
                walker.FaceToward(Vector3.right, instant: true);
                Assert.That(Vector3.Dot(walker.transform.forward, Vector3.right), Is.GreaterThan(0.98f));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }

        [Test]
        public void FaceTravelTurnsInPlaceBeforeMoving()
        {
            var walker = WalkerView.Create(null, Vector3.zero, Color.white);
            try
            {
                walker.FaceTravel = true;
                walker.FaceToward(Vector3.forward, instant: true);
                walker.HopTo(new Vector3(2f, 0f, 0f));

                Assert.That(walker.IsTurning, Is.True);
                Assert.That(walker.IsHopping, Is.True);
                for (var i = 0; i < 8; i++)
                    walker.TickFacing(0.05f);

                Assert.That(walker.transform.position, Is.EqualTo(Vector3.zero));
                Assert.That(walker.IsTurning, Is.True);
                Assert.That(walker.YawDegrees, Is.GreaterThan(10f));

                for (var i = 0; i < 80; i++)
                    walker.TickFacing(0.05f);

                Assert.That(walker.IsTurning, Is.False);
                Assert.That(walker.YawDegrees, Is.EqualTo(90f).Within(ScoutRotationMove.FacingAlignDegrees));
                Assert.That(walker.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }

        [Test]
        public void FaceTravelYawReachesTargetWithoutSnapping()
        {
            var walker = WalkerView.Create(null, Vector3.zero, Color.white);
            try
            {
                walker.FaceTravel = true;
                walker.YawDegreesPerSecond = 90f;
                walker.FaceToward(Vector3.forward, instant: true);
                walker.HopTo(new Vector3(2f, 0f, 0f));

                var previous = walker.YawDegrees;
                var dt = 0.05f;
                var maxStep = walker.YawDegreesPerSecond * dt + 0.001f;
                for (var i = 0; i < 40 && walker.IsTurning; i++)
                {
                    walker.TickFacing(dt);
                    Assert.That(
                        Mathf.Abs(Mathf.DeltaAngle(previous, walker.YawDegrees)),
                        Is.LessThanOrEqualTo(maxStep));
                    previous = walker.YawDegrees;
                }

                Assert.That(walker.IsTurning, Is.False);
                Assert.That(walker.YawDegrees, Is.EqualTo(90f).Within(ScoutRotationMove.FacingAlignDegrees));
            }
            finally
            {
                Object.DestroyImmediate(walker.gameObject);
            }
        }
    }
}
