namespace Game.Unity.View
{
    /// <summary>
    /// Hold-to-pause clock used while a radar path preview plays.
    /// </summary>
    public interface IRadarScanControls
    {
        bool HeldPaused { get; }

        void SetProgress(float elapsedSeconds, float totalSeconds);
    }
}
