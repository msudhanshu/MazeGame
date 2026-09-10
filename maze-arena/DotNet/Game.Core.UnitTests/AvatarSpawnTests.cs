using System;
using Game.Core;
using NUnit.Framework;
using Nixin.Grid.Core;
using Nixin.Maze.Core;

namespace Game.Core.Tests
{
    [TestFixture]
    public class AvatarSpawnTests
    {
        [Test]
        public void SpawnAtSouthOpeningFacesNorthAtCellCenter()
        {
            var pose = AvatarSpawn.AtEntry(new MazeOpening(new GridCoord(1, 0), WallSide.South), 2f);
            Assert.That(pose.X, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(pose.Y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(pose.Z, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(pose.YawDegrees, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void SpawnAtEastOpeningFacesWest()
        {
            var pose = AvatarSpawn.AtEntry(new MazeOpening(new GridCoord(3, 2), WallSide.East), 2f);
            Assert.That(pose.YawDegrees, Is.EqualTo(-90f).Within(0.0001f));
        }

        [Test]
        public void RejectsNonPositiveCellSizeAndNonCardinalYaw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AvatarSpawn.AtEntry(new MazeOpening(new GridCoord(0, 0), WallSide.South), 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AvatarSpawn.YawFacingInward(WallSide.None));
        }
    }
}
