using System.Threading.Tasks;
using Game.Core.Domain;
using Nixin.Boot;
using UnityEngine;

namespace Game.Unity
{
    public static class GridPathBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Object.FindAnyObjectByType<GridPathPlay>() != null)
                return;
            if (Object.FindAnyObjectByType<TestLab.ArenaTestLab>() != null)
                return;
            if (Object.FindAnyObjectByType<GraphPathPlay>() != null)
                return;
            if (Object.FindAnyObjectByType<JourneyHubPlay>() != null)
                return;
            if (Object.FindAnyObjectByType<LoadingScreen>() != null)
                return;

            GameBoot.Run(
                new ILoadTask[]
                {
                    new DelegateLoadTask("Reading levels", 1f, (progress, cancellationToken) =>
                    {
                        var catalog = new LevelCatalog();
                        for (var level = 1; level <= catalog.Count; level++)
                        {
                            catalog.Get(level);
                            progress.Report((float)level / catalog.Count);
                        }

                        return Task.CompletedTask;
                    }),
                    new DelegateLoadTask("Laying the floor", 2f, (progress, cancellationToken) =>
                    {
                        // Generating the first level up front catches an impossible shape spec
                        // here, on the loading screen, instead of mid-play.
                        var factory = new LevelPathFactory(new LevelCatalog());
                        _ = factory.Create(1, seed: 1);
                        progress.Report(1f);
                        return Task.CompletedTask;
                    })
                },
                () =>
                {
                    var go = new GameObject("GridPath");
                    go.AddComponent<GridPathPlay>();
                },
                AppIdentity.ProductName);
        }
    }
}
