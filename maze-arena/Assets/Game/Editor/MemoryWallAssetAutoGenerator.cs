using Game.Unity.Memory;
using UnityEditor;

namespace Game.Editor
{
    [InitializeOnLoad]
    static class MemoryWallAssetAutoGenerator
    {
        const string CatalogPath = "Assets/Resources/Memory/DefaultMemoryCatalog.asset";
        const string SessionKey = "Game.Editor.MemoryWallAssetsGenerated";

        static MemoryWallAssetAutoGenerator()
        {
            EditorApplication.delayCall += TryGenerateOnce;
        }

        static void TryGenerateOnce()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;
            if (AssetDatabase.LoadAssetAtPath<MemoryWallCatalog>(CatalogPath) != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            MemoryWallDefaultAssetGenerator.Generate();
            SessionState.SetBool(SessionKey, true);
        }
    }
}
