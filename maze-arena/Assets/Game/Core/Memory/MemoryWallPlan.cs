using System;
using System.Collections.Generic;
using Nixin.Maze.Core;

namespace Game.Core.Memory
{
    public sealed class MemoryWallPlan
    {
        public MemoryWallPlan(
            string levelId,
            IReadOnlyList<MemoryWallPlacement> placements,
            IReadOnlyList<string> genreIdsUsed)
        {
            LevelId = levelId ?? string.Empty;
            Placements = placements ?? Array.Empty<MemoryWallPlacement>();
            GenreIdsUsed = genreIdsUsed ?? Array.Empty<string>();
        }

        public string LevelId { get; }
        public IReadOnlyList<MemoryWallPlacement> Placements { get; }
        public IReadOnlyList<string> GenreIdsUsed { get; }

        public bool TryGetPlacement(MazeEdge edge, WallFace face, out MemoryWallPlacement placement)
        {
            var key = edge.Normalized();
            for (var i = 0; i < Placements.Count; i++)
            {
                var candidate = Placements[i];
                if (candidate.Face == face && candidate.Edge.Equals(key))
                {
                    placement = candidate;
                    return true;
                }
            }

            placement = null;
            return false;
        }
    }
}
