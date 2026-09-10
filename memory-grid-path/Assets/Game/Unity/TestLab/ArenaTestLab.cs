using System.Collections.Generic;
using Game.Core.Rules;
using Game.Unity.Data;
using Game.Unity.Graph;
using Game.Unity.Input;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.Ui;
using Game.Unity.View;
using Nixin.Game.Core;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity.TestLab
{
    /// <summary>
    /// Standalone harness for arena visual experiments. Open the Arena Test Lab scene and press Play.
    /// </summary>
    public sealed class ArenaTestLab : MonoBehaviour
    {
        [SerializeField] TestLabScenario _scenario = TestLabScenario.ClassicColorPath;
        [SerializeField] int _gridWidth = 6;
        [SerializeField] int _gridHeight = 6;
        [SerializeField] int _pathSeed = 42;
        [SerializeField] ArenaVisualSettings _arenaSettings;
        [SerializeField] PatchworkTextureSet _patchworkTextureSetOverride;
        [SerializeField] GraphLevelDefinition _graphLevel;

        GridBoardView _board;
        GraphBoardView _graphBoard;
        WalkerView _walker;
        GridWalkRun _gridRun;
        GraphWalkRun _graphRun;
        Camera _camera;
        TestLabScenarioResolver.Profile _profile;
        readonly BoardInput _gridInput = new BoardInput();
        readonly GraphBoardInput _graphInput = new GraphBoardInput();
        readonly GraphNodeCircleViewFactory _graphFactory = new GraphNodeCircleViewFactory();
        readonly GraphViewportController _graphViewport = new GraphViewportController();

        Texture2D _proceduralMosaic;
        Texture2D[] _proceduralPatchwork;
        bool _showPath;
        string _status = string.Empty;

        bool IsGraphMode => _scenario == TestLabScenario.GraphNodeArena;

        void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraGo = new GameObject("Main Camera");
                _camera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            _arenaSettings = _arenaSettings != null ? _arenaSettings : LoadArenaVisualSettings();
            if (_graphLevel == null)
                _graphLevel = LoadGraphLevel() ?? GraphLevelDefinition.CreateSampleRuntime();
        }

        void Start() => Rebuild();

        void LateUpdate()
        {
            if (IsGraphMode)
            {
                if (_graphBoard != null && _graphBoard.IsBuilt && _graphViewport.IsActive)
                    _graphViewport.Apply(_camera);
                return;
            }

            UpdateFollowCamera();
        }

        void Update()
        {
            if (TryReadScenarioHotkey(out var next))
            {
                _scenario = next;
                Rebuild();
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    _pathSeed++;
                    Rebuild();
                    return;
                }

                if (keyboard.hKey.wasPressedThisFrame)
                {
                    _showPath = !_showPath;
                    RefreshBoard();
                }
            }

            if (IsGraphMode)
                UpdateGraph();
            else
                UpdateGrid();
        }

        void UpdateGrid()
        {
            if (_gridRun == null || _gridRun.IsOver || _board == null || !_board.IsBuilt)
                return;

            if (_gridRun.IsAwaitingNextWalk)
            {
                _gridRun.BeginNextWalk();
                _walker.SnapTo(_board.WorldPosition(_gridRun.CurrentCell));
                RefreshBoard();
                _status = string.Empty;
                return;
            }

            var hopping = _walker != null && _walker.IsHopping;
            var visibleOptions = Game.Core.Domain.PathOptionFilter.VisibleGridOptions(_gridRun.WalkedCells, _gridRun.Options());
            var hasTarget = _gridInput.TryReadTarget(
                _camera,
                _board,
                _gridRun.CurrentCell,
                visibleOptions,
                PlayerSettingsStore.PathDrag,
                _gridRun.IsOption,
                out var target);
            if (hopping)
                return;

            if (!hasTarget || !_gridRun.IsOption(target) || !Game.Core.Domain.PathOptionFilter.Contains(visibleOptions, target))
                return;

            StepGrid(target);
        }

        void UpdateGraph()
        {
            if (_graphRun == null || _graphRun.IsOver || _graphBoard == null || !_graphBoard.IsBuilt)
                return;

            if (_graphRun.IsAwaitingNextWalk)
            {
                _graphRun.BeginNextWalk();
                _walker.SnapTo(_graphBoard.WorldPosition(_graphRun.CurrentNode));
                RefreshBoard();
                _status = string.Empty;
                return;
            }

            if (_walker != null && _walker.IsHopping)
                return;

            _graphViewport.UpdateInput(_camera, _graphBoard.Layout, allowInput: true);
            if (_graphViewport.BlocksNodePick)
                return;

            var options = Game.Core.Domain.PathOptionFilter.VisibleGraphOptions(_graphRun.WalkedNodes, _graphRun.Options());
            if (!_graphInput.TryReadTarget(_camera, _graphBoard, _graphRun.CurrentNode, options, out var target))
                return;

            if (!_graphRun.IsOption(target))
                return;

            StepGraph(target);
        }

        void OnGUI()
        {
            const int pad = 12;
            var box = new Rect(pad, pad, 460f, 180f);
            GUI.Box(box, string.Empty);
            GUILayout.BeginArea(box);
            GUILayout.Label("<b>Arena Test Lab</b>");
            GUILayout.Label(_profile.Title ?? _scenario.ToString());
            GUILayout.Label(_profile.Hint ?? string.Empty);
            GUILayout.Space(4f);
            GUILayout.Label("1 Classic  |  2 Mosaic+coords  |  3 Patchwork+cam  |  4 Graph nodes");
            GUILayout.Label(IsGraphMode
                ? "Click node or 1–6 pick option  |  Pinch/scroll zoom, drag pan  |  R new path  |  H reveal path"
                : "Arrows/WASD move  |  R new path  |  H toggle path reveal");
            if (!string.IsNullOrEmpty(_status))
                GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        void Rebuild()
        {
            TearDown();
            _profile = TestLabScenarioResolver.Resolve(
                _scenario,
                _arenaSettings,
                _proceduralMosaic,
                _proceduralPatchwork,
                _patchworkTextureSetOverride);

            if (IsGraphMode)
                RebuildGraph();
            else
                RebuildGrid();
        }

        void RebuildGrid()
        {
            EnsureProceduralAssets();

            var size = new GridSize(Mathf.Max(3, _gridWidth), Mathf.Max(3, _gridHeight));
            var start = new GridCoord(0, 0);
            var goal = new GridCoord(size.Width - 1, size.Height - 1);
            var minLength = Mathf.Max(4, (size.Width + size.Height) / 2);
            var maxLength = size.Width * size.Height;
            var shape = new PathShapeSpec(minLength, maxLength, minTurns: 2, maxTurns: maxLength);

            GridPath path;
            try
            {
                path = SelfAvoidingPathGenerator.Generate(size, start, goal, shape, new XorShiftRandom(_pathSeed));
            }
            catch (PathGenerationException)
            {
                shape = new PathShapeSpec(4, maxLength, 0, maxLength);
                path = SelfAvoidingPathGenerator.Generate(size, start, goal, shape, new XorShiftRandom(_pathSeed));
            }

            _gridRun = new GridWalkRun(path);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GridBoardView>();
            _board.Build(size, _profile.Factory, _profile.TileSize, _profile.TileGap);

            _profile.Factory.ApplyEnvironment(_camera, _board.Layout, transform);

            _walker = WalkerView.Create(transform, _board.WorldPosition(_gridRun.CurrentCell), DanceFloorPalette.Start);
            ApplyCameraFraming();
            RefreshBoard();
            _status = string.Empty;
        }

        void RebuildGraph()
        {
            _graphRun = GraphBoardPresenter.CreateRun(_graphLevel, _pathSeed);

            var boardGo = new GameObject("GraphBoard");
            boardGo.transform.SetParent(transform, false);
            _graphBoard = boardGo.AddComponent<GraphBoardView>();
            _graphBoard.Build(_graphLevel, _graphFactory);

            _graphFactory.ApplyEnvironment(_camera, _graphBoard.Layout, transform);
            _graphViewport.Reset(_graphBoard.Layout, _camera.aspect);
            _graphViewport.Apply(_camera);

            _walker = WalkerView.Create(transform, _graphBoard.WorldPosition(_graphRun.CurrentNode), DanceFloorPalette.Start);
            RefreshBoard();
            _status = string.Empty;
        }

        void StepGrid(GridCoord target)
        {
            var outcome = _gridRun.Choose(target);
            if (outcome != WalkOutcome.Advanced && outcome != WalkOutcome.LevelCompleted)
                _gridInput.StopPathDrag();
            HandleOutcome(outcome, _board.WorldPosition(_gridRun.CurrentCell));
            RefreshBoard();
        }

        void StepGraph(GraphNodeId target)
        {
            var from = _graphRun.CurrentNode;
            var outcome = _graphRun.Choose(target);
            HandleOutcome(outcome, _graphBoard.WorldPosition(_graphRun.CurrentNode), _graphBoard.EdgeWorldPoints(from, _graphRun.CurrentNode));
            RefreshBoard();
        }

        void HandleOutcome(WalkOutcome outcome, Vector3 walkerPosition, IReadOnlyList<Vector3> hopPath = null)
        {
            switch (outcome)
            {
                case WalkOutcome.Advanced:
                case WalkOutcome.WrongRevealed:
                    HopWalker(walkerPosition, hopPath);
                    _status = outcome == WalkOutcome.WrongRevealed
                        ? $"Off path — {_graphRun?.LivesLeft ?? _gridRun.LivesLeft} lives left."
                        : string.Empty;
                    break;

                case WalkOutcome.RunFailed:
                    HopWalker(walkerPosition, hopPath);
                    _status = "Run failed — pick a node to restart the walk.";
                    break;

                case WalkOutcome.LevelCompleted:
                    HopWalker(walkerPosition, hopPath);
                    _status = "Goal reached! Press R for a new path.";
                    break;

                case WalkOutcome.SessionOver:
                    HopWalker(walkerPosition, hopPath);
                    _status = "Session over. Press R to try again.";
                    break;
            }
        }

        void HopWalker(Vector3 destination, IReadOnlyList<Vector3> hopPath)
        {
            if (hopPath != null && hopPath.Count >= 2)
                _walker.HopAlong(hopPath);
            else
                _walker.HopTo(destination);
        }

        void RefreshBoard()
        {
            if (IsGraphMode)
            {
                GraphBoardPresenter.Refresh(_graphBoard, _graphRun, _showPath);
                return;
            }

            if (_board == null || !_board.IsBuilt || _gridRun == null)
                return;

            GridBoardPresenter.Refresh(_board, _gridRun, _showPath);
        }

        void ApplyCameraFraming()
        {
            if (_camera == null || _board == null || !_board.IsBuilt)
                return;

            if (_profile.CameraMode == ArenaCameraMode.FollowWalker && _walker != null)
            {
                var focus = _walker.transform.position;
                focus.y = _board.Layout.Origin.y;
                BoardCamera.FrameFollow(_camera, focus, _profile.FollowOrthographicSize);
                return;
            }

            BoardCamera.FrameTopDown(_camera, _board.Layout, _camera.aspect);
        }

        void UpdateFollowCamera()
        {
            if (_profile.CameraMode != ArenaCameraMode.FollowWalker || IsGraphMode)
                return;
            if (_board == null || !_board.IsBuilt || _walker == null || _camera == null)
                return;

            var focus = _walker.transform.position;
            focus.y = _board.Layout.Origin.y;
            BoardCamera.SmoothFollowWalker(
                _camera,
                focus,
                _profile.FollowOrthographicSize,
                _profile.FollowSmoothing);
        }

        static bool TryReadScenarioHotkey(out TestLabScenario scenario)
        {
            scenario = default;
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                scenario = TestLabScenario.ClassicColorPath;
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                scenario = TestLabScenario.MosaicCoordinateDebug;
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                scenario = TestLabScenario.PatchworkFollowWalker;
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
                scenario = TestLabScenario.GraphNodeArena;
            else
                return false;

            return true;
        }

        void EnsureProceduralAssets()
        {
            if (_proceduralMosaic == null)
            {
                _proceduralMosaic = TestLabProceduralTextures.CreateCoordinateGridTexture(
                    Mathf.Max(3, _gridWidth),
                    Mathf.Max(3, _gridHeight));
            }

            if (_proceduralPatchwork == null || _proceduralPatchwork.Length == 0)
                _proceduralPatchwork = TestLabProceduralTextures.CreatePatchworkSet();
        }

        static ArenaVisualSettings LoadArenaVisualSettings()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ArenaVisualSettings>(
                "Assets/Game/Unity/Data/ArenaVisualSettings.asset");
            if (asset != null)
                return asset;
#endif
            return Resources.Load<ArenaVisualSettings>("ArenaVisualSettings");
        }

        static GraphLevelDefinition LoadGraphLevel()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GraphLevelDefinition>(
                "Assets/Game/Unity/Data/GraphLevels/SampleVillage.asset");
#else
            return null;
#endif
        }

        void TearDown()
        {
            _graphViewport.Clear();
            if (_board != null)
            {
                _board.Clear();
                if (_board.gameObject != null)
                    Destroy(_board.gameObject);
                _board = null;
            }

            if (_graphBoard != null)
            {
                _graphBoard.Clear();
                if (_graphBoard.gameObject != null)
                    Destroy(_graphBoard.gameObject);
                _graphBoard = null;
            }

            if (_walker != null)
            {
                Destroy(_walker.gameObject);
                _walker = null;
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.Contains("Post") || child.name.Contains("Room"))
                    Destroy(child.gameObject);
            }

            _gridRun = null;
            _graphRun = null;
        }

        void OnDestroy() => TearDown();
    }
}
