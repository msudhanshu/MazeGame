using Game.Core;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class ArenaControlSchemeTests
    {
        [Test]
        public void DualSticksRailWaypointAndRailJoystickAreImplemented()
        {
            Assert.That(ArenaControlSchemes.IsImplemented(ArenaControlScheme.DualSticks), Is.True);
            Assert.That(ArenaControlSchemes.IsImplemented(ArenaControlScheme.RailWaypoint), Is.True);
            Assert.That(ArenaControlSchemes.IsImplemented(ArenaControlScheme.RailJoystick), Is.True);
            Assert.That(ArenaControlSchemes.IsImplemented(ArenaControlScheme.GazeWalk), Is.False);
            Assert.That(ArenaControlSchemes.IsImplemented(ArenaControlScheme.ArrowLook), Is.False);
        }

        [Test]
        public void RailSchemesIncludeWaypointAndJoystick()
        {
            Assert.That(ArenaControlSchemes.UsesRail(ArenaControlScheme.RailWaypoint), Is.True);
            Assert.That(ArenaControlSchemes.UsesRail(ArenaControlScheme.RailJoystick), Is.True);
            Assert.That(ArenaControlSchemes.UsesRail(ArenaControlScheme.DualSticks), Is.False);
        }

        [Test]
        public void UnimplementedSchemesFallBackToRailWaypoint()
        {
            Assert.That(ArenaControlSchemes.Resolve(ArenaControlScheme.GazeWalk), Is.EqualTo(ArenaControlScheme.RailWaypoint));
            Assert.That(ArenaControlSchemes.Resolve(ArenaControlScheme.ArrowLook), Is.EqualTo(ArenaControlScheme.RailWaypoint));
            Assert.That(ArenaControlSchemes.Resolve(ArenaControlScheme.DualSticks), Is.EqualTo(ArenaControlScheme.DualSticks));
            Assert.That(ArenaControlSchemes.Resolve(ArenaControlScheme.RailWaypoint), Is.EqualTo(ArenaControlScheme.RailWaypoint));
            Assert.That(ArenaControlSchemes.Resolve(ArenaControlScheme.RailJoystick), Is.EqualTo(ArenaControlScheme.RailJoystick));
        }

        [Test]
        public void EverySchemeHasADisplayNameAndDescription()
        {
            for (var i = 0; i < ArenaControlSchemes.All.Length; i++)
            {
                var scheme = ArenaControlSchemes.All[i];
                Assert.That(ArenaControlSchemes.Name(scheme), Is.Not.Null.And.Not.Empty);
                Assert.That(ArenaControlSchemes.Description(scheme), Is.Not.Null.And.Not.Empty);
            }

            Assert.That(ArenaControlSchemes.Name(ArenaControlScheme.DualSticks), Is.EqualTo("Dual sticks"));
            Assert.That(ArenaControlSchemes.Name(ArenaControlScheme.GazeWalk), Is.EqualTo("Gaze walk"));
            Assert.That(ArenaControlSchemes.Name(ArenaControlScheme.ArrowLook), Is.EqualTo("Arrow look"));
            Assert.That(ArenaControlSchemes.Name(ArenaControlScheme.RailWaypoint), Is.EqualTo("Rail waypoint"));
            Assert.That(ArenaControlSchemes.Name(ArenaControlScheme.RailJoystick), Is.EqualTo("Rail joystick"));
        }
    }
}
