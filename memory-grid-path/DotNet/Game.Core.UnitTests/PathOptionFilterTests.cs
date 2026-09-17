using Game.Core.Domain;
using NUnit.Framework;
using Nixin.Graph.Core;
using Nixin.Grid.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class PathOptionFilterTests
    {
        [Test]
        public void VisibleGridOptionsHideEveryWalkedNeighbour()
        {
            var walked = new[]
            {
                new GridCoord(0, 0),
                new GridCoord(1, 0),
                new GridCoord(1, 1)
            };
            var options = new[]
            {
                new GridCoord(1, 0),
                new GridCoord(0, 0),
                new GridCoord(2, 1),
                new GridCoord(1, 2)
            };

            var visible = PathOptionFilter.VisibleGridOptions(walked, options);

            Assert.That(visible, Is.EqualTo(new[] { new GridCoord(2, 1), new GridCoord(1, 2) }));
        }

        [Test]
        public void VisibleGraphOptionsHideEveryWalkedNeighbour()
        {
            var start = new GraphNodeId("start");
            var mid = new GraphNodeId("mid");
            var here = new GraphNodeId("here");
            var ahead = new GraphNodeId("ahead");
            var walked = new[] { start, mid, here };
            var options = new[] { mid, start, ahead };

            var visible = PathOptionFilter.VisibleGraphOptions(walked, options);

            Assert.That(visible, Is.EqualTo(new[] { ahead }));
        }
    }
}
