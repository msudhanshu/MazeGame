using Game.Core;
using Game.Unity.Memory;
using Nixin.Maze;
using Nixin.Rail;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Scene-owned game entry. Walk / debug defaults live here so they can be set in Edit Mode.
    /// Maze size, difficulty, and seed live on <see cref="MazeArena"/>; memory assets on
    /// <see cref="MemoryArenaMemory"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Nixin Studio/Maze Game")]
    public sealed class MazeGameHost : MonoBehaviour
    {
        [Header("Scene")]
        public MazeArena Arena;
        public Camera EyeCamera;
        public OrbitCameraRig Orbit;
        public MazeHud Hud;
        public MazeRunController Run;
        public MemoryArenaMemory Memory;
        public MazeWalker Walker;

        [Header("Walk")]
        public ArenaControlScheme Scheme = ArenaControlScheme.RailWaypoint;
        public LookStickMode LookStick = LookStickMode.AppearOnDrag;
        public WaypointStyle WaypointStyle = WaypointStyle.FloorTile;
        [Tooltip("Rail waypoint only. Required: tap the next cell. Skip: tap any chip until the next turn. Remove: hide straight-hall chips and only show the far turn.")]
        public IntermediateWaypointMode IntermediateWaypoint = IntermediateWaypointMode.Skip;
        public float RailMoveSpeed = RailTravel.DefaultMoveSpeed;
        public float JoystickMoveSpeed = RailDrive.DefaultMoveSpeed;
        public float LookDegreesPerSecond = 220f;
        [Tooltip("When on, look cannot spin a full circle. It stops at 180° left or right of spawn facing. A fade shows on the screen edge. Turn off to allow a full spin.")]
        public bool LimitLookYaw = true;
        [Tooltip("Rail waypoint only. When on, you can tilt your head down. When off, look is fixed at Default Look Down.")]
        public bool AllowLookDown = true;
        [Range(0f, 85f)]
        [Tooltip("Rail waypoint only. Fixed look-down angle when Allow Look Down is off. Keeps nearby floor chips in view. 0° is straight ahead.")]
        public float DefaultLookDown = RailLook.DefaultLookDown;
        [Range(15f, 85f)]
        [Tooltip("Rail waypoint only. How far you can tilt your head down toward floor chips. 0° is the horizon.")]
        public float MaxLookDown = RailLook.MaxLookDown;
        [Tooltip("Rail waypoint only, and only when Allow Look Down is off. A fast swipe down while walking parks you at least one cell short of the target so you can look at wall photos, then tap a chip to walk again.")]
        public bool SwipeToStop = true;
        [Tooltip("Laptop. A/D or arrows look. W/S walks the hall you face if you use them. Tap chips and touch look still work.")]
        public bool KeyboardWasd = true;

        [Header("Play")]
        [Tooltip("Shows the sandbox panel, orbit camera, and 2D map in Play. Leave off for player builds.")]
        public bool ShowDebugTools;
        [Tooltip("When on, Play uses the campaign level list (Courtyard, Alley, …). Size and difficulty come from that list. When off, this scene’s MazeArena width, height, difficulty, and seed are used.")]
        public bool UseCampaign = true;
        [Tooltip("When on, Play starts in first-person walk. When off, you start in orbit view (you can still walk later from debug tools).")]
        public bool StartWalking = true;

        void Awake()
        {
            if (Application.isPlaying)
                Wire();
        }

        void Start()
        {
            if (!Application.isPlaying)
                return;
            Wire();
            BeginSession();
        }

        void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            ApplyWalkSettings();
            ApplyDebugVisibility();
        }

        public void Wire()
        {
            HideSampleAvatar();

            if (Arena == null)
                Arena = FindInScene<MazeArena>() ?? CreateArena();

            Memory = Memory != null
                ? Memory
                : Arena.GetComponent<MemoryArenaMemory>() ?? Arena.gameObject.AddComponent<MemoryArenaMemory>();
            Memory.Arena = Arena;
            if (Memory.Catalog == null)
                Memory.Catalog = MemoryDefaults.LoadCatalog();
            else
                MemoryPhotoBinder.Apply(Memory.Catalog);
            if (Memory.Level == null)
                Memory.Level = MemoryDefaults.LoadLevel();
            if (Memory.DisplayKit == null)
                Memory.DisplayKit = MemoryDefaults.LoadDisplayKit();

            if (EyeCamera == null)
            {
                var main = Camera.main;
                if (main != null && main.gameObject.scene == gameObject.scene)
                    EyeCamera = main;
                else
                    EyeCamera = FindInScene<Camera>();
                if (EyeCamera == null)
                {
                    var camGo = new GameObject("Main Camera", typeof(Camera));
                    camGo.tag = "MainCamera";
                    EyeCamera = camGo.GetComponent<Camera>();
                }
            }

            Orbit = Orbit != null
                ? Orbit
                : EyeCamera.GetComponent<OrbitCameraRig>() ?? EyeCamera.gameObject.AddComponent<OrbitCameraRig>();

            if (Hud == null)
                Hud = FindInScene<MazeHud>();
            if (Hud == null)
                Hud = new GameObject("MazeHud").AddComponent<MazeHud>();

            if (Application.isPlaying && (Hud.Styles == null || Hud.Styles.Length == 0))
                Hud.Styles = MazeThemeFactory.All();
            if (Arena.StyleSet == null && Hud.Styles != null && Hud.Styles.Length > 0)
                Arena.StyleSet = Hud.Styles[0];

            Hud.Arena = Arena;
            Hud.CameraRig = Orbit;
            Hud.Memory = Memory;

            var map = FindInScene<TopDownMazeMapView>();
            if (map == null)
            {
                map = new GameObject("TopDownMap").AddComponent<TopDownMazeMapView>();
                map.Arena = Arena;
            }
            else
            {
                map.Arena = Arena;
            }

            if (Walker == null)
                Walker = FindInScene<MazeWalker>();
            if (Walker == null)
                Walker = MazeWalker.Create(Arena, EyeCamera, Orbit);
            Walker.Arena = Arena;
            Walker.EyeCamera = EyeCamera;
            Walker.Orbit = Orbit;
            Hud.Walker = Walker;
            var look = Walker.GetComponent<YawLookStick>();
            if (look != null)
                look.Hud = Hud;

            if (Run == null)
                Run = FindInScene<MazeRunController>();
            if (Run == null)
                Run = new GameObject("MazeRun").AddComponent<MazeRunController>();
            Run.Arena = Arena;
            Run.Memory = Memory;
            Run.Walker = Walker;
            Run.Hud = Hud;
            Hud.RunController = Run;

            ApplyWalkSettings();
            ApplyDebugVisibility();
        }

        public void ApplyWalkSettings()
        {
            if (Walker == null)
                return;

            var controls = Walker.Controls != null
                ? Walker.Controls
                : Walker.GetComponent<ArenaControls>();
            if (controls == null)
                return;

            controls.Scheme = Scheme;
            controls.LookStick = LookStick;
            controls.WaypointStyle = WaypointStyle;
            controls.IntermediateWaypoint = IntermediateWaypoint;
            controls.RailMoveSpeed = RailMoveSpeed;
            controls.JoystickMoveSpeed = JoystickMoveSpeed;
            controls.LookDegreesPerSecond = LookDegreesPerSecond;
            controls.LimitLookYaw = LimitLookYaw;
            controls.AllowLookDown = AllowLookDown;
            controls.DefaultLookDown = DefaultLookDown;
            controls.MaxLookDown = MaxLookDown;
            controls.SwipeToStop = SwipeToStop;
            controls.KeyboardWasd = KeyboardWasd;
            if (Application.isPlaying)
                Walker.ApplyLocomotion();
        }

        public void BeginSession()
        {
            ApplyWalkSettings();
            ApplyDebugVisibility();

            if (UseCampaign && Run != null)
                Run.StartCampaign(ClockSeed());
            else if (Arena != null)
                RebuildFromArena();

            if (StartWalking && Hud != null)
                Hud.SetWalk(true);
        }

        public static void HideSampleAvatar()
        {
            var sample = GameObject.Find("RedCylinder");
            if (sample != null)
                sample.SetActive(false);
        }

        void ApplyDebugVisibility()
        {
            if (Hud != null)
            {
                Hud.AllowDebugTools = ShowDebugTools;
                if (!ShowDebugTools)
                    Hud.DebugTools = false;
            }

            var map = FindInScene<TopDownMazeMapView>();
            if (map != null)
                map.enabled = ShowDebugTools;
        }

        void RebuildFromArena()
        {
            Arena.Rebuild(Arena.Seed);
            if (Memory != null)
                Memory.Decorate(Arena.Seed);
            if (Walker != null)
                Walker.PlaceAtEntry();
        }

        MazeArena CreateArena()
        {
            if (FindInScene<Light>() == null)
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

        static int ClockSeed()
        {
            var tick = System.Environment.TickCount;
            return tick == 0 ? 1 : tick;
        }

        T FindInScene<T>() where T : Component
        {
            var scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return FindAnyObjectByType<T>();

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
