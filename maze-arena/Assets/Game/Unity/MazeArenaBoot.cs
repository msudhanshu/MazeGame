using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Fallback for empty scenes such as MazeSandbox. A scene that already has
    /// <see cref="MazeGameHost"/> boots from that object instead.
    /// </summary>
    public static class MazeArenaBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<MazeGameHost>() != null)
                return;

            MazeGameHost.HideSampleAvatar();
            var host = new GameObject("MazeGame").AddComponent<MazeGameHost>();
            host.ShowDebugTools = true;
            host.Wire();
        }
    }
}
