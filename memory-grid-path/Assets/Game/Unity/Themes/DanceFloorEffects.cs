using System.Collections.Generic;
using Game.Unity.Audio;
using Game.Unity.Vfx;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Themes
{
    /// <summary>
    /// Dance-floor feedback: a white paint on a correct step, a held red tile plus a fail
    /// sting on a mistake, and catalog SFX from MemoryPathAudioCatalog.
    /// </summary>
    public sealed class DanceFloorEffects : MonoBehaviour, ITileEffects
    {
        public const float WrongFlashSeconds = 0.55f;
        public const float WrongIntenseFlashSeconds = 0.85f;

        public static DanceFloorEffects Create(Transform parent)
        {
            var go = new GameObject("DanceFloor Effects");
            go.transform.SetParent(parent, false);
            return go.AddComponent<DanceFloorEffects>();
        }

        public void PlayCorrect(ITileView tile)
        {
            tile?.SetState(TileVisualState.Walked);
            MemoryPathAudio.Play(MemoryPathCue.CorrectStep);
        }

        public void PlayMistake(ITileView wrong, ITileView revealed, bool intense = false)
        {
            _ = revealed;
            if (wrong == null)
            {
                MemoryPathAudio.PlayMistake();
                return;
            }

            var state = intense ? TileVisualState.WrongIntense : TileVisualState.Wrong;
            wrong.SetState(state);
            wrong.Flash(state, intense ? WrongIntenseFlashSeconds : WrongFlashSeconds);
            MemoryPathAudio.PlayMistake();
        }

        public void PlayRunFailed() => MemoryPathAudio.PlayWalkFail();

        public void PlaySessionFailed() => MemoryPathAudio.PlayLongFail();

        public void PlayLevelCompleted() => MemoryPathAudio.PlaySuccess();

        public void PlayPathGlimpse(IReadOnlyList<ITileView> pathTiles)
        {
            if (pathTiles == null)
                return;

            for (var i = 0; i < pathTiles.Count; i++)
                pathTiles[i]?.Flash(TileVisualState.Revealed, 0.7f);

            MemoryPathAudio.Play(MemoryPathCue.CorrectStep);
        }
    }
}
