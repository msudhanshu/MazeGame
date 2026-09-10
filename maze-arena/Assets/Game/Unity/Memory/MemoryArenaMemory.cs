using System.Collections.Generic;
using Game.Core.Memory;
using Nixin.Game.Core;
using Nixin.Maze;
using UnityEngine;

namespace Game.Unity.Memory
{
    /// <summary>
    /// Assigns memory-game wall displays after <see cref="MazeArena.Rebuild"/>.
    /// </summary>
    public sealed class MemoryArenaMemory : MonoBehaviour
    {
        public MazeArena Arena;
        public MemoryWallCatalog Catalog;
        public MemoryLevelProfile Level;
        public MemoryWallDisplayKit DisplayKit;

        readonly List<string> _previousGenres = new List<string>();

        public MemoryWallPlan LastPlan { get; private set; }

        public void Decorate(int seed)
        {
            if (Arena == null || Arena.Layout == null || Catalog == null || Level == null || DisplayKit == null)
                return;

            var level = Level.ToSpec();
            var slotCount = MemoryWallPlanner.CountSlots(Arena.Layout, level);
            var snapshot = MemoryCatalogExpander.EnsureCapacity(Catalog.ToSnapshot(), slotCount, level.MaxSameEntryPerWalk);
            var plan = MemoryWallPlanner.Plan(
                Arena.Layout,
                snapshot,
                level,
                new XorShiftRandom(seed),
                _previousGenres);

            MemoryWallApplier.Apply(plan, Arena, Catalog, DisplayKit);
            LastPlan = plan;
            _previousGenres.Clear();
            for (var i = 0; i < plan.GenreIdsUsed.Count; i++)
                _previousGenres.Add(plan.GenreIdsUsed[i]);
        }

        public void ResetGenreHistory()
        {
            _previousGenres.Clear();
        }
    }
}
