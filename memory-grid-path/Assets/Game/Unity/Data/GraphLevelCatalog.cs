using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Data
{
    [CreateAssetMenu(fileName = "GraphLevelCatalog", menuName = "Nixin Studio/Memory Grid Path/Graph Level Catalog")]
    public sealed class GraphLevelCatalog : ScriptableObject
    {
        [SerializeField] List<GraphLevelDefinition> _levels = new List<GraphLevelDefinition>();

        public int Count => _levels.Count;

        public GraphLevelDefinition Get(int index)
        {
            if (index < 0 || index >= _levels.Count)
                throw new System.ArgumentOutOfRangeException(nameof(index));

            return _levels[index];
        }

        public IReadOnlyList<GraphLevelDefinition> Levels => _levels;
    }
}
