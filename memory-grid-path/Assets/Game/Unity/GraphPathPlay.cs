using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Unity.Data;
using Game.Unity.Graph;
using Game.Unity.Themes;
using Game.Unity.View;
using Nixin.Graph.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Unity
{
    /// <summary>
    /// Play mode for graph-authored levels. Separate from grid <see cref="GridPathPlay"/>.
    /// </summary>
    public sealed class GraphPathPlay : MonoBehaviour
    {
        [SerializeField] GraphLevelCatalog _catalog;
        [SerializeField] int _levelIndex;
        [SerializeField] int _pathSeed = 1;

        GraphBoardView _board;
        GraphWalkRun _run;
        WalkerView _walker;
        Camera _camera;
        readonly GraphBoardInput _input = new GraphBoardInput();
        readonly GraphNodeCircleViewFactory _factory = new GraphNodeCircleViewFactory();
        readonly GraphViewportController _viewport = new GraphViewportController();
        bool _showPath;
        string _message = string.Empty;

        void Awake()
        {
            _camera = Camera.main;
            if (_catalog == null)
                _catalog = LoadCatalog();
        }

        void Start() => StartLevel(_levelIndex);

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    _pathSeed++;
                    StartLevel(_levelIndex);
                    return;
                }

                if (keyboard.hKey.wasPressedThisFrame)
                {
                    _showPath = !_showPath;
                    GraphBoardPresenter.Refresh(_board, _run, _showPath);
                }

                if (keyboard.leftBracketKey.wasPressedThisFrame)
                    StartLevel(Mathf.Max(0, _levelIndex - 1));
                else if (keyboard.rightBracketKey.wasPressedThisFrame && _catalog != null)
                    StartLevel(Mathf.Min(_catalog.Count - 1, _levelIndex + 1));
            }

            if (_run == null || _run.IsOver || _board == null || !_board.IsBuilt)
                return;

            _viewport.UpdateInput(_camera, _board.Layout, allowInput: true);
            if (_viewport.BlocksNodePick)
                return;

            if (_run.IsAwaitingNextWalk)
            {
                _run.BeginNextWalk();
                _walker.SnapTo(_board.WorldPosition(_run.CurrentNode));
                GraphBoardPresenter.Refresh(_board, _run, _showPath);
                _message = string.Empty;
                return;
            }

            if (_walker != null && _walker.IsHopping)
                return;

            var options = PathOptionFilter.VisibleGraphOptions(_run.WalkedNodes, _run.Options());
            if (!_input.TryReadTarget(_camera, _board, _run.CurrentNode, options, out var target))
                return;

            if (!_run.IsOption(target))
                return;

            var from = _run.CurrentNode;
            var outcome = _run.Choose(target);
            _walker.HopAlong(_board.EdgeWorldPoints(from, _run.CurrentNode));
            GraphBoardPresenter.Refresh(_board, _run, _showPath);

            _message = outcome switch
            {
                WalkOutcome.LevelCompleted => "Path complete! ] next level, R new seed.",
                WalkOutcome.SessionOver => "Out of lives. R to retry.",
                WalkOutcome.WrongRevealed => $"Wrong junction — {_run.LivesLeft} lives left.",
                WalkOutcome.RunFailed => "Run failed — pick a node to restart.",
                _ => "On the path."
            };
        }

        void OnGUI()
        {
            var level = CurrentLevel();
            GUILayout.BeginArea(new Rect(12f, 12f, 420f, 120f));
            GUILayout.Label("<b>Graph Path Play</b>");
            GUILayout.Label(level != null ? level.DisplayName : "No graph levels in catalog");
            GUILayout.Label($"Level {_levelIndex + 1}/{Mathf.Max(1, _catalog?.Count ?? 0)}  |  [ ] switch  |  R reseed  |  H reveal  |  Pinch/scroll zoom, drag pan");
            if (!string.IsNullOrEmpty(_message))
                GUILayout.Label(_message);
            GUILayout.EndArea();
        }

        void StartLevel(int index)
        {
            TearDown();
            if (_catalog == null || _catalog.Count == 0)
            {
                var sample = GraphLevelDefinition.CreateSampleRuntime();
                _run = GraphBoardPresenter.CreateRun(sample, _pathSeed);
                BuildBoard(sample);
                return;
            }

            _levelIndex = Mathf.Clamp(index, 0, _catalog.Count - 1);
            var level = _catalog.Get(_levelIndex);
            _run = GraphBoardPresenter.CreateRun(level, _pathSeed);
            BuildBoard(level);
        }

        void BuildBoard(GraphLevelDefinition level)
        {
            var boardGo = new GameObject("GraphBoard");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GraphBoardView>();
            _factory.FogOfWar = true;
            _board.Build(level, _factory);
            _factory.ApplyEnvironment(_camera, _board.Layout, transform);
            _viewport.Reset(_board.Layout, _camera.aspect);
            _viewport.Apply(_camera);
            _walker = WalkerView.Create(transform, _board.WorldPosition(_run.CurrentNode), DanceFloorPalette.Start);
            GraphBoardPresenter.Refresh(_board, _run, _showPath);
            _message = "Memorize the path.";
        }

        void LateUpdate()
        {
            if (_camera == null || _board == null || !_board.IsBuilt)
                return;

            if (_viewport.IsActive)
                _viewport.Apply(_camera);
            else
                BoardCamera.FrameTopDownForBounds(
                    _camera,
                    _board.Layout.Origin,
                    _board.Layout.WorldWidth,
                    _board.Layout.WorldDepth,
                    _camera.aspect,
                    bottomAlign: true);
        }

        GraphLevelDefinition CurrentLevel()
        {
            if (_catalog == null || _catalog.Count == 0)
                return null;

            return _catalog.Get(_levelIndex);
        }

        static GraphLevelCatalog LoadCatalog()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GraphLevelCatalog>(
                "Assets/Game/Unity/Data/GraphLevelCatalog.asset");
#else
            return Resources.Load<GraphLevelCatalog>("GraphLevelCatalog");
#endif
        }

        void TearDown()
        {
            _viewport.Clear();
            ArenaEnvironment.Clear(transform);

            if (_board != null)
            {
                _board.Clear();
                Destroy(_board.gameObject);
                _board = null;
            }

            if (_walker != null)
            {
                Destroy(_walker.gameObject);
                _walker = null;
            }

            _run = null;
        }

        void OnDestroy() => TearDown();
    }
}
