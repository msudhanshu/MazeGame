using UnityEngine;

namespace Game.Unity.Memory
{
    /// <summary>
    /// Loads authored memory-wall assets from Resources, with procedural factory fallback.
    /// </summary>
    public static class MemoryDefaults
    {
        const string CatalogPath = "Memory/DefaultMemoryCatalog";
        const string LevelPath = "Memory/DefaultMemoryLevel";
        const string DisplayKitPath = "Memory/DefaultMemoryDisplayKit";

        public static MemoryWallCatalog LoadCatalog()
        {
            var catalog = Resources.Load<MemoryWallCatalog>(CatalogPath)
                          ?? MemoryCatalogFactory.CreateDefaultCatalog();
            MemoryPhotoBinder.Apply(catalog);
            return catalog;
        }

        public static MemoryLevelProfile LoadLevel()
        {
            return Resources.Load<MemoryLevelProfile>(LevelPath)
                   ?? MemoryCatalogFactory.CreateDefaultLevel();
        }

        public static MemoryWallDisplayKit LoadDisplayKit()
        {
            return Resources.Load<MemoryWallDisplayKit>(DisplayKitPath)
                   ?? MemoryCatalogFactory.CreateDefaultDisplayKit();
        }
    }
}
