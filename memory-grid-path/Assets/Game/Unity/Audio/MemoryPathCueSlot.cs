using System;
using UnityEngine;

namespace Game.Unity.Audio
{
    [Serializable]
    public struct MemoryPathCueSlot
    {
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume;

        public MemoryPathCueSlot(AudioClip clip, float volume)
        {
            Clip = clip;
            Volume = Mathf.Clamp01(volume);
        }

        public float ClampedVolume => Mathf.Clamp01(Volume);
    }
}
