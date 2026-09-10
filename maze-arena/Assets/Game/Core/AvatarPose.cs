using System;
using Nixin.Grid.Core;
using Nixin.Locomotion.Core;
using Nixin.Maze.Core;

namespace Game.Core
{
    /// <summary>
    /// Places the walker in the entry cell, facing into the maze (Unity yaw: 0 looks +Z).
    /// </summary>
    public static class AvatarSpawn
    {
        public static FirstPersonPose AtEntry(MazeOpening entry, float cellSize, float originX, float originY, float originZ)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");

            var x = originX + entry.Cell.X * cellSize + cellSize * 0.5f;
            var z = originZ + entry.Cell.Y * cellSize + cellSize * 0.5f;
            return new FirstPersonPose(x, originY, z, YawFacingInward(entry.Outward));
        }

        public static FirstPersonPose AtEntry(MazeOpening entry, float cellSize)
        {
            return AtEntry(entry, cellSize, 0f, 0f, 0f);
        }

        public static float YawFacingInward(WallSide outward)
        {
            switch (outward)
            {
                case WallSide.North: return 180f;
                case WallSide.South: return 0f;
                case WallSide.East: return -90f;
                case WallSide.West: return 90f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outward), outward, "Yaw is defined only for a single cardinal side.");
            }
        }
    }
}
