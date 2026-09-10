using UnityEngine;

namespace Game.Unity.Memory
{
    public static class MemoryPhotoBinder
    {
        public const string ResourcesFolder = "MazePhotos";

        public static void Apply(MemoryWallCatalog catalog)
        {
            if (catalog == null)
                return;

            var photos = Resources.LoadAll<Texture2D>(ResourcesFolder);
            catalog.BindDroppedPhotos(photos);
        }
    }
}
