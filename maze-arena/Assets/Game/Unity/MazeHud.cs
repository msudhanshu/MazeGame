using Game.Core;
using Game.Unity.Memory;
using Nixin.Maze;
using Nixin.Maze.Core;
using Nixin.Rail;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    public sealed class MazeHud : MonoBehaviour, ILookStickHud
    {
        public MazeArena Arena;
        public OrbitCameraRig CameraRig;
        public MazeWalker Walker;
        public MemoryArenaMemory Memory;
        public MazeRunController RunController;
        public MazeStyleSet[] Styles;
        public bool AllowDebugTools;
        public bool DebugTools;

        public bool BlocksPointer(float screenX, float screenY, float screenWidth, float screenHeight)
        {
            var yTop = screenHeight - screenY;
            if (screenX >= 16f && screenX <= 296f && yTop >= 16f && yTop <= 300f)
                return true;
            if (DebugTools)
            {
                const float w = 260f;
                if (screenX >= screenWidth - w - 16f && yTop >= 16f)
                    return true;
            }

            return false;
        }

        int _width = 8;
        int _height = 8;
        int _difficulty;
        float _braid = 0.25f;
        int _seed = 7;
        int _styleIndex;
        bool _photos = true;
        bool _memory = true;
        bool _bothFaces = true;
        bool _walk = true;
        readonly string[] _difficultyNames = { "Easy", "Medium", "Hard", "Brutal" };
        readonly MazePresetCatalog _catalog = MazePresetCatalog.Default();
        GUIStyle _wrap;

        void Start()
        {
            if (Arena != null)
            {
                _width = Arena.Width;
                _height = Arena.Height;
                _difficulty = (int)Arena.Difficulty;
                _braid = Arena.BraidFactor < 0f ? MazeDifficultyBands.DefaultBraid(Arena.Difficulty) : Arena.BraidFactor;
                _seed = Arena.Seed;
                _photos = Arena.PaintPhotos;
            }
        }

        public void SyncFromRun(MazeRun run)
        {
            if (run == null)
                return;
            var spec = run.CurrentSpec;
            _width = spec.Size.Width;
            _height = spec.Size.Height;
            _difficulty = (int)spec.Difficulty;
            _braid = spec.BraidFactor;
            _seed = run.AttemptSeed;
        }

        void OnGUI()
        {
            DrawPlayHud();
            if (AllowDebugTools && DebugTools)
                DrawSandboxHud();
        }

        void DrawPlayHud()
        {
            var run = RunController != null ? RunController.Run : null;
            GUILayout.BeginArea(new Rect(16, 16, 280, 260), GUI.skin.box);
            if (run == null)
            {
                GUILayout.Label("Maze Arena");
            }
            else
            {
                GUILayout.Label("Level " + (run.LevelIndex + 1) + " / " + run.LevelCount + "  " + run.CurrentLevel.Name);
                if (run.Phase == MazeRunPhase.Playing)
                {
                    GUILayout.Label("Time " + FormatTime(run.TimeRemaining));
                    GUILayout.Label("Find the exit");
                }
                else if (run.Phase == MazeRunPhase.Failed)
                {
                    GUILayout.Label("Time's up");
                    if (GUILayout.Button("Retry"))
                        RunController.Retry();
                }
                else if (run.Phase == MazeRunPhase.Complete)
                {
                    GUILayout.Label("You escaped the labyrinth");
                    if (GUILayout.Button("Play again"))
                        RunController.PlayAgain();
                }
            }

            if (AllowDebugTools)
            {
                DebugTools = GUILayout.Toggle(DebugTools, "Debug tools");
                if (Walker != null && GUILayout.Button(_walk ? "Orbit view" : "Walk maze"))
                    SetWalk(!_walk);
            }

            GUILayout.Label(_walk ? WalkHint() : "RMB orbit, scroll zoom");
            GUILayout.EndArea();
        }

        void DrawSandboxHud()
        {
            const float w = 300f;
            var stickReserve = Mathf.Min(Screen.width, Screen.height) * 0.42f;
            var hudH = Mathf.Max(180f, Screen.height - 32f - stickReserve);
            GUILayout.BeginArea(new Rect(Screen.width - w - 16, 16, w, hudH), GUI.skin.box);
            GUILayout.Label("Sandbox");
            DrawControlSchemeToggles();

            GUILayout.Label("Preset");
            for (var i = 0; i < _catalog.All.Count; i++)
            {
                var preset = _catalog.All[i];
                if (GUILayout.Button(preset.Name))
                    ApplyPreset(preset);
            }

            GUILayout.Space(8);
            GUILayout.Label("Size " + _width + " x " + _height);
            _width = Mathf.RoundToInt(GUILayout.HorizontalSlider(_width, 2, 24));
            _height = Mathf.RoundToInt(GUILayout.HorizontalSlider(_height, 2, 24));

            GUILayout.Label("Difficulty");
            _difficulty = GUILayout.Toolbar(_difficulty, _difficultyNames);

            GUILayout.Label("Braid " + _braid.ToString("0.00"));
            _braid = GUILayout.HorizontalSlider(_braid, 0f, 1f);

            GUILayout.Label("Seed " + _seed);
            if (GUILayout.Button("Reseed"))
                _seed++;
            int.TryParse(GUILayout.TextField(_seed.ToString()), out _seed);

            if (Styles != null && Styles.Length > 0)
            {
                GUILayout.Label("Wall style");
                var names = new string[Styles.Length];
                for (var i = 0; i < Styles.Length; i++)
                    names[i] = Styles[i] != null ? Styles[i].name : i.ToString();
                _styleIndex = GUILayout.Toolbar(_styleIndex % Styles.Length, names);
            }

            _photos = GUILayout.Toggle(_photos, "Plain photo walls");
            if (Memory != null && Memory.Catalog != null)
            {
                _memory = GUILayout.Toggle(_memory, "Memory wall catalog");
                if (_memory)
                    _bothFaces = GUILayout.Toggle(_bothFaces, "Both wall faces");
            }

            if (GUILayout.Button("Regenerate"))
                Rebuild();

            if (Arena != null && Arena.Layout != null)
            {
                var m = Arena.Layout.Metrics;
                GUILayout.Space(8);
                GUILayout.Label("Solution " + m.SolutionLength + "  turns " + m.TurnCount);
                GUILayout.Label("Dead ends " + m.DeadEndCount + "  score " + m.DifficultyScore.ToString("0.00"));
                if (_memory && Memory != null && Memory.LastPlan != null)
                    GUILayout.Label("Memory walls " + Memory.LastPlan.Placements.Count);
            }

            GUILayout.EndArea();
        }

        void ApplyPreset(MazePreset preset)
        {
            _width = preset.Width;
            _height = preset.Height;
            _difficulty = (int)preset.Difficulty;
            _braid = preset.BraidFactor;
            _seed = preset.Seed;
            Rebuild();
        }

        void Rebuild()
        {
            if (Arena == null)
                return;

            Arena.Width = Mathf.Max(2, _width);
            Arena.Height = Mathf.Max(2, _height);
            Arena.Difficulty = (MazeDifficulty)_difficulty;
            Arena.BraidFactor = _braid;
            var useMemory = _memory && Memory != null && Memory.Catalog != null && Memory.Level != null && Memory.DisplayKit != null;
            Arena.PaintPhotos = !useMemory && _photos;
            if (Styles != null && Styles.Length > 0)
                Arena.StyleSet = Styles[Mathf.Clamp(_styleIndex, 0, Styles.Length - 1)];

            Arena.Rebuild(_seed);
            if (useMemory)
            {
                if (Memory.Level != null)
                    Memory.Level.DecorateOppositeFaces = _bothFaces;
                Memory.Decorate(_seed);
            }
            if (Walker != null)
                Walker.PlaceAtEntry();
            if (_walk && Walker != null)
                Walker.SetWalking(true);
            else
                FrameCamera();
        }

        public void SetWalk(bool walk)
        {
            _walk = walk;
            if (Walker == null)
                return;
            Walker.SetWalking(walk);
            if (!walk)
                FrameCamera();
        }

        static string FormatTime(float seconds)
        {
            var clamped = Mathf.Max(0f, seconds);
            var whole = Mathf.CeilToInt(clamped);
            var m = whole / 60;
            var s = whole % 60;
            return m + ":" + s.ToString("00");
        }

        void DrawControlSchemeToggles()
        {
            if (Walker == null || Walker.Controls == null)
                return;

            GUILayout.Label("Walk scheme");
            var current = Walker.Controls.Scheme;
            for (var i = 0; i < ArenaControlSchemes.All.Length; i++)
            {
                var scheme = ArenaControlSchemes.All[i];
                var on = current == scheme;
                if (GUILayout.Toggle(on, ArenaControlSchemes.Name(scheme), GUI.skin.button) && !on)
                {
                    Walker.Controls.Scheme = scheme;
                    Walker.ApplyLocomotion();
                    current = scheme;
                }
            }

            GUILayout.Label(ArenaControlSchemes.Description(current), Wrap());
            GUILayout.Space(6);

            Walker.Controls.LimitLookYaw = GUILayout.Toggle(Walker.Controls.LimitLookYaw, "Limit Look Yaw");
            GUILayout.Label("Stops look at 180° left or right of spawn facing.", Wrap());

            if (current != ArenaControlScheme.RailWaypoint && current != ArenaControlScheme.RailJoystick)
                return;

            GUILayout.Label("Look Stick");
            DrawLookStickButtons();

            Walker.Controls.KeyboardWasd = GUILayout.Toggle(Walker.Controls.KeyboardWasd, "Keyboard WASD");
            GUILayout.Label("Laptop: A/D looks, W/S walks the hall you face. Tap chips and touch look still work.", Wrap());

            if (current != ArenaControlScheme.RailWaypoint)
                return;

            GUILayout.Label("Waypoint Style");
            DrawWaypointStyleButtons();
            GUILayout.Label("Intermediate Waypoints");
            DrawIntermediateButtons();
            GUILayout.Label(IntermediateWaypoints.Description(Walker.Controls.IntermediateWaypoint), Wrap());
            Walker.Controls.AllowLookDown = GUILayout.Toggle(Walker.Controls.AllowLookDown, "Allow Look Down");
            if (Walker.Controls.AllowLookDown && Walker.Controls.WaypointStyle == WaypointStyle.FloorTile)
            {
                GUILayout.Label("Max Look Down " + Mathf.RoundToInt(Walker.Controls.MaxLookDown) + "°");
                GUILayout.Label("How far you can tilt your head down toward floor chips.", Wrap());
                Walker.Controls.MaxLookDown = GUILayout.HorizontalSlider(Walker.Controls.MaxLookDown, 15f, 85f);
            }
            else if (!Walker.Controls.AllowLookDown)
            {
                GUILayout.Label("Default Look Down " + Mathf.RoundToInt(Walker.Controls.DefaultLookDown) + "°");
                GUILayout.Label("Fixed look-down so nearby floor chips stay in view.", Wrap());
                Walker.Controls.DefaultLookDown = GUILayout.HorizontalSlider(Walker.Controls.DefaultLookDown, 0f, 85f);
                Walker.Controls.SwipeToStop = GUILayout.Toggle(Walker.Controls.SwipeToStop, "Swipe To Stop");
                GUILayout.Label("Fast swipe down while walking parks you to look at wall photos.", Wrap());
            }
            else
            {
                GUILayout.Label("Look stays on the horizon for space blobs.", Wrap());
            }
        }

        GUIStyle Wrap()
        {
            if (_wrap == null)
                _wrap = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 };
            return _wrap;
        }

        void DrawLookStickButtons()
        {
            var current = Walker.Controls.LookStick;
            DrawModeButton(current == LookStickMode.FixedBottom, LookStickModes.Name(LookStickMode.FixedBottom),
                () => Walker.Controls.LookStick = LookStickMode.FixedBottom);
            DrawModeButton(current == LookStickMode.AppearOnDrag, LookStickModes.Name(LookStickMode.AppearOnDrag),
                () => Walker.Controls.LookStick = LookStickMode.AppearOnDrag);
            DrawModeButton(current == LookStickMode.HiddenDrag, LookStickModes.Name(LookStickMode.HiddenDrag),
                () => Walker.Controls.LookStick = LookStickMode.HiddenDrag);
        }

        void DrawWaypointStyleButtons()
        {
            var current = Walker.Controls.WaypointStyle;
            DrawModeButton(current == WaypointStyle.FloorTile, "Floor tile",
                () => Walker.Controls.WaypointStyle = WaypointStyle.FloorTile);
            DrawModeButton(current == WaypointStyle.SpaceBlob, "Space blob",
                () => Walker.Controls.WaypointStyle = WaypointStyle.SpaceBlob);
        }

        void DrawIntermediateButtons()
        {
            var current = Walker.Controls.IntermediateWaypoint;
            DrawModeButton(current == IntermediateWaypointMode.Required, IntermediateWaypoints.Name(IntermediateWaypointMode.Required),
                () => Walker.Controls.IntermediateWaypoint = IntermediateWaypointMode.Required);
            DrawModeButton(current == IntermediateWaypointMode.Skip, IntermediateWaypoints.Name(IntermediateWaypointMode.Skip),
                () => Walker.Controls.IntermediateWaypoint = IntermediateWaypointMode.Skip);
            DrawModeButton(current == IntermediateWaypointMode.Remove, IntermediateWaypoints.Name(IntermediateWaypointMode.Remove),
                () => Walker.Controls.IntermediateWaypoint = IntermediateWaypointMode.Remove);
        }

        static void DrawModeButton(bool on, string label, System.Action select)
        {
            if (GUILayout.Toggle(on, label, GUI.skin.button) && !on)
                select();
        }

        string WalkHint()
        {
            var scheme = Walker != null && Walker.Controls != null
                ? Walker.Controls.ActiveScheme
                : ArenaControlScheme.RailWaypoint;
            if (scheme == ArenaControlScheme.RailJoystick)
            {
                var mode = Walker != null && Walker.Controls != null
                    ? Walker.Controls.LookStick
                    : LookStickMode.AppearOnDrag;
                var keys = Walker != null && Walker.Controls != null && Walker.Controls.KeyboardWasd
                    ? "; A/D looks, W/S walks"
                    : "";
                if (mode == LookStickMode.FixedBottom)
                    return "Bottom stick: left/right looks, up/down walks the corridor you face" + keys;
                return "Drag: left/right looks, up/down walks the corridor you face" + keys;
            }

            if (scheme == ArenaControlScheme.RailWaypoint)
            {
                var mode = Walker != null && Walker.Controls != null
                    ? Walker.Controls.LookStick
                    : LookStickMode.AppearOnDrag;
                var blob = Walker.Controls.WaypointStyle == WaypointStyle.SpaceBlob;
                var keys = Walker.Controls.KeyboardWasd
                    ? "; A/D looks, W/S walks the hall"
                    : "";
                if (mode == LookStickMode.FixedBottom)
                    return blob
                        ? "Tap a glowing blob to walk; bottom stick looks" + keys
                        : "Tap a floor chip to walk; bottom stick looks around" + keys;
                return blob
                    ? "Tap a glowing blob to walk; drag to look" + keys
                    : "Tap a floor chip to walk (far chips skip a straight hall); drag to look around" + keys;
            }

#if UNITY_EDITOR
            var mobile = UnityEngine.Device.Application.isMobilePlatform;
#else
            var mobile = Application.isMobilePlatform;
#endif
            return mobile
                ? "Left stick move, right stick look"
                : "WASD move, mouse look (Esc frees cursor)";
        }

        public void FrameCamera()
        {
            if (Arena == null || CameraRig == null || Arena.Dimensions == null)
                return;

            var size = Arena.Dimensions.CellSize * Mathf.Max(Arena.Width, Arena.Height);
            var center = new Vector3(
                Arena.Width * Arena.Dimensions.CellSize * 0.5f,
                Arena.Dimensions.WallHeight * 0.4f,
                Arena.Height * Arena.Dimensions.CellSize * 0.5f);
            CameraRig.Frame(center, size);
        }
    }
}
