using UnityEngine;

namespace Game.Unity.Audio
{
    /// <summary>
    /// One place to swap clips and volumes for Memory Path SFX.
    /// Select this asset in the Project window to tune.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MemoryPathAudioCatalog",
        menuName = "Nixin Studio/Memory Grid Path/Audio Catalog")]
    public sealed class MemoryPathAudioCatalog : ScriptableObject
    {
        public const string ResourceName = "MemoryPathAudioCatalog";
        public const string EditorAssetPath = "Assets/Game/Unity/Audio/Resources/MemoryPathAudioCatalog.asset";

        [Header("Menus and buttons")]
        [SerializeField] MemoryPathCueSlot _uiClick = new MemoryPathCueSlot(null, 0.55f);

        [Header("Play session")]
        [SerializeField] MemoryPathCueSlot _gameStart = new MemoryPathCueSlot(null, 0.7f);
        [SerializeField] MemoryPathCueSlot _success = new MemoryPathCueSlot(null, 0.8f);
        [SerializeField] MemoryPathCueSlot _fail = new MemoryPathCueSlot(null, 0.7f);

        [Header("Steps on the board")]
        [SerializeField] MemoryPathCueSlot _correctStep = new MemoryPathCueSlot(null, 0.45f);
        [SerializeField] MemoryPathCueSlot _mistake = new MemoryPathCueSlot(null, 0.55f);

        public MemoryPathCueSlot Slot(MemoryPathCue cue)
        {
            switch (cue)
            {
                case MemoryPathCue.GameStart:
                    return _gameStart;
                case MemoryPathCue.Success:
                    return _success;
                case MemoryPathCue.Fail:
                    return _fail;
                case MemoryPathCue.CorrectStep:
                    return _correctStep;
                case MemoryPathCue.Mistake:
                    return _mistake;
                default:
                    return _uiClick;
            }
        }

        public void Apply(MemoryPathCue cue, AudioClip clip, float volume)
        {
            var slot = new MemoryPathCueSlot(clip, volume);
            switch (cue)
            {
                case MemoryPathCue.GameStart:
                    _gameStart = slot;
                    break;
                case MemoryPathCue.Success:
                    _success = slot;
                    break;
                case MemoryPathCue.Fail:
                    _fail = slot;
                    break;
                case MemoryPathCue.CorrectStep:
                    _correctStep = slot;
                    break;
                case MemoryPathCue.Mistake:
                    _mistake = slot;
                    break;
                default:
                    _uiClick = slot;
                    break;
            }
        }
    }
}
