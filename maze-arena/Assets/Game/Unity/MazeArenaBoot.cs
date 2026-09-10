using Game.Unity.Memory;
using Nixin.Maze;
using UnityEngine;

namespace Game.Unity
{
    public static class MazeArenaBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            HideSampleAvatar();

            var arena = Object.FindAnyObjectByType<MazeArena>();
            if (arena == null)
                arena = CreateArena();

            var memory = arena.GetComponent<MemoryArenaMemory>() ?? arena.gameObject.AddComponent<MemoryArenaMemory>();
            memory.Arena = arena;
            if (memory.Catalog == null)
                memory.Catalog = MemoryDefaults.LoadCatalog();
            else
                MemoryPhotoBinder.Apply(memory.Catalog);
            if (memory.Level == null)
                memory.Level = MemoryDefaults.LoadLevel();
            if (memory.DisplayKit == null)
                memory.DisplayKit = MemoryDefaults.LoadDisplayKit();

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
            if (camGo.GetComponent<Camera>() == null)
                camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            var camera = camGo.GetComponent<Camera>();
            var rig = camGo.GetComponent<OrbitCameraRig>() ?? camGo.AddComponent<OrbitCameraRig>();

            var ui = Object.FindAnyObjectByType<MazeHud>();
            if (ui == null)
            {
                ui = new GameObject("MazeHud").AddComponent<MazeHud>();
                ui.Styles = MazeThemeFactory.All();
                if (arena.StyleSet == null)
                    arena.StyleSet = ui.Styles[0];
            }

            ui.Arena = arena;
            ui.CameraRig = rig;
            ui.Memory = memory;

            if (Object.FindAnyObjectByType<TopDownMazeMapView>() == null)
            {
                var map = new GameObject("TopDownMap").AddComponent<TopDownMazeMapView>();
                map.Arena = arena;
            }

            var walker = Object.FindAnyObjectByType<MazeWalker>();
            if (walker == null)
                walker = MazeWalker.Create(arena, camera, rig);
            walker.Arena = arena;
            walker.EyeCamera = camera;
            walker.Orbit = rig;
            ui.Walker = walker;
            ui.SetWalk(true);

            var run = Object.FindAnyObjectByType<MazeRunController>()
                      ?? new GameObject("MazeRun").AddComponent<MazeRunController>();
            run.Arena = arena;
            run.Memory = memory;
            run.Walker = walker;
            run.Hud = ui;
            ui.RunController = run;
            run.StartCampaign(unchecked(System.Environment.TickCount == 0 ? 1 : System.Environment.TickCount));
        }

        static MazeArena CreateArena()
        {
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var arenaGo = new GameObject("MazeArena");
            var arena = arenaGo.AddComponent<MazeArena>();
            arena.Width = 6;
            arena.Height = 6;
            arena.PaintPhotos = false;
            arena.Prefabs = MazePrefabKit.LoadDefaults();
            var memory = arenaGo.AddComponent<MemoryArenaMemory>();
            memory.Arena = arena;
            memory.Catalog = MemoryDefaults.LoadCatalog();
            memory.Level = MemoryDefaults.LoadLevel();
            memory.DisplayKit = MemoryDefaults.LoadDisplayKit();
            return arena;
        }

        static void HideSampleAvatar()
        {
            var sample = GameObject.Find("RedCylinder");
            if (sample != null)
                sample.SetActive(false);
        }
    }
}
