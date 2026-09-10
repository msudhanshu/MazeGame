using Game.Core.State;

namespace Game.Core.Fue
{
    /// <summary>
    /// First Graph Arena level coach for pinch/scroll zoom and drag pan.
    /// Completes from real gestures, not camera drift caused by zoom-to-cursor.
    /// </summary>
    public sealed class GraphLevelOneFueSession
    {
        public GraphLevelOneFueBeat Beat { get; private set; } = GraphLevelOneFueBeat.PromptZoom;

        public bool IsActive => Beat != GraphLevelOneFueBeat.Completed;
        public bool IsComplete => Beat == GraphLevelOneFueBeat.Completed;

        public static GraphLevelOneFueSession TryStart(GameModeId mode, int levelNumber, bool alreadySeen)
        {
            if (!GraphLevelOneFueSpec.ShouldStart(mode, levelNumber, alreadySeen))
                return null;

            return new GraphLevelOneFueSession();
        }

        public void ObserveGestures(bool zoomed, bool panned)
        {
            if (Beat == GraphLevelOneFueBeat.PromptZoom && zoomed)
                Beat = GraphLevelOneFueBeat.PromptPan;

            if (Beat == GraphLevelOneFueBeat.PromptPan && panned)
                Beat = GraphLevelOneFueBeat.Completed;
        }
    }
}
