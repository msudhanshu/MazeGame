using System.Threading.Tasks;
using Game.Core.Domain;
using Nixin.Boot;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Unity
{
    public static class DetectiveBoot
    {
        public const string BootScene = "Boot";
        public const string PlayScene = "Play";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<LoadingScreen>() != null)
                return;
            if (Object.FindAnyObjectByType<InvestigationPlay>() != null)
                return;

            var scene = SceneManager.GetActiveScene().name;
            if (scene == PlayScene)
            {
                StartInvestigation();
                return;
            }

            if (scene != BootScene)
                return;

            GameBoot.Run(
                new ILoadTask[]
                {
                    new DelegateLoadTask("Loading case file", 1f, (progress, ct) =>
                    {
                        _ = FirstCase.ManorMurder();
                        progress.Report(1f);
                        return Task.CompletedTask;
                    }),
                    new DelegateLoadTask("Fetching briefing", 2f, async (progress, ct) =>
                    {
                        for (var i = 1; i <= 5; i++)
                        {
                            ct.ThrowIfCancellationRequested();
                            await Task.Delay(90, ct);
                            progress.Report(i / 5f);
                        }
                    }),
                    new SceneLoadTask("Opening investigation", 3f, PlayScene)
                },
                StartInvestigation,
                "Memory Detective");
        }

        public static void StartInvestigation()
        {
            if (Object.FindAnyObjectByType<InvestigationPlay>() != null)
                return;
            var go = new GameObject("Investigation");
            go.AddComponent<InvestigationPlay>();
        }

        public static void ReloadPlay()
        {
            GameBoot.ToScene(
                PlayScene,
                StartInvestigation,
                "Loading case",
                new DelegateLoadTask("Preparing next case", 1f, (progress, ct) =>
                {
                    _ = FirstCase.ManorMurder();
                    progress.Report(1f);
                    return Task.CompletedTask;
                }));
        }
    }
}
