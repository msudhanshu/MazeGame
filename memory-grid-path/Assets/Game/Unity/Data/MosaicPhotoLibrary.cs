using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Unity.Data
{
    /// <summary>
    /// Photos under Resources/Photo/Mosaic for mosaic arena levels.
    /// Picks are seeded per level so catalog rebuilds stay stable.
    /// </summary>
    public static class MosaicPhotoLibrary
    {
        public const string ResourcePath = "Photo/Mosaic";
#if UNITY_EDITOR
        public const string AssetFolder = "Assets/Game/Unity/Resources/Photo/Mosaic";
#endif
        public const float DefaultParticleChance = 0.35f;

        public static Texture2D[] LoadAll()
        {
            var textures = Resources.LoadAll<Texture2D>(ResourcePath);
#if UNITY_EDITOR
            if (textures == null || textures.Length == 0)
                textures = LoadAllFromAssetDatabase();
#endif
            if (textures == null || textures.Length == 0)
                return Array.Empty<Texture2D>();

            Array.Sort(textures, (a, b) => string.CompareOrdinal(a.name, b.name));
            return textures;
        }

#if UNITY_EDITOR
        static Texture2D[] LoadAllFromAssetDatabase()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AssetFolder });
            if (guids == null || guids.Length == 0)
                return Array.Empty<Texture2D>();

            var textures = new Texture2D[guids.Length];
            var count = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (texture == null)
                    continue;
                textures[count++] = texture;
            }

            if (count == textures.Length)
                return textures;

            Array.Resize(ref textures, count);
            return textures;
        }
#endif

        public static Texture2D Pick(int levelNumber)
        {
            var all = LoadAll();
            if (all.Length == 0)
                return null;

            var rng = new System.Random(9000 + Math.Max(1, levelNumber));
            return all[rng.Next(all.Length)];
        }

        public static bool RollParticles(int levelNumber, float chance = DefaultParticleChance)
        {
            var rng = new System.Random(17000 + Math.Max(1, levelNumber));
            return rng.NextDouble() < chance;
        }
    }
}
