using System;
using UnityEngine;

namespace Game.Unity.Memory
{
    [CreateAssetMenu(fileName = "MemoryWallDisplayKit", menuName = "Nixin Studio/Maze Arena/Memory Wall Display Kit")]
    public sealed class MemoryWallDisplayKit : ScriptableObject
    {
        [SerializeField] GameObject defaultPhotoPrefab;
        [SerializeField] FrameVariant[] frameVariants = Array.Empty<FrameVariant>();
        [SerializeField] Material reliefMaterial;
        [SerializeField] float reliefPictureScale = 0.75f;
        [SerializeField] float objectScale = 0.35f;

        public GameObject DefaultPhotoPrefab => defaultPhotoPrefab;
        public Material ReliefMaterial => reliefMaterial;
        public float ReliefPictureScale => reliefPictureScale;
        public float ObjectScale => objectScale;

        public void Configure(
            GameObject photoPrefab,
            Material reliefMat = null,
            FrameVariant[] variants = null,
            float reliefScale = 0.75f,
            float objScale = 0.35f)
        {
            defaultPhotoPrefab = photoPrefab;
            if (reliefMat != null)
                reliefMaterial = reliefMat;
            if (variants != null)
                frameVariants = variants;
            reliefPictureScale = reliefScale;
            objectScale = objScale;
        }

        public GameObject FramePrefabFor(string variantId)
        {
            if (string.IsNullOrEmpty(variantId))
                return defaultPhotoPrefab;

            for (var i = 0; i < frameVariants.Length; i++)
            {
                if (frameVariants[i] != null && frameVariants[i].Id == variantId)
                    return frameVariants[i].Prefab != null ? frameVariants[i].Prefab : defaultPhotoPrefab;
            }

            return defaultPhotoPrefab;
        }

        [Serializable]
        public sealed class FrameVariant
        {
            public string Id = "plain";
            public GameObject Prefab;
        }
    }
}
