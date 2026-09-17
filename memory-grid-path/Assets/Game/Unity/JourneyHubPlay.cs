using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Fue;
using Game.Core.Rules;
using Game.Core.State;
using Game.Unity.Audio;
using Game.Unity.Data;
using Game.Unity.Fue;
using Game.Unity.Graph;
using Game.Unity.Input;
using Game.Unity.Save;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.Ui;
using Game.Unity.Vfx;
using Game.Unity.View;
using Nixin.Game.Core;
using Nixin.Graph.Core;
using Nixin.Grid.Core;
using Nixin.Fue;
using Nixin.Ui;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Game.Unity
{
    /// <summary>
    /// New main play loop: three independently saved modes, home picker, and per-level visuals.
    /// Leaves <see cref="GridPathPlay"/> untouched.
    /// </summary>
    public sealed class JourneyHubPlay : MonoBehaviour
    {
        [SerializeField] JourneyCatalog _catalog;
        [SerializeField] JourneyHomeScreen _home;
        [SerializeField] JourneyLevelSelectScreen _levelSelect;
        [SerializeField] LevelDetailScreen _levelDetail;
        [SerializeField] SettingsScreen _settings;
        [SerializeField] MemoryPathPopup _popup;
        [SerializeField] GridPathHud _hud;

        IPlayerRepository<JourneyProgress> _repository;
        JourneyProgress _journey;
        GameModeId _selectedMode = GameModeId.TileArena;

        GridPathGame _gridGame;
        bool _sharedGridProgress;
        GameModeId _playingMode;
        int _playingLevel;
        GraphWalkRun _graphRun;
        GraphLevelDefinition _graphLevel;

        ITileViewFactory _theme;
        ITileEffects _effects;
        GridBoardView _board;
        GraphBoardView _graphBoard;
        WalkerView _walker;
        Camera _camera;
        ArenaVisualSettings _arenaSettings;
        Texture2D _proceduralMosaic;
        readonly BoardInput _gridInput = new BoardInput();
        readonly GraphBoardInput _graphInput = new GraphBoardInput();
        readonly GraphNodeCircleViewFactory _graphFactory = new GraphNodeCircleViewFactory();
        readonly GraphViewportController _graphViewport = new GraphViewportController();
        bool _holdingReveal;
        Coroutine _revealHold;
        bool _completionOverview;
        bool _zoomingOverview;
        bool _suppressScoutIntroHighlights;
        bool _followReturnPan;

        TutorialSession _tutorial;
        readonly MemoryPathTutorialChrome _tutorialChrome = new MemoryPathTutorialChrome();
        readonly OpeningCardChrome _openingCard = new OpeningCardChrome();
        bool _openingCardBlocking;
        ArenaIntroPlayer _arenaIntro;
        bool _arenaIntroPlaying;
        LevelOneFueSession _levelOneFue;
        readonly MemoryPathLevelOneChrome _levelOneChrome = new MemoryPathLevelOneChrome();
        GraphLevelOneFueSession _graphLevelOneFue;
        MemoryPathStepCallout _stepCallout;
        readonly MemoryPathGraphLevelOneChrome _graphLevelOneChrome = new MemoryPathGraphLevelOneChrome();
        ScoutScanFueSession _scoutScanFue;
        readonly MemoryPathScoutScanChrome _scoutScanChrome = new MemoryPathScoutScanChrome();
        IFueSeenStore _fueStore;
        FueDirector _fueDirector;
        bool _tutorialReplay;
        bool _radarPlaying;
        bool _ownsGraphLevel;

        const float RevealHoldSeconds = 1.25f;
        const float ScoutIntroHoldSeconds = 0.55f;
        const float ScoutIntroZoomSeconds = 1.45f;
        const float ScoutIntroChoicePreviewSeconds = 0.45f;

        void Awake()
        {
            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();
            _repository = new PlayerPrefsJourneyRepository();
            _journey = _repository.Load();
            _catalog = _catalog != null ? _catalog : LoadCatalog();
            if (_catalog == null)
                _catalog = ScriptableObject.CreateInstance<JourneyCatalog>();
            _camera = Camera.main;
            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = MemoryPathPalette.PlayBackground;
            }
            _hud = GridPathHud.Resolve(_hud, transform);
            _hud.SetVisible(false);
            _hud.BindExit(ShowPause);
            _effects = DanceFloorEffects.Create(transform);
            _selectedMode = _journey.LastSelectedMode;
            MemoryPathAudio.Ensure();
            JourneyUi.Ensure(_home, _levelSelect, _levelDetail, _settings, _popup);
            JourneyUi.Ensure().UnhandledBack = OnUnhandledBack;
            EnsureFue();
            if (TutorialSkipGate.ShouldAutoPlay(
                    _fueStore != null && _fueStore.HasSeen(TutorialSpec.LessonId),
                    PlayerSettingsStore.TutorialSkipCount,
                    replayRequested: false))
                StartTutorial(replay: false);
            else
                ShowHome();
        }

        void OnUnhandledBack()
        {
            var playing = _hud != null && _hud.IsShown && IsPlayActive;
            if (DeviceBackPolicy.Resolve(false, playing) == DeviceBackAction.PausePlay)
                ShowPause();
            else
                AppShell.Minimize();
        }

        bool IsPlayActive =>
            _arenaIntroPlaying
            || _holdingReveal
            || (_tutorial != null)
            || (_gridGame != null && _gridGame.IsPlaying)
            || (_graphRun != null && !_graphRun.IsOver);

        static JourneyCatalog LoadCatalog()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<JourneyCatalog>(
                "Assets/Game/Unity/Data/JourneyCatalog.asset");
            if (asset != null)
                return asset;
#endif
            return Resources.Load<JourneyCatalog>("JourneyCatalog");
        }

        void LateUpdate()
        {
            try
            {
                UpdateOverlayFocusState();

                if (_zoomingOverview || _followReturnPan)
                    return;

                if (_completionOverview)
                {
                    ApplyOverviewFraming();
                    return;
                }

                if (_arenaSettings != null && _arenaSettings.CameraMode == ArenaCameraMode.FollowWalker)
                {
                    UpdateFollowCamera();
                    return;
                }

                if (_graphBoard != null && _graphBoard.IsBuilt && _graphViewport.IsActive)
                {
                    _graphViewport.Apply(_camera);
                    return;
                }

                if (_graphBoard != null && _graphBoard.IsBuilt)
                    ApplyCameraFraming();
                else if (_board != null && _board.IsBuilt)
                    ApplyCameraFraming();
            }
            finally
            {
                CameraFeel.Apply(_camera);
            }
        }

        void UpdateFollowCamera()
        {
            if (_arenaSettings == null || _arenaSettings.CameraMode != ArenaCameraMode.FollowWalker)
                return;
            if (_walker == null || _camera == null)
                return;

            Vector3 focus;
            if (_board != null && _board.IsBuilt)
            {
                focus = _walker.transform.position;
                focus.y = _board.Layout.Origin.y;
            }
            else if (_graphBoard != null && _graphBoard.IsBuilt)
            {
                focus = _walker.transform.position;
                focus.y = _graphBoard.Layout.Origin.y;
            }
            else
            {
                return;
            }

            BoardCamera.SmoothFollowWalker(
                _camera,
                focus,
                _arenaSettings.FollowOrthographicSize,
                _arenaSettings.FollowSmoothing,
                FollowHeadingYaw(),
                yawSmoothing: 0f);
        }

        bool UsesScoutRotation => ScoutRotationMove.UsesRotation(_arenaSettings);

        float FollowHeadingYaw() =>
            UsesScoutRotation && _walker != null ? _walker.YawDegrees : 0f;

        void ApplyWalkerFacing(bool instant)
        {
            if (_walker == null)
                return;

            _walker.FaceTravel = UsesScoutRotation;
            if (!UsesScoutRotation)
                return;
            _walker.YawDegreesPerSecond = ScoutRotationMove.PlayYawDegreesPerSecond;
            if (TryFirstPathDelta(out var delta))
                _walker.FaceToward(delta, instant);
        }

        bool TryFirstPathDelta(out Vector3 delta)
        {
            delta = Vector3.forward;
            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            {
                var cells = _gridGame.Run.Path.Cells;
                if (cells == null || cells.Count < 2)
                    return false;
                delta = _board.WorldPosition(cells[1]) - _board.WorldPosition(cells[0]);
                delta.y = 0f;
                return delta.sqrMagnitude > 0.0001f;
            }

            if (_graphBoard != null && _graphBoard.IsBuilt && _graphRun != null)
            {
                var nodes = _graphRun.Path.Nodes;
                if (nodes == null || nodes.Count < 2)
                    return false;
                var points = _graphBoard.EdgeWorldPoints(nodes[0], nodes[1]);
                if (points != null && points.Length >= 2)
                    delta = points[1] - points[0];
                else
                    delta = _graphBoard.WorldPosition(nodes[1]) - _graphBoard.WorldPosition(nodes[0]);
                delta.y = 0f;
                return delta.sqrMagnitude > 0.0001f;
            }

            return false;
        }

        void Update()
        {
            if (_fueDirector != null)
                _fueDirector.Tick(Time.unscaledDeltaTime);

            if (_tutorial != null)
            {
                TickTutorial();
                return;
            }

            if (_graphRun != null)
            {
                TickGraph();
                return;
            }

            if (_gridGame == null || !_gridGame.IsPlaying)
                return;

            var run = _gridGame.Run;
            var current = run != null ? run.CurrentCell : default;
            var visibleOptions = PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options());
            var hasTarget = _gridInput.TryReadTarget(
                _camera,
                _board,
                current,
                visibleOptions,
                PlayerSettingsStore.PathDrag,
                IsGridPlayOption,
                out var target,
                UsesScoutRotation ? ScoutRotationMove.HeadingYaw(_camera) : 0f);

            if (_hud == null || !_hud.IsShown || InputLocked)
                return;
            if (UiNavigator.Current != null && UiNavigator.Current.IsBlocking)
                return;
            if (!hasTarget || !_gridGame.Run.IsOption(target) || !PathOptionFilter.Contains(visibleOptions, target))
                return;

            StepGrid(target);
        }

        bool IsGridPlayOption(GridCoord cell) =>
            _gridGame != null && _gridGame.Run != null && _gridGame.Run.IsOption(cell);

        bool InputLocked =>
            _arenaIntroPlaying
            || _openingCardBlocking
            || _holdingReveal
            || _radarPlaying
            || (_walker != null && _walker.IsHopping);

        void EnsureFue()
        {
            if (_fueStore != null)
                return;
            _fueStore = new PlayerPrefsFueSeenStore("memory-grid-path.fue.");
            _fueDirector = new FueDirector(_fueStore);
            _fueDirector.Register(new FueCue
            {
                Id = TutorialSpec.LessonId,
                TriggerKind = FueTriggerKind.Lesson,
                ShowOnce = true,
                Ensure = true,
                Replayable = true,
                Compulsory = true,
                Priority = 100,
                Eligible = () => true,
                WantsToShow = () => _tutorial != null,
                Satisfied = () => _tutorial != null && _tutorial.IsCompleted
            });
            _fueDirector.Register(new FueCue
            {
                Id = LevelOneFueSpec.LessonId,
                TriggerKind = FueTriggerKind.Lesson,
                ShowOnce = false,
                Ensure = true,
                Replayable = false,
                Compulsory = false,
                Priority = 50,
                Eligible = () => _gridGame != null
                    && _playingMode == GameModeId.TileArena
                    && OpeningCardSpec.ShouldShow(_playingLevel),
                WantsToShow = () => _levelOneFue != null && _levelOneFue.IsActive,
                Satisfied = () => _levelOneFue != null && _levelOneFue.IsComplete
            });
            _fueDirector.Register(new FueCue
            {
                Id = GraphLevelOneFueSpec.LessonId,
                TriggerKind = FueTriggerKind.Lesson,
                ShowOnce = true,
                Ensure = true,
                Replayable = false,
                Compulsory = false,
                Priority = 50,
                Eligible = () => _graphRun != null && _playingMode == GameModeId.GraphArena && _playingLevel == GraphLevelOneFueSpec.LevelNumber,
                WantsToShow = () => _graphLevelOneFue != null && _graphLevelOneFue.IsActive,
                Satisfied = () => _graphLevelOneFue != null && _graphLevelOneFue.IsComplete
            });
            _fueDirector.Register(new FueCue
            {
                Id = ScoutScanFueSpec.LessonId,
                TriggerKind = FueTriggerKind.Lesson,
                ShowOnce = true,
                Ensure = true,
                Replayable = false,
                Compulsory = true,
                Priority = 55,
                Eligible = () => _playingMode == GameModeId.ScoutArena && _playingLevel == ScoutScanFueSpec.LevelNumber,
                WantsToShow = () => _scoutScanFue != null && _scoutScanFue.IsActive,
                Satisfied = () => _scoutScanFue != null && _scoutScanFue.IsComplete
            });
        }

        void StartTutorial(bool replay)
        {
            EnsureFue();
            StopRevealHold();
            TearDownViews();
            _gridGame = null;
            _graphRun = null;
            _tutorialReplay = replay;
            _tutorial = new TutorialSession();
            if (replay)
                _fueDirector.RequestReplay(TutorialSpec.LessonId);
            _fueDirector.BeginScope();

            JourneyUi.HideScreens();
            _hud.SetVisible(true);
            _tutorialChrome.Ensure(_hud);
            _tutorialChrome.OnSkip = SkipTutorial;
            _tutorialChrome.OnIntroDismissed = DismissTutorialIntro;
            _tutorialChrome.OnReadyDismissed = FinishTutorialSuccess;

            _arenaSettings = ArenaVisualSettings.CreateClassicOverride();
            _theme = ArenaThemeResolver.Resolve(_arenaSettings);
            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GridBoardView>();
            _board.Build(TutorialSpec.Size, _theme, _arenaSettings.TileSize, _arenaSettings.TileGap);
            _theme.ApplyEnvironment(_camera, _board.Layout, transform);
            _walker = WalkerView.Create(transform, _board.WorldPosition(TutorialSpec.Start), DanceFloorPalette.Start);
            ApplyCameraFraming();
            MemoryPathAudio.Play(MemoryPathCue.GameStart);
            RefreshTutorial();
        }

        void StopTutorial()
        {
            _tutorialChrome.Hide();
            _openingCard.Hide();
            _openingCardBlocking = false;
            _tutorial = null;
        }

        void TickTutorial()
        {
            if (_tutorial == null || _hud == null || !_hud.IsShown || InputLocked)
                return;
            if (UiNavigator.Current != null && UiNavigator.Current.IsBlocking)
                return;
            if (!_tutorial.IsPlaying)
                return;

            var current = _tutorial.CurrentCell;
            var visibleOptions = _tutorial.VisibleOptions();
            var hasTarget = _gridInput.TryReadTarget(
                _camera,
                _board,
                current,
                visibleOptions,
                PlayerSettingsStore.PathDrag,
                _tutorial.IsOption,
                out var target);
            if (!hasTarget || !_tutorial.IsOption(target) || !PathOptionFilter.Contains(visibleOptions, target))
                return;

            StepTutorial(target);
        }

        void StepTutorial(GridCoord target)
        {
            _tutorialChrome.ClearTileHighlights(_board);
            var outcome = _tutorial.Choose(target);
            var destination = _board.WorldPosition(_tutorial.CurrentCell);
            if (outcome != WalkOutcome.Advanced && outcome != WalkOutcome.LevelCompleted)
                _gridInput.StopPathDrag();

            switch (outcome)
            {
                case WalkOutcome.Advanced:
                    _effects.PlayCorrect(_board.TileAt(target));
                    _walker.HopTo(destination);
                    RefreshTutorial();
                    break;
                case WalkOutcome.LevelCompleted:
                    _effects.PlayLevelCompleted();
                    _walker.HopTo(destination);
                    RefreshTutorial();
                    break;
            }
        }

        void FinishTutorialSuccess()
        {
            _fueStore?.MarkSeen(TutorialSpec.LessonId);
            ShowHome();
        }

        void DismissTutorialIntro()
        {
            if (_tutorial == null || _tutorial.Beat != TutorialBeat.Intro)
                return;
            _tutorial.DismissIntro();
            RefreshTutorial();
            StopRevealHold();
            _revealHold = StartCoroutine(RadarThenPlay(
                _tutorial.Run.Path.Cells,
                TutorialSpec.RadarSweepSeconds,
                TutorialSpec.RadarHoldSeconds,
                RefreshTutorial,
                () =>
                {
                    _tutorial?.BeginWalk();
                    RefreshTutorial();
                }));
        }

        void SkipTutorial()
        {
            var skips = PlayerSettingsStore.RecordTutorialSkip();
            if (TutorialSkipGate.ShouldMarkSeen(skips))
                _fueStore?.MarkSeen(TutorialSpec.LessonId);
            ShowHome();
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.TutorialSkipped(null),
                replacePopups: true);
        }

        void RefreshTutorial()
        {
            if (_tutorial == null || _hud == null)
                return;

            _hud.SetStats("T", 0, _tutorial.Step, _tutorial.TotalSteps);
            _hud.SetHealth(
                _tutorial.HudRunNumber,
                _tutorial.LivesLeft,
                _tutorial.LivesPerRun,
                _tutorial.RunsPerSession);
            _hud.SetMessage(_tutorial.Beat switch
            {
                TutorialBeat.Watching => TutorialCopy.Watch,
                TutorialBeat.Completed => TutorialCopy.Ready,
                _ => TutorialCopy.Prompt
            });
            if (_radarPlaying)
            {
                _tutorialChrome.ClearTileHighlights(_board);
                return;
            }

            _tutorialChrome.Present(_tutorial, _board, _camera);
        }

        void PollGraphInput(out bool hasTarget, out GraphNodeId target)
        {
            hasTarget = false;
            target = default;
            if (_graphRun.IsOver || _graphBoard == null || !_graphBoard.IsBuilt)
                return;

            if (_graphRun.IsAwaitingNextWalk)
                return;

            if (_walker != null && _walker.IsHopping)
                return;

            var options = PathOptionFilter.VisibleGraphOptions(_graphRun.WalkedNodes, _graphRun.Options());
            hasTarget = _graphInput.TryReadTarget(_camera, _graphBoard, _graphRun.CurrentNode, options, out target);
        }

        void TickGraph()
        {
            var graphInputAllowed = _hud != null
                && _hud.IsShown
                && !InputLocked
                && (UiNavigator.Current == null || !UiNavigator.Current.IsBlocking);
            if (_graphViewport.IsActive)
            {
                if (_graphBoard != null && _graphBoard.IsBuilt && _camera != null)
                    _graphViewport.RefreshLimits(_graphBoard.Layout, _camera.aspect, TileHudViewportInset());
                _graphViewport.UpdateInput(_camera, _graphBoard != null ? _graphBoard.Layout : null, graphInputAllowed);
            }

            RefreshGraphLevelOneFue();

            if (_hud == null || !_hud.IsShown || InputLocked)
                return;
            if (UiNavigator.Current != null && UiNavigator.Current.IsBlocking)
                return;
            if (_graphViewport.BlocksNodePick)
                return;

            PollGraphInput(out var hasTarget, out var target);
            if (hasTarget && _graphRun.IsOption(target))
                StepGraph(target);
        }

        void StartSelectedLevel()
        {
            if (!EnsureModeUnlocked(_selectedMode, out var reason))
            {
                ShowNotice(reason, "OK", ShowHome);
                return;
            }

            StartLevel(_selectedMode, _journey.For(_selectedMode).HighestUnlockedLevel);
        }

        void StartLevel(GameModeId mode, int levelNumber)
        {
            StopRevealHold();
            TearDownViews();
            _gridGame = null;
            _graphRun = null;
            _sharedGridProgress = false;

            if (!EnsureModeUnlocked(mode, out var reason))
            {
                ShowNotice(reason, "OK", ShowHome);
                return;
            }

            var modeDef = _catalog.Mode(mode);
            if (ArenaLevelCount(mode) < 1)
            {
                ShowNotice("No levels in this arena yet.", "OK", ShowHome);
                return;
            }

            var progress = _journey.For(mode);
            if (!progress.IsUnlocked(levelNumber))
            {
                ShowNotice("That level is still locked.", "OK", ShowHome);
                return;
            }

            _playingMode = mode;
            _playingLevel = levelNumber;
            _journey.RememberPlayed(mode, levelNumber);
            _repository.Save(_journey);

            var entry = _catalog.PlayableEntry(mode, levelNumber);
            JourneyUi.HideScreens();
            _hud.SetVisible(true);

            if ((entry.IsGraph || mode == GameModeId.GraphArena) && mode != GameModeId.ScoutArena)
            {
                StartGraphLevel(modeDef, entry);
                return;
            }

            StartGridLevel(mode, modeDef, entry);
        }

        void StartGridLevel(GameModeId mode, JourneyModeDefinition modeDef, JourneyLevelEntry entry)
        {
            _sharedGridProgress = mode == GameModeId.TileArena;
            var config = GameConfig.Default;
            if (_sharedGridProgress)
            {
                _gridGame = new GridPathGame(_catalog.CreateGridCatalog(mode), config, _journey.For(mode));
            }
            else
            {
                _gridGame = new GridPathGame(
                    _catalog.CreateSingleGridCatalog(mode, _playingLevel, entry),
                    config,
                    new PlayerProgress());
            }

            try
            {
                _gridGame.SelectLevel(_sharedGridProgress ? _playingLevel : 1);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNotice("Could not open that level.", "TRY AGAIN", () => StartLevel(mode, _playingLevel));
                return;
            }

            RefreshHud();
            BeginGridPlay();
        }

        void BeginGridPlay()
        {
            try
            {
                _gridGame.StartPlay(Environment.TickCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNotice("Could not build that level.", "TRY AGAIN", () => StartLevel(_playingMode, _playingLevel));
                return;
            }

            BuildGridBoard();
            _suppressScoutIntroHighlights = ShouldPlayScoutIntro();
            _levelOneFue = LevelOneFueSession.TryStart(
                _playingMode,
                _playingLevel,
                _fueStore != null && _fueStore.HasSeen(LevelOneFueSpec.LessonIdFor(_playingLevel)));
            _hud.SetVisible(true);
            _hud.SetMessage("On the path. Keep going!");
            MemoryPathAudio.Play(MemoryPathCue.GameStart);
            _arenaIntroPlaying = true;
            RefreshBoard();
            RefreshHud();
            PlayArenaIntro(() => ContinueAfterArenaIntro("On the path. Keep going!"));
        }

        void StartGraphLevel(JourneyModeDefinition modeDef, JourneyLevelEntry entry)
        {
            _gridGame = null;
            _sharedGridProgress = false;
            _graphLevel = GraphLevelDefinition.CreatePlayable(
                _playingLevel,
                _catalog.PlayableGraphSource(1)
                    ?? _catalog.PlayableGraphSource(_playingLevel)
                    ?? entry.GraphLevel);
            _ownsGraphLevel = true;
            _arenaSettings = ArenaVisualSettings.CreateClassicOverride(
                modeDef.CameraMode,
                ScoutFollowSize(modeDef, entry),
                modeDef.ResolvedFollowSmoothing,
                modeDef.LocksOrthographicSize(entry),
                modeDef.ScoutMoveMode);
            _graphRun = GraphBoardPresenter.CreateRun(_graphLevel, Environment.TickCount);

            var boardGo = new GameObject("GraphBoard");
            boardGo.transform.SetParent(transform, false);
            _graphBoard = boardGo.AddComponent<GraphBoardView>();
            _graphFactory.OceanBackdrop = modeDef.ModeId == GameModeId.ScoutArena;
            _graphFactory.FogOfWar = modeDef.ModeId == GameModeId.GraphArena;
            _graphBoard.Build(_graphLevel, _graphFactory);
            _graphFactory.ApplyEnvironment(_camera, _graphBoard.Layout, transform);
            if (_playingMode == GameModeId.GraphArena)
                _graphViewport.Reset(_graphBoard.Layout, _camera.aspect, TileHudViewportInset());
            else
                _graphViewport.Clear();
            _walker = WalkerView.Create(
                transform,
                _graphBoard.WorldPosition(_graphRun.CurrentNode),
                DanceFloorPalette.Start,
                modeDef.WalkerScaleFor(entry));
            ApplyWalkerFacing(instant: true);
            _suppressScoutIntroHighlights = ShouldPlayScoutIntro();
            _graphLevelOneFue = GraphLevelOneFueSession.TryStart(
                _playingMode,
                _playingLevel,
                _fueStore != null && _fueStore.HasSeen(GraphLevelOneFueSpec.LessonId));
            RefreshGraphBoard();
            if (_graphViewport.IsActive)
                _graphViewport.Apply(_camera);
            else
                ApplyCameraFraming();
            _hud.SetMessage("Tap the next node on the path.");
            MemoryPathAudio.Play(MemoryPathCue.GameStart);
            _arenaIntroPlaying = true;
            RefreshHud();
            PlayArenaIntro(() => ContinueAfterArenaIntro("Tap the next node on the path."));
        }

        void BuildGridBoard()
        {
            TearDownViews();
            var modeDef = _catalog.Mode(_playingMode);
            var entry = _catalog.PlayableEntry(_playingMode, _playingLevel);
            EnsureProceduralMosaic(entry);
            _arenaSettings = JourneyVisualResolver.Resolve(
                entry,
                modeDef.CameraMode,
                _proceduralMosaic,
                ScoutFollowSize(modeDef, entry),
                modeDef.ResolvedFollowSmoothing,
                modeDef.LocksOrthographicSize(entry),
                modeDef.ScoutMoveMode);
            _theme = ArenaThemeResolver.Resolve(_arenaSettings);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GridBoardView>();
            _board.Build(_gridGame.CurrentLevel.Size, _theme, _arenaSettings.TileSize, _arenaSettings.TileGap);
            _theme.ApplyEnvironment(_camera, _board.Layout, transform);
            _graphViewport.Clear();
            var walkerScale = _playingMode == GameModeId.ScoutArena
                ? ScoutRotationMove.WalkerScale
                : modeDef.WalkerScaleFor(entry);
            _walker = WalkerView.Create(
                transform,
                _board.WorldPosition(_gridGame.Run.Path.Start),
                DanceFloorPalette.Start,
                walkerScale);
            ApplyWalkerFacing(instant: true);
            ApplyCameraFraming();
        }

        void EnsureProceduralMosaic(JourneyLevelEntry entry)
        {
            if (entry.VisualType != ArenaVisualType.MosaicImage || entry.MosaicTexture != null)
                return;
            if (_proceduralMosaic != null)
                return;

            _proceduralMosaic = MosaicPhotoLibrary.Pick(_playingLevel);
            if (_proceduralMosaic != null)
                return;

            var spec = entry.GridSpec;
            _proceduralMosaic = TestLab.TestLabProceduralTextures.CreateCoordinateGridTexture(
                Math.Max(1, spec.Width),
                Math.Max(1, spec.Height));
        }

        void ApplyCameraFraming()
        {
            if (_camera == null)
                return;

            if (_graphBoard != null && _graphBoard.IsBuilt && _graphViewport.IsActive)
            {
                _graphViewport.Apply(_camera);
                return;
            }

            if (_arenaSettings != null
                && (_arenaSettings.CameraMode == ArenaCameraMode.FollowWalker
                    || _arenaSettings.LockOrthographicSize)
                && _walker != null)
            {
                var focus = _walker.transform.position;
                if (_board != null && _board.IsBuilt)
                    focus.y = _board.Layout.Origin.y;
                else if (_graphBoard != null && _graphBoard.IsBuilt)
                    focus.y = _graphBoard.Layout.Origin.y;

                if (_arenaSettings.CameraMode == ArenaCameraMode.FollowWalker)
                    BoardCamera.FrameFollow(_camera, focus, _arenaSettings.FollowOrthographicSize, FollowHeadingYaw());
                else
                {
                    Vector3 origin;
                    if (_board != null && _board.IsBuilt)
                        origin = _board.Layout.Origin;
                    else if (_graphBoard != null && _graphBoard.IsBuilt)
                        origin = _graphBoard.Layout.Origin;
                    else
                        origin = focus;
                    BoardCamera.FrameFollow(_camera, origin, _arenaSettings.FollowOrthographicSize);
                }

                return;
            }

            if (_board != null && _board.IsBuilt)
                BoardCamera.FrameTopDown(_camera, _board.Layout, _camera.aspect, TileHudViewportInset());
            else if (_graphBoard != null && _graphBoard.IsBuilt)
                BoardCamera.FrameTopDownForBounds(
                    _camera,
                    _graphBoard.Layout.Origin,
                    _graphBoard.Layout.WorldWidth,
                    _graphBoard.Layout.WorldDepth,
                    _camera.aspect,
                    bottomAlign: false,
                    topViewportInset: _playingMode == GameModeId.GraphArena ? TileHudViewportInset() : 0f);
        }

        float ScoutFollowSize(JourneyModeDefinition modeDef, JourneyLevelEntry entry)
        {
            if (_playingMode != GameModeId.ScoutArena)
                return modeDef.FollowSizeFor(entry);
            if (_gridGame?.CurrentLevel != null)
                return ScoutRotationMove.FollowOrthographicSizeFor(_gridGame.CurrentLevel.Size);
            return ScoutRotationMove.FollowSizeTwo;
        }

        float TileHudViewportInset()
        {
            if (_hud == null || !_hud.IsShown)
                return 0f;
            return _hud.TopChromeViewportHeight(_camera);
        }

        void StepGrid(GridCoord target)
        {
            _levelOneChrome.ClearTileHighlights(_board);
            var run = _gridGame.Run;
            var wrongFrom = _board.WorldPosition(run.Path.Cells[run.Step]);
            var wrongTo = _board.WorldPosition(target);
            var wrongTile = _board.TileAt(target);
            var outcome = _gridGame.Choose(target);
            var destination = _board.WorldPosition(run.CurrentCell);
            if (outcome != WalkOutcome.Advanced && outcome != WalkOutcome.LevelCompleted)
                _gridInput.StopPathDrag();

            switch (outcome)
            {
                case WalkOutcome.Advanced:
                    _effects.PlayCorrect(_board.TileAt(target), run.Step);
                    _walker.HopTo(destination);
                    _levelOneFue?.OnCorrectStep();
                    CompleteLevelOneFueIfNeeded();
                    if (run.WasFailedInPriorWalk(target))
                        ShowStepCallout(MemoryPathStepCueCopy.RandomRecovered(), recover: true);
                    _hud.SetMessage("On the path. Keep going!");
                    RefreshBoard();
                    RefreshHud();
                    break;
                case WalkOutcome.WrongRevealed:
                    _levelOneFue?.OnWrongStep();
                    _hud.SetMessage($"Off the path — {run.LivesLeft} heart slices left.");
                    BeginGridMistake(
                        wrongTile,
                        wrongFrom,
                        wrongTo,
                        null,
                        forgotPriorWalk: run.LastRevealed.HasValue
                            && run.WasCoveredInPriorWalk(run.LastRevealed.Value));
                    break;
                case WalkOutcome.RunFailed:
                    BeginWalkFailRestart();
                    break;
                case WalkOutcome.LevelCompleted:
                    _levelOneFue?.OnCorrectStep();
                    CompleteLevelOneFueIfNeeded();
                    if (run.LastRevealed.HasValue)
                    {
                        BeginGridMistake(wrongTile, wrongFrom, wrongTo, () =>
                        {
                            _effects.PlayLevelCompleted();
                            BeginCompletion(run.Score);
                        });
                    }
                    else
                    {
                        _effects.PlayLevelCompleted();
                        _walker.HopTo(destination);
                        RefreshBoard();
                        BeginCompletion(run.Score);
                    }

                    break;
                case WalkOutcome.SessionOver:
                    BeginLevelFail(run.Score);
                    break;
            }

            if (!_holdingReveal)
                RefreshHud();
        }

        void BeginWalkFailRestart()
        {
            StopRevealHold();
            _holdingReveal = true;
            _effects.PlayRunFailed();
            RefreshHud();
            _revealHold = StartCoroutine(CrashRestartGrid());
        }

        void BeginLevelFail(int score)
        {
            StopRevealHold();
            _holdingReveal = true;
            _effects.PlaySessionFailed();
            _hud.SetMessage("Out of lives.");
            RefreshHud();
            FinishSession(false, score);
        }

        void BeginGridMistake(
            ITileView wrong,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Action afterPainted,
            bool runFailed = false,
            bool sessionFailed = false,
            bool forgotPriorWalk = false)
        {
            StopRevealHold();
            _holdingReveal = true;
            var run = _gridGame.Run;
            if (sessionFailed)
                _effects.PlaySessionFailed();
            else if (runFailed)
                _effects.PlayRunFailed();
            if (forgotPriorWalk)
                ShowStepCallout(MemoryPathStepCueCopy.RandomForgot(), recover: false);

            var revealed = _board.TileAt(run.CurrentCell);
            _revealHold = StartCoroutine(GridMissRoutine(wrong, revealed, wrongFrom, wrongTo, afterPainted, magnetPull: !runFailed));
        }

        IEnumerator GridMissRoutine(
            ITileView wrong,
            ITileView revealed,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Action afterPainted,
            bool magnetPull)
        {
            var run = _gridGame.Run;
            var destination = _board.WorldPosition(run.CurrentCell);
            var tileSize = _board.Layout.TileSize;
            yield return MissBeat.Play(
                this,
                _walker,
                _board.Overlay,
                _camera,
                _hud,
                wrong,
                revealed,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                destination,
                run.RunNumber,
                tileSize,
                intense: false,
                onSpark: RefreshHud,
                afterFlash: () =>
                {
                    RefreshBoard();
                    RefreshHud();
                },
                after: () =>
                {
                    _holdingReveal = afterPainted != null;
                    _revealHold = null;
                    afterPainted?.Invoke();
                    if (afterPainted == null)
                        _holdingReveal = false;
                },
                magnetPull: magnetPull,
                walkWrongPath: _playingMode != GameModeId.ScoutArena);
        }

        IEnumerator CrashRestartGrid()
        {
            _holdingReveal = true;
            var panHome = ShouldPlayScoutIntro();
            _followReturnPan = panHome;
            var run = _gridGame.Run;
            var walked = CopyCells(run.WalkedCells);
            yield return WalkedTrailFade.CrashOut(
                _walker,
                _board,
                _board != null ? _board.Overlay : null,
                walked,
                WalkedTrailFade.FadeSeconds);
            RestoreWalkedTiles(walked);
            _gridGame.BeginNextWalk();
            run = _gridGame.Run;
            var origin = _board.WorldPosition(run.Path.Start);
            if (panHome)
                yield return PanArenaTo(origin);
            _walker.SnapTo(origin, restoreAlpha: false);
            ApplyWalkerFacing(instant: true);
            if (!panHome || UsesScoutRotation)
                ApplyCameraFraming();
            if (_walker != null)
                yield return _walker.Rematerialize(WalkerView.CrashInSeconds);
            _hud.SetMessage("On the path. Keep going!");
            RefreshBoard();
            RefreshHud();
            _followReturnPan = false;
            _holdingReveal = false;
            _revealHold = null;
        }

        static GridCoord[] CopyCells(IReadOnlyList<GridCoord> cells)
        {
            if (cells == null || cells.Count == 0)
                return Array.Empty<GridCoord>();
            var copy = new GridCoord[cells.Count];
            for (var i = 0; i < cells.Count; i++)
                copy[i] = cells[i];
            return copy;
        }

        void RestoreWalkedTiles(IReadOnlyList<GridCoord> walked)
        {
            if (_board == null || walked == null)
                return;
            for (var i = 0; i < walked.Count; i++)
            {
                var tile = _board.TileAt(walked[i]);
                tile?.SetState(TileVisualState.Idle);
            }
        }

        IEnumerator RadarThenPlay(
            IReadOnlyList<GridCoord> path,
            float sweepSeconds,
            float holdSeconds,
            Action refresh,
            Action after)
        {
            _holdingReveal = true;
            _radarPlaying = true;
            refresh?.Invoke();
            var radar = RadarPathPreview.Ensure(transform);
            ShowRadarMemorizeCue();
            yield return radar.Play(_board, path, sweepSeconds, holdSeconds);
            HideScoutScanPlayer();
            HideRadarMemorizeCue();
            _radarPlaying = false;
            refresh?.Invoke();
            RefreshHud();
            _holdingReveal = false;
            _revealHold = null;
            after?.Invoke();
        }

        IEnumerator RadarThenPlayAfterIntro(Action after)
        {
            _holdingReveal = true;
            yield return PlayRadarSweep();
            RefreshHud();
            _holdingReveal = false;
            _revealHold = null;
            after?.Invoke();
        }

        IEnumerator PlayRadarSweep()
        {
            _radarPlaying = true;
            if (_graphRun != null)
                RefreshGraphBoard();
            else
                RefreshBoard();

            var radar = RadarPathPreview.Ensure(transform);
            var scan = TryShowTileOrGraphScanPlayer();
            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            {
                var seconds = _gridGame.CurrentLevel != null
                    ? PathPreviewSeconds.For(_gridGame.CurrentLevel)
                    : PathPreviewSeconds.ForLevel(_playingLevel);
                var hold = _gridGame.CurrentLevel != null
                    ? PathPreviewSeconds.HoldFor(_gridGame.CurrentLevel)
                    : PathPreviewSeconds.HoldFor(_playingLevel);
                yield return radar.Play(_board, _gridGame.Run.Path.Cells, seconds, hold, scan);
            }
            else if (_graphBoard != null && _graphBoard.IsBuilt && _graphRun != null)
            {
                var origin = _graphBoard.Layout.Origin + new Vector3(0f, GridPathOverlay.Lift + 0.08f, 0f);
                var worldPath = _graphBoard.RouteWorldPoints(_graphRun.Path.Nodes, GridPathOverlay.Lift);
                var width = _graphBoard.Layout.WorldWidth;
                var depth = _graphBoard.Layout.WorldDepth;
                if (_playingMode == GameModeId.GraphArena && _graphLevel != null)
                {
                    var seconds = _graphLevel.PreviewSeconds;
                    var hold = GraphLevelLadder.For(_playingLevel).HoldSeconds;
                    if (_graphLevel.PreviewKind == PathPreviewKind.CameraFlash)
                        yield return radar.PlayFlash(worldPath, origin, width, depth, seconds, scan);
                    else
                        yield return radar.Play(worldPath, origin, width, depth, seconds, hold, scan);
                }
                else
                {
                    yield return radar.Play(
                        worldPath,
                        origin,
                        width,
                        depth,
                        PathPreviewSeconds.ForLevel(_playingLevel),
                        PathPreviewSeconds.Hold,
                        scan);
                }
            }

            HideScoutScanPlayer();
            HideRadarMemorizeCue();
            _radarPlaying = false;
            if (_graphRun != null)
                RefreshGraphBoard();
            else
                RefreshBoard();
        }

        bool HasRadarPath() =>
            (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            || (_graphBoard != null && _graphBoard.IsBuilt && _graphRun != null);

        void ShowStepCallout(string caption, bool recover)
        {
            if (_hud == null || _walker == null)
                return;

            _stepCallout = MemoryPathStepCallout.Ensure(_hud.OverlayRoot);
            if (_stepCallout == null)
                return;

            _stepCallout.ShowAwayFromAvatar(
                _camera,
                _walker.transform.position,
                caption,
                recover);
        }

        void StepGraph(Nixin.Graph.Core.GraphNodeId target)
        {
            var from = _graphRun.CurrentNode;
            var wrongFrom = _graphBoard.WorldPosition(from);
            var wrongTo = _graphBoard.WorldPosition(target);
            var wrongPath = _graphBoard.EdgeWorldPoints(from, target);
            var outcome = _graphRun.Choose(target);
            var hopPath = _graphBoard.EdgeWorldPoints(from, _graphRun.CurrentNode);

            switch (outcome)
            {
                case WalkOutcome.Advanced:
                    _graphLevelOneFue?.OnCorrectStep(reachedGoal: false);
                    _walker.HopAlong(hopPath);
                    RefreshGraphBoard();
                    _effects.PlayCorrect(null);
                    _hud.SetMessage("On the path. Keep going!");
                    break;
                case WalkOutcome.WrongRevealed:
                    _graphLevelOneFue?.OnWrongStep();
                    _hud.SetMessage($"Wrong junction — {_graphRun.LivesLeft} heart slices left.");
                    BeginGraphMistake(from, target, wrongFrom, wrongTo, wrongPath, hopPath, null);
                    break;
                case WalkOutcome.RunFailed:
                    BeginGraphWalkFailRestart();
                    break;
                case WalkOutcome.LevelCompleted:
                    _graphLevelOneFue?.OnCorrectStep(reachedGoal: true);
                    if (_graphRun.LastRevealed.HasValue)
                    {
                        BeginGraphMistake(from, target, wrongFrom, wrongTo, wrongPath, hopPath, () =>
                        {
                            _effects.PlayLevelCompleted();
                            BeginCompletion(_graphRun.Score);
                        });
                    }
                    else
                    {
                        _walker.HopAlong(hopPath);
                        RefreshGraphBoard();
                        _effects.PlayLevelCompleted();
                        BeginCompletion(_graphRun.Score);
                    }

                    break;
                case WalkOutcome.SessionOver:
                    BeginLevelFail(_graphRun.Score);
                    break;
            }

            if (!_holdingReveal)
                RefreshHud();
        }

        void BeginGraphWalkFailRestart()
        {
            StopRevealHold();
            _holdingReveal = true;
            _effects.PlayRunFailed();
            RefreshHud();
            _revealHold = StartCoroutine(CrashRestartGraph());
        }

        void BeginGraphMistake(
            Nixin.Graph.Core.GraphNodeId from,
            Nixin.Graph.Core.GraphNodeId wrong,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            IReadOnlyList<Vector3> wrongPath,
            IReadOnlyList<Vector3> hopPath,
            Action afterPainted,
            bool runFailed = false,
            bool sessionFailed = false)
        {
            StopRevealHold();
            _holdingReveal = true;
            if (sessionFailed)
                _effects.PlaySessionFailed();
            else if (runFailed)
                _effects.PlayRunFailed();

            _revealHold = StartCoroutine(GraphMissRoutine(
                from,
                wrong,
                wrongFrom,
                wrongTo,
                wrongPath,
                hopPath,
                afterPainted,
                magnetPull: !runFailed));
        }

        IEnumerator GraphMissRoutine(
            Nixin.Graph.Core.GraphNodeId from,
            Nixin.Graph.Core.GraphNodeId wrong,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            IReadOnlyList<Vector3> wrongPath,
            IReadOnlyList<Vector3> hopPath,
            Action afterPainted,
            bool magnetPull)
        {
            var destination = _graphBoard.WorldPosition(_graphRun.CurrentNode);
            var revealed = _graphBoard.NodeAt(_graphRun.CurrentNode) as GraphNodeCircleView;
            yield return MissBeat.Play(
                this,
                _walker,
                null,
                _camera,
                _hud,
                null,
                null,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                destination,
                _graphRun.RunNumber,
                0.44f,
                intense: false,
                onSpark: RefreshHud,
                afterFlash: () =>
                {
                    RefreshGraphBoard();
                    RefreshHud();
                },
                after: () =>
                {
                    _holdingReveal = afterPainted != null;
                    _revealHold = null;
                    afterPainted?.Invoke();
                    if (afterPainted == null)
                        _holdingReveal = false;
                },
                magnetPull: magnetPull,
                approachPath: wrongPath,
                magnetPath: hopPath,
                paintWrong: () =>
                {
                    _graphBoard.NodeAt(wrong)?.SetVisible(true);
                    _graphBoard.SetState(wrong, GraphNodeVisualState.Wrong);
                    _graphBoard.SetEdgeVisual(from, wrong, GraphEdgeVisualState.Wrong);
                },
                flashRevealed: () => revealed?.Flash(GraphNodeVisualState.Revealed, 0.45f),
                walkWrongPath: _playingMode != GameModeId.ScoutArena);
        }

        void BeginCompletion(int score)
        {
            RecordSession(true, score);
            _hud.SetMessage("Splendid Memory!");
            StopRevealHold();
            _revealHold = StartCoroutine(CelebrateThenPopup(score));
        }

        void FinishSession(bool completed, int score)
        {
            RecordSession(completed, score);
            _hud.SetMessage(completed ? "Splendid Memory!" : "Out of lives!");

            if (completed)
            {
                ShowComplete(score, NextPlayable(), _playingLevel);
                return;
            }

            ShowGameOver(_playingLevel, "The path faded away.");
        }

        void RecordSession(bool completed, int score)
        {
            if (!_sharedGridProgress)
            {
                _journey.For(_playingMode).RecordResult(
                    _playingLevel,
                    Math.Max(0, score),
                    completed,
                    _catalog.Mode(_playingMode).Count,
                    mistakes: SessionMistakes());
            }

            _journey.RememberPlayed(_playingMode, _playingLevel);
            _repository.Save(_journey);
        }

        int SessionMistakes()
        {
            if (_graphRun != null)
                return _graphRun.MistakesMade;
            if (_gridGame != null && _gridGame.Run != null)
                return _gridGame.Run.MistakesMade;
            return 0;
        }

        IEnumerator CelebrateThenPopup(int score)
        {
            _holdingReveal = true;
            while (_walker != null && _walker.IsHopping)
                yield return null;

            var zoomOut = TryOverviewFrame(out var origin, out var targetSize);
            var targetPos = new Vector3(origin.x, BoardCamera.Height, origin.z);
            var alreadyOverview = zoomOut && BoardCamera.NearlyMatchesOverview(_camera, targetPos, targetSize);
            if (alreadyOverview)
            {
                _completionOverview = true;
                ApplyOverviewFraming();
                if (GridPathOverlay.AlreadyOverviewPopupDelay > 0f)
                    yield return new WaitForSeconds(GridPathOverlay.AlreadyOverviewPopupDelay);
                _holdingReveal = false;
                _revealHold = null;
                ShowComplete(score, NextPlayable(), _playingLevel);
                yield break;
            }

            var hold = GridPathOverlay.CompletionHoldSeconds;
            var zoomSeconds = zoomOut ? GridPathOverlay.CelebrateZoomSeconds : 0f;
            var duration = Mathf.Max(hold, zoomSeconds);

            var startPos = _camera != null ? _camera.transform.position : origin;
            var startSize = _camera != null ? _camera.orthographicSize : targetSize;

            if (zoomOut && zoomSeconds > 0.01f)
            {
                _zoomingOverview = true;
                if (_graphViewport.IsActive && _graphBoard != null && _graphBoard.IsBuilt)
                    _graphViewport.Reset(
                        _graphBoard.Layout,
                        _camera != null ? _camera.aspect : 1f,
                        TileHudViewportInset());
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (_zoomingOverview && _camera != null)
                {
                    var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, zoomSeconds)));
                    _camera.orthographic = true;
                    _camera.transform.SetPositionAndRotation(
                        Vector3.Lerp(startPos, targetPos, t),
                        BoardCamera.TopDownRotation);
                    _camera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
                    _camera.nearClipPlane = 0.1f;
                    _camera.farClipPlane = BoardCamera.FarClip;
                    if (elapsed >= zoomSeconds)
                    {
                        _zoomingOverview = false;
                        _completionOverview = true;
                        ApplyOverviewFraming();
                    }
                }

                yield return null;
            }

            _zoomingOverview = false;
            if (zoomOut)
            {
                _completionOverview = true;
                ApplyOverviewFraming();
            }

            _holdingReveal = false;
            _revealHold = null;
            ShowComplete(score, NextPlayable(), _playingLevel);
        }

        bool TryOverviewFrame(out Vector3 origin, out float size)
        {
            origin = Vector3.zero;
            size = 1f;
            if (_camera == null)
                return false;

            if (_board != null && _board.IsBuilt)
            {
                origin = _board.Layout.Origin;
                var inset = TileHudViewportInset();
                size = BoardCamera.OrthographicSize(_board.Layout, _camera.aspect, inset);
                origin.z += BoardCamera.TopHudFocusOffset(inset, size);
                return true;
            }

            if (_graphBoard != null && _graphBoard.IsBuilt)
            {
                origin = _graphBoard.Layout.Origin;
                var inset = _playingMode == GameModeId.GraphArena ? TileHudViewportInset() : 0f;
                size = BoardCamera.ContainOrthographicSize(
                    _graphBoard.Layout.WorldWidth,
                    _graphBoard.Layout.WorldDepth,
                    _camera.aspect,
                    inset) * BoardCamera.Padding;
                origin.z += BoardCamera.TopHudFocusOffset(inset, size);
                return true;
            }

            return false;
        }

        void ApplyOverviewFraming()
        {
            if (_camera == null)
                return;

            if (_board != null && _board.IsBuilt)
            {
                BoardCamera.FrameTopDown(_camera, _board.Layout, _camera.aspect, TileHudViewportInset());
                return;
            }

            if (_graphBoard != null && _graphBoard.IsBuilt)
            {
                BoardCamera.FrameTopDownForBounds(
                    _camera,
                    _graphBoard.Layout.Origin,
                    _graphBoard.Layout.WorldWidth,
                    _graphBoard.Layout.WorldDepth,
                    _camera.aspect,
                    bottomAlign: false,
                    topViewportInset: _playingMode == GameModeId.GraphArena ? TileHudViewportInset() : 0f);
            }
        }

        void UpdateOverlayFocusState()
        {
            var overlay = CurrentOverlay();
            if (overlay == null)
                return;

            var focused = _playingMode == GameModeId.ScoutArena
                && _arenaSettings != null
                && _arenaSettings.CameraMode == ArenaCameraMode.FollowWalker
                && !_completionOverview
                && !_zoomingOverview
                && !_holdingReveal;
            overlay.SetFocusedStyle(focused);
        }

        GridPathOverlay CurrentOverlay()
        {
            if (_board != null && _board.IsBuilt)
                return _board.Overlay;
            if (_graphBoard != null && _graphBoard.IsBuilt)
                return _graphBoard.Overlay;
            return null;
        }

        void MaybeStartScoutIntro(string settledMessage)
        {
            if (!ShouldPlayScoutIntro() || !TryOverviewFrame(out var origin, out var overviewSize))
            {
                _suppressScoutIntroHighlights = false;
                return;
            }

            StopRevealHold();
            _revealHold = StartCoroutine(ScoutRotationIntroRoutine(origin, overviewSize, settledMessage));
        }

        void PlayArenaIntro(Action after)
        {
            if (ShouldPlayScoutIntro())
            {
                _arenaIntroPlaying = false;
                after?.Invoke();
                return;
            }

            if (_arenaIntro == null)
                _arenaIntro = ArenaIntroPlayer.Ensure(transform);
            _arenaIntro.Stop();
            _arenaIntroPlaying = true;

            void Done()
            {
                _arenaIntroPlaying = false;
                if (_board != null && _board.IsBuilt)
                    ApplyCameraFraming();
                RefreshHud();
                after?.Invoke();
            }

            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            {
                _arenaIntro.PlayGrid(_board, _walker, _gridGame.Run.Path.Start, Done);
                return;
            }

            if (_graphBoard != null && _graphBoard.IsBuilt)
            {
                PrepareGraphIntroVisuals();
                _arenaIntro.PlayBounce(
                    _graphBoard.transform,
                    _walker,
                    _graphBoard.Layout != null ? _graphBoard.Layout.Origin : _graphBoard.transform.position,
                    Done);
                return;
            }

            Done();
        }

        void PrepareGraphIntroVisuals()
        {
            if (_graphBoard == null || _graphRun == null)
                return;

            _graphBoard.SetAllNodesVisible(false);
            _graphBoard.SetAllEdgesVisible(false);
            if (_graphBoard.Overlay != null)
                _graphBoard.Overlay.gameObject.SetActive(false);
        }

        void StopArenaIntro()
        {
            _arenaIntro?.Stop();
            _arenaIntroPlaying = false;
        }

        void ReplayOpeningCard()
        {
            var playing = _tutorial == null
                && _hud != null
                && _hud.IsShown
                && IsPlayActive;
            JourneyUi.HideScreens();
            if (playing)
            {
                ShowOpeningCard(force: true);
                return;
            }

            ShowHome();
            ShowOpeningCard(force: true, overlayHome: true);
        }

        void ContinueAfterArenaIntro(string settledMessage)
        {
            if (ShouldShowOpeningCardNow()
                && ShowOpeningCard(force: false, afterDismissed: () => ContinueAfterOpeningCard(settledMessage)))
                return;

            ContinueAfterOpeningCard(settledMessage);
        }

        void ContinueAfterOpeningCard(string settledMessage)
        {
            if (ShouldPlayScoutIntro())
            {
                MaybeStartScoutIntro(settledMessage);
                return;
            }

            if (HasRadarPath())
            {
                _revealHold = StartCoroutine(RadarThenPlayAfterIntro(null));
                return;
            }

            if (_graphBoard != null && _graphBoard.IsBuilt)
                RefreshGraphBoard();
        }

        bool ShouldShowOpeningCardNow()
        {
            if (_graphLevelOneFue != null && _graphLevelOneFue.IsActive)
                return false;
            if (!OpeningCardSpec.ShouldShow(_playingLevel))
                return false;
            return _fueStore == null || !_fueStore.HasSeen(OpeningCardSpec.LessonIdFor(_playingLevel));
        }

        bool ShowOpeningCard(bool force, bool overlayHome = false, Action afterDismissed = null)
        {
            if (!force && !OpeningCardSpec.ShouldShow(_playingLevel))
                return false;
            if (!force
                && _fueStore != null
                && _fueStore.HasSeen(OpeningCardSpec.LessonIdFor(_playingLevel)))
                return false;

            Transform parent = null;
            if (overlayHome)
            {
                var nav = UiNavigator.Current;
                parent = nav != null ? nav.transform : null;
            }

            if (parent == null && _hud != null)
                parent = _hud.OverlayRoot;
            if (parent == null)
                return false;

            _openingCard.Ensure(parent);
            _openingCardBlocking = true;
            _levelOneChrome.Hide();
            var level = _playingLevel;
            var markSeen = !overlayHome;
            _openingCard.Show(() =>
            {
                _openingCardBlocking = false;
                if (markSeen && OpeningCardSpec.ShouldShow(level))
                    _fueStore?.MarkSeen(OpeningCardSpec.LessonIdFor(level));
                afterDismissed?.Invoke();
            });
            return true;
        }

        bool ShouldPlayScoutIntro()
        {
            return _playingMode == GameModeId.ScoutArena
                && _arenaSettings != null
                && _arenaSettings.CameraMode == ArenaCameraMode.FollowWalker
                && _walker != null
                && _camera != null;
        }

        IEnumerator ScoutIntroRoutine(Vector3 overviewOrigin, float overviewSize, string settledMessage)
        {
            _holdingReveal = true;
            _completionOverview = false;
            _zoomingOverview = true;

            var target = _walker.transform.position;
            if (_board != null && _board.IsBuilt)
                target.y = _board.Layout.Origin.y;
            else if (_graphBoard != null && _graphBoard.IsBuilt)
                target.y = _graphBoard.Layout.Origin.y;

            var startPosition = new Vector3(overviewOrigin.x, BoardCamera.Height, overviewOrigin.z);
            var endPosition = new Vector3(target.x, BoardCamera.Height, target.z);

            _camera.orthographic = true;
            _camera.transform.SetPositionAndRotation(startPosition, BoardCamera.TopDownRotation);
            _camera.orthographicSize = overviewSize;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = BoardCamera.FarClip;

            yield return new WaitForSeconds(ScoutIntroHoldSeconds);
            yield return PlayRadarSweep();

            var elapsed = 0f;
            while (elapsed < ScoutIntroZoomSeconds)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ScoutIntroZoomSeconds));
                _camera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, endPosition, t),
                    BoardCamera.TopDownRotation);
                _camera.orthographicSize = Mathf.Lerp(overviewSize, _arenaSettings.FollowOrthographicSize, t);
                yield return null;
            }

            _zoomingOverview = false;
            _suppressScoutIntroHighlights = false;
            ApplyCameraFraming();
            _hud.SetMessage(settledMessage);
            RefreshBoard();
            RefreshGraphBoard();
            yield return new WaitForSeconds(ScoutIntroChoicePreviewSeconds);
            _holdingReveal = false;
            _revealHold = null;
        }

        IEnumerator ScoutRotationIntroRoutine(Vector3 overviewOrigin, float overviewSize, string settledMessage)
        {
            _holdingReveal = true;
            _completionOverview = false;
            _zoomingOverview = false;
            CurrentOverlay()?.ResetVisuals();
            if (_graphBoard != null && _graphBoard.IsBuilt)
            {
                _graphBoard.SetAllNodesVisible(false);
                _graphBoard.SetAllEdgesVisible(false);
                if (_graphBoard.Overlay != null)
                    _graphBoard.Overlay.gameObject.SetActive(false);
            }

            ApplyWalkerFacing(instant: true);
            if (_walker != null)
                _walker.YawDegreesPerSecond = ScoutRotationMove.TourYawDegreesPerSecond;
            var start = ScoutWalkerOrigin();
            BoardCamera.FrameFollow(
                _camera,
                start,
                _arenaSettings.FollowOrthographicSize,
                FollowHeadingYaw());
            var scan = ShowScoutScanPlayer();
            BeginScoutScanFue(scan);
            yield return WalkScoutTourPath(scan);
            HideScoutScanFue();
            HideScoutScanPlayer();
            yield return new WaitForSeconds(ScoutRotationMove.DestinationHoldSeconds);

            _zoomingOverview = true;
            yield return ScoutRotationMove.LerpFollow(
                _camera,
                _walker != null ? _walker.transform.position : start,
                _arenaSettings.FollowOrthographicSize,
                FollowHeadingYaw(),
                overviewOrigin,
                overviewSize,
                0f,
                ScoutRotationMove.ZoomSeconds);
            yield return new WaitForSeconds(ScoutRotationMove.OverviewHoldSeconds);

            HideScoutTourPathLine();
            if (_walker != null)
            {
                _walker.MotionPaused = false;
                _walker.SnapTo(start, restoreAlpha: true);
            }
            ApplyWalkerFacing(instant: true);
            yield return ScoutRotationMove.LerpFollow(
                _camera,
                overviewOrigin,
                overviewSize,
                0f,
                start,
                _arenaSettings.FollowOrthographicSize,
                FollowHeadingYaw(),
                ScoutRotationMove.ZoomSeconds);

            _zoomingOverview = false;
            _suppressScoutIntroHighlights = false;
            ApplyCameraFraming();
            _hud.SetMessage(settledMessage);
            RefreshBoard();
            RefreshGraphBoard();
            yield return new WaitForSeconds(ScoutIntroChoicePreviewSeconds);
            _holdingReveal = false;
            _revealHold = null;
        }

        Vector3 ScoutWalkerOrigin()
        {
            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
                return _board.WorldPosition(_gridGame.Run.Path.Start);
            if (_graphBoard != null && _graphBoard.IsBuilt && _graphRun != null)
                return _graphBoard.WorldPosition(_graphRun.Path.Start);
            return _walker != null ? _walker.transform.position : Vector3.zero;
        }

        IEnumerator WalkScoutTourPath(ScoutScanPlayer player)
        {
            if (_walker == null)
                yield break;

            var hops = ScoutTourHopSeconds();
            var pause = ScoutRotationMove.TourPauseSecondsFor(_playingLevel);
            var total = ScoutRotationMove.TourTotalSeconds(hops, pause);
            player?.SetProgress(0f, total);
            yield return WaitScoutScanFue(player);
            if (hops.Length == 0)
                yield break;

            for (var i = 0; i < hops.Length; i++)
            {
                StartScoutTourHop(i);
                while (_walker != null && _walker.IsHopping)
                {
                    SyncScoutTourPause(player);
                    TickScoutScanFue(player);
                    player?.SetProgress(
                        ScoutRotationMove.TourCoveredSeconds(hops, i, _walker.TravelNormalized, pause),
                        total);
                    yield return null;
                }

                player?.SetProgress(ScoutRotationMove.TourCoveredSeconds(hops, i + 1, 0f, pause), total);
                if (i >= hops.Length - 1)
                    continue;

                var pauseElapsed = 0f;
                var pauseBase = ScoutRotationMove.TourCoveredSeconds(hops, i + 1, 0f, pause);
                while (pauseElapsed < pause)
                {
                    SyncScoutTourPause(player);
                    TickScoutScanFue(player);
                    if (player == null || !player.HeldPaused)
                        pauseElapsed += Time.deltaTime;
                    player?.SetProgress(pauseBase + Mathf.Min(pauseElapsed, pause), total);
                    yield return null;
                }
            }

            SyncScoutTourPause(null);
            player?.SetProgress(total, total);
        }

        float[] ScoutTourHopSeconds()
        {
            var hopSeconds = ScoutRotationMove.TourHopSecondsFor(_playingLevel);
            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            {
                var cells = _gridGame.Run.Path.Cells;
                if (cells == null || cells.Count < 2)
                    return System.Array.Empty<float>();
                var hops = new float[cells.Count - 1];
                for (var i = 1; i < cells.Count; i++)
                {
                    hops[i - 1] = WalkerView.SecondsForDistance(
                        _board.WorldPosition(cells[i - 1]),
                        _board.WorldPosition(cells[i]),
                        hopSeconds);
                }

                return hops;
            }

            if (_graphBoard == null || !_graphBoard.IsBuilt || _graphRun == null)
                return System.Array.Empty<float>();

            var nodes = _graphRun.Path.Nodes;
            if (nodes == null || nodes.Count < 2)
                return System.Array.Empty<float>();
            var graphHops = new float[nodes.Count - 1];
            for (var i = 1; i < nodes.Count; i++)
            {
                var edge = _graphBoard.EdgeWorldPoints(nodes[i - 1], nodes[i]);
                graphHops[i - 1] = WalkerView.SecondsForPath(edge, hopSeconds);
            }

            return graphHops;
        }

        void StartScoutTourHop(int hopIndex)
        {
            if (_walker == null)
                return;

            if (_board != null && _board.IsBuilt && _gridGame?.Run != null)
            {
                var cells = _gridGame.Run.Path.Cells;
                var next = _board.WorldPosition(cells[hopIndex + 1]);
                _walker.HopTo(
                    next,
                    WalkerView.SecondsForDistance(
                        _walker.transform.position,
                        next,
                        ScoutRotationMove.TourHopSecondsFor(_playingLevel)));
                return;
            }

            if (_graphBoard == null || !_graphBoard.IsBuilt || _graphRun == null)
                return;

            var nodes = _graphRun.Path.Nodes;
            var edge = _graphBoard.EdgeWorldPoints(nodes[hopIndex], nodes[hopIndex + 1]);
            _walker.HopAlong(edge, WalkerView.SecondsForPath(edge, ScoutRotationMove.TourHopSecondsFor(_playingLevel)));
        }

        void SyncScoutTourPause(ScoutScanPlayer player)
        {
            if (_walker == null)
                return;
            _walker.MotionPaused = player != null && player.HeldPaused;
        }

        void HideScoutTourPathLine()
        {
            CurrentOverlay()?.ResetVisuals();
            if (_graphBoard != null && _graphBoard.Overlay != null)
                _graphBoard.Overlay.gameObject.SetActive(false);
        }

        int NextPlayable()
        {
            var progress = _journey.For(_playingMode);
            var next = _playingLevel + 1;
            if (next <= ArenaLevelCount(_playingMode) && progress.IsUnlocked(next))
                return next;
            return _playingLevel;
        }

        void StartNextGridWalk()
        {
            _gridGame.BeginNextWalk();
            var run = _gridGame.Run;
            _walker.SnapTo(_board.WorldPosition(run.Path.Start));
            ApplyWalkerFacing(instant: true);
            ApplyCameraFraming();
            _hud.SetMessage("On the path. Keep going!");
            RefreshBoard();
            RefreshHud();
        }

        IEnumerator CrashRestartGraph()
        {
            _holdingReveal = true;
            var panHome = ShouldPlayScoutIntro();
            _followReturnPan = panHome;
            var walked = CopyNodes(_graphRun.WalkedNodes);
            yield return WalkedTrailFade.CrashOutGraph(
                _walker,
                _graphBoard,
                walked,
                WalkedTrailFade.FadeSeconds);
            _graphRun.BeginNextWalk();
            var origin = _graphBoard != null
                ? _graphBoard.WorldPosition(_graphRun.CurrentNode)
                : Vector3.zero;

            if (_playingMode == GameModeId.GraphArena && _graphBoard != null && _graphBoard.IsBuilt)
            {
                if (_walker != null)
                    _walker.SnapTo(origin, restoreAlpha: false);
                var aspect = _camera != null ? _camera.aspect : 1f;
                _graphViewport.Reset(_graphBoard.Layout, aspect, TileHudViewportInset());
                ApplyCameraFraming();
                _graphViewport.Apply(_camera);
            }
            else if (panHome)
            {
                yield return PanArenaTo(origin);
                if (_walker != null)
                    _walker.SnapTo(origin, restoreAlpha: false);
                ApplyWalkerFacing(instant: true);
                if (UsesScoutRotation)
                    ApplyCameraFraming();
            }
            else
            {
                if (_walker != null)
                    _walker.SnapTo(origin, restoreAlpha: false);
                ApplyWalkerFacing(instant: true);
                ApplyCameraFraming();
            }

            if (_walker != null)
                yield return _walker.Rematerialize(WalkerView.CrashInSeconds);
            _hud.SetMessage("On the path. Keep going!");
            RefreshGraphBoard();
            RefreshHud();
            _followReturnPan = false;
            _holdingReveal = false;
            _revealHold = null;
        }

        static Nixin.Graph.Core.GraphNodeId[] CopyNodes(IReadOnlyList<Nixin.Graph.Core.GraphNodeId> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return Array.Empty<Nixin.Graph.Core.GraphNodeId>();
            var copy = new Nixin.Graph.Core.GraphNodeId[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
                copy[i] = nodes[i];
            return copy;
        }

        IEnumerator PanArenaTo(Vector3 boardPoint)
        {
            if (_camera == null)
                yield break;

            var to = new Vector3(boardPoint.x, BoardCamera.Height, boardPoint.z);
            var seconds = BoardStepFeedback.ReturnPanSeconds(_camera.transform.position, to);
            yield return BoardStepFeedback.PanLinear(_camera, to, seconds);
        }

        void ShowHome()
        {
            StopRevealHold();
            StopTutorial();
            TearDownViews();
            _gridGame = null;
            _graphRun = null;
            ReleasePlayableGraphLevel();
            _sharedGridProgress = false;
            _hud.SetVisible(false);
            if (!ModeExists(_selectedMode) || !GameModeUnlock.IsUnlocked(
                    _selectedMode,
                    _journey,
                    _catalog.UnlockConfig,
                    Math.Max(1, ArenaLevelCount(GameModeId.TileArena)),
                    Math.Max(1, ArenaLevelCount(GameModeId.GraphArena))))
            {
                _selectedMode = GameModeId.TileArena;
            }

            _journey.SelectMode(_selectedMode);
            _repository.Save(_journey);
            var mode = _catalog.Mode(_selectedMode);
            var progress = _journey.For(_selectedMode);
            JourneyUi.Ensure().Open<JourneyHomeScreen, JourneyHomePayload>(new JourneyHomePayload
            {
                Level = progress.HighestUnlockedLevel,
                StarsEarned = LevelAccess.StarsEarned(progress),
                StarsMax = LevelAccess.StarsPossible(Math.Max(1, ArenaLevelCount(_selectedMode))),
                CareerScore = progress.CareerScore,
                SoundOn = PlayerSettingsStore.SoundEffects,
                Modes = BuildModeIcons(),
                SelectedModeTitle = mode.DisplayName,
                OnSelectMode = SelectMode,
                OnPlay = StartSelectedLevel,
                OnLevels = ShowLevelSelect,
                OnSettings = ShowSettings,
                OnVolume = ToggleSound
            });
        }

        JourneyModeIconInfo[] BuildModeIcons()
        {
            return new[]
            {
                Icon(GameModeId.TileArena, MemoryPathPalette.Teal),
                Icon(GameModeId.GraphArena, MemoryPathPalette.HomeGraph),
                Icon(GameModeId.ScoutArena, MemoryPathPalette.Mascot)
            };
        }

        void ToggleSound()
        {
            PlayerSettingsStore.SoundEffects = !PlayerSettingsStore.SoundEffects;
        }

        JourneyModeIconInfo Icon(GameModeId id, Color tint)
        {
            var mode = _catalog.Mode(id);
            return new JourneyModeIconInfo
            {
                Id = id,
                // GameModeId.ScoutArena ? "Cozy Path" :
                Title = mode.DisplayName,
                Icon = ResolveModeIcon(mode),
                Unlocked = GameModeUnlock.IsUnlocked(
                    id,
                    _journey,
                    _catalog.UnlockConfig,
                    Math.Max(1, ArenaLevelCount(GameModeId.TileArena)),
                    Math.Max(1, ArenaLevelCount(GameModeId.GraphArena))),
                Selected = _selectedMode == id,
                Tint = tint
            };
        }

        static Sprite ResolveModeIcon(JourneyModeDefinition mode)
        {
            if (mode == null)
                return null;
            if (mode.Icon != null)
                return mode.Icon;

            var levels = mode.Levels;
            if (levels == null)
                return null;

            for (var i = 0; i < levels.Length; i++)
            {
                var entry = levels[i];
                if (entry == null)
                    continue;
                if (entry.Thumbnail != null)
                    return entry.Thumbnail;
                if (entry.ThumbnailSource is Texture2D texture)
                {
                    return Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }
            }

            return null;
        }

        void SelectMode(GameModeId id)
        {
            if (!GameModeUnlock.IsUnlocked(
                    id,
                    _journey,
                    _catalog.UnlockConfig,
                    Math.Max(1, ArenaLevelCount(GameModeId.TileArena)),
                    Math.Max(1, ArenaLevelCount(GameModeId.GraphArena))))
            {
                ShowNotice(GameModeUnlock.LockReason(id, _catalog.UnlockConfig), "OK", ShowHome);
                return;
            }

            _selectedMode = id;
            _journey.SelectMode(id);
            _repository.Save(_journey);
            ShowHome();
        }

        void ShowLevelSelect()
        {
            StopRevealHold();
            TearDownViews();
            if (_hud != null)
                _hud.SetVisible(false);

            if (!EnsureModeUnlocked(_selectedMode, out var reason))
            {
                ShowNotice(reason, "OK", ShowHome);
                return;
            }

            var mode = _catalog.Mode(_selectedMode);
            var progress = _journey.For(_selectedMode);
            var count = ArenaLevelCount(_selectedMode);
            var tiles = new JourneyLevelTileInfo[count];
            for (var i = 0; i < tiles.Length; i++)
            {
                var number = i + 1;
                var lane = LevelAccess.Lane(progress, number);
                var entry = _catalog.PlayableEntry(_selectedMode, number);
                tiles[i] = new JourneyLevelTileInfo
                {
                    Number = number,
                    Lane = lane,
                    Stars = LevelAccess.StarsOn(progress, number),
                    Thumbnail = entry.ThumbnailSource,
                    Swatch = SwatchFor(entry)
                };
            }

            JourneyUi.Ensure().Open<JourneyLevelSelectScreen, JourneyLevelSelectPayload>(new JourneyLevelSelectPayload
            {
                Title = mode.DisplayName,
                StarsEarned = LevelAccess.StarsEarned(progress),
                StarsMax = LevelAccess.StarsPossible(Math.Max(1, count)),
                Tiles = tiles,
                OnBack = ShowHome,
                OnPick = ShowLevelDetail
            });
        }

        static Color SwatchFor(JourneyLevelEntry entry)
        {
            if (entry.IsGraph)
                return MemoryPathPalette.Resume;
            switch (entry.VisualType)
            {
                case ArenaVisualType.MosaicImage:
                    return MemoryPathPalette.Sunset;
                case ArenaVisualType.PatchworkTiles:
                    return MemoryPathPalette.Mascot;
                default:
                    return MemoryPathPalette.Teal;
            }
        }

        void ShowLevelDetail(int levelNumber)
        {
            var entry = _catalog.PlayableEntry(_selectedMode, levelNumber);
            var progress = _journey.For(_selectedMode);
            var gridLabel = _selectedMode == GameModeId.GraphArena || entry.IsGraph
                ? (levelNumber == 1 ? "Follow the finger" : "Graph path")
                : GridSizeLabel(_selectedMode, levelNumber, entry);
            var steps = _selectedMode == GameModeId.GraphArena || entry.IsGraph
                ? "Tap the next node"
                : "Walk the hidden path";

            JourneyUi.Ensure().Open<LevelDetailScreen, LevelDetailPayload>(new LevelDetailPayload
            {
                Level = levelNumber,
                Headline = LevelTitles.Headline(levelNumber),
                GridLabel = gridLabel,
                StepsLabel = steps,
                BestScore = progress.BestScoreFor(levelNumber),
                Thumbnail = entry.ThumbnailSource,
                OnBack = ShowLevelSelect,
                OnStart = () => StartLevel(_selectedMode, levelNumber)
            });
        }

        static string GridSizeLabel(GameModeId mode, int levelNumber, JourneyLevelEntry entry)
        {
            var spec = JourneyCatalog.PlayableGridSpec(mode, levelNumber - 1, entry.GridSpec);
            return spec.Width + " x " + spec.Height + " Grid";
        }

        void ShowSettings()
        {
            JourneyUi.Ensure().Open<SettingsScreen, SettingsPayload>(new SettingsPayload
            {
                SoundEffects = PlayerSettingsStore.SoundEffects,
                Music = PlayerSettingsStore.Music,
                Haptics = PlayerSettingsStore.Haptics,
                PathDrag = PlayerSettingsStore.PathDrag,
                ThemeId = PlayerSettingsStore.ThemeId,
                OnBack = ShowHome,
                OnSoundEffects = value => PlayerSettingsStore.SoundEffects = value,
                OnMusic = value => PlayerSettingsStore.Music = value,
                OnHaptics = value => PlayerSettingsStore.Haptics = value,
                OnPathDrag = value => PlayerSettingsStore.PathDrag = value,
                OnTheme = value => PlayerSettingsStore.ThemeId = value,
                OnReplayTutorial = () => StartTutorial(replay: true),
                OnReplayOpening = ReplayOpeningCard
            });
        }

        void ShowNotice(string body, string actionLabel, Action onAction)
        {
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Notice("Not yet", body, actionLabel, onAction),
                replacePopups: true);
        }

        void ShowComplete(int score, int nextLevel, int currentLevel)
        {
            var canAdvance = nextLevel > currentLevel;
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Complete(
                    score,
                    canAdvance,
                    currentLevel,
                    canAdvance ? (Action)(() => StartLevel(_playingMode, nextLevel)) : null,
                    () => StartLevel(_playingMode, currentLevel),
                    ShowHome,
                    LevelAccess.StarsFromMistakes(SessionMistakes())),
                replacePopups: true);
        }

        void ShowGameOver(int level, string body)
        {
            _ = body;
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.GameOver(level, ShowHome, () => StartLevel(_playingMode, level)),
                replacePopups: true);
        }

        void ShowPause()
        {
            if (_hud == null || !_hud.IsShown)
                return;

            if (_tutorial != null)
            {
                JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                    MemoryPathPopups.Pause(
                        null,
                        () => StartTutorial(_tutorialReplay),
                        ShowHome,
                        ConfirmQuitToMenu),
                    replacePopups: true);
                return;
            }

            _selectedMode = _playingMode;
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Pause(null, () => StartLevel(_playingMode, _playingLevel), ShowLevelSelect, ConfirmQuitToMenu),
                replacePopups: true);
        }

        void ConfirmQuitToMenu()
        {
            // Bypass exit confirmation for now; keep MemoryPathPopups.Exit in place.
            ShowHome();
        }

        int ArenaLevelCount(GameModeId id) =>
            JourneyCatalog.PlayableLevelCount(_catalog, id);

        void ReleasePlayableGraphLevel()
        {
            if (_graphLevel != null && _ownsGraphLevel)
            {
                if (Application.isPlaying)
                    Destroy(_graphLevel);
                else
                    DestroyImmediate(_graphLevel);
            }

            _graphLevel = null;
            _ownsGraphLevel = false;
        }

        bool EnsureModeUnlocked(GameModeId mode, out string reason)
        {
            var unlocked = GameModeUnlock.IsUnlocked(
                mode,
                _journey,
                _catalog.UnlockConfig,
                Math.Max(1, ArenaLevelCount(GameModeId.TileArena)),
                Math.Max(1, ArenaLevelCount(GameModeId.GraphArena)));
            reason = unlocked ? string.Empty : GameModeUnlock.LockReason(mode, _catalog.UnlockConfig);
            return unlocked;
        }

        bool ModeExists(GameModeId id) => _catalog != null && _catalog.Mode(id) != null;

        bool IsScoutGridPlay =>
            _playingMode == GameModeId.ScoutArena
            && _board != null
            && _board.IsBuilt
            && _gridGame != null
            && _gridGame.Run != null;

        void RefreshBoard()
        {
            var run = _gridGame?.Run;
            var visibleOptions = run == null
                ? null
                : _radarPlaying || (_suppressScoutIntroHighlights && IsScoutGridPlay)
                    ? System.Array.Empty<GridCoord>()
                    : PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options());
            GridBoardPresenter.Refresh(
                _board,
                run,
                visibleOptions: visibleOptions,
                showChoicePaths: !_radarPlaying
                    && !(_suppressScoutIntroHighlights && IsScoutGridPlay)
                    && (_levelOneFue == null || !_levelOneFue.IsActive),
                highlightOrigin: !IsScoutGridPlay,
                trailWidthScale: IsScoutGridPlay ? ScoutRotationMove.TrailWidthScale : 1f);
            RefreshLevelOneFue(run);
        }

        void RefreshGraphBoard()
        {
            if (_graphBoard == null || !_graphBoard.IsBuilt || _graphRun == null)
                return;

            var hideChoices = _radarPlaying
                || (_suppressScoutIntroHighlights && _playingMode == GameModeId.ScoutArena);
            var visibleOptions = hideChoices
                ? System.Array.Empty<Nixin.Graph.Core.GraphNodeId>()
                : PathOptionFilter.VisibleGraphOptions(_graphRun.WalkedNodes, _graphRun.Options());
            GraphBoardPresenter.Refresh(_graphBoard, _graphRun, false, visibleOptions, showChoicePaths: !hideChoices);
            if (!_radarPlaying)
                return;

            _graphBoard.SetAllNodesVisible(false);
            _graphBoard.SetAllEdgesVisible(false);
            if (_graphBoard.Overlay != null)
            {
                _graphBoard.Overlay.ResetVisuals();
                _graphBoard.Overlay.gameObject.SetActive(false);
            }
        }

        void RefreshHud()
        {
            if (_graphRun != null)
            {
                _hud.SetStats(_playingLevel, _graphRun.Score, _graphRun.Step, _graphRun.TotalSteps);
                _hud.SetHealth(_graphRun.RunNumber, _graphRun.LivesLeft, _graphRun.Config.LivesPerRun, _graphRun.Config.RunsPerSession);
                RefreshGraphLevelOneFue();
                return;
            }

            var run = _gridGame?.Run;
            var level = _gridGame?.CurrentLevel;
            if (level == null)
                return;

            _hud.SetStats(_playingLevel, run?.Score ?? 0, run?.Step ?? 0, run?.TotalSteps ?? 0);
            var livesPerRun = run?.Config.LivesPerRun ?? level.LivesPerRun;
            var runsPerSession = run?.Config.RunsPerSession ?? level.RunsPerSession;
            if (run != null)
                _hud.SetHealth(run.RunNumber, run.LivesLeft, livesPerRun, runsPerSession);
            else
                _hud.SetHealth(1, livesPerRun, livesPerRun, runsPerSession);
            RefreshLevelOneFue(run);
        }

        void RefreshLevelOneFue(GridWalkRun run)
        {
            if (_arenaIntroPlaying || _radarPlaying || _openingCardBlocking)
            {
                _levelOneChrome.Hide();
                return;
            }

            if (_levelOneFue == null || !_levelOneFue.IsActive || run == null)
            {
                if (_levelOneFue == null || _levelOneFue.IsComplete)
                    _levelOneChrome.Hide();
                return;
            }

            _levelOneChrome.Present(_levelOneFue, _board, _camera, run);
        }

        void CompleteLevelOneFueIfNeeded()
        {
            if (_levelOneFue == null || !_levelOneFue.IsComplete)
                return;

            _fueStore?.MarkSeen(LevelOneFueSpec.LessonIdFor(_playingLevel));
            _levelOneChrome.Hide();
            _levelOneFue = null;
        }

        void RefreshGraphLevelOneFue()
        {
            if (_arenaIntroPlaying || _radarPlaying)
            {
                _graphLevelOneChrome.Hide();
                return;
            }

            if (_graphLevelOneFue == null || !_graphLevelOneFue.IsActive)
            {
                if (_graphLevelOneFue == null || _graphLevelOneFue.IsComplete)
                    _graphLevelOneChrome.Hide();
                return;
            }

            var coachingReady = !InputLocked;
            if (coachingReady)
            {
                _graphLevelOneChrome.Present(_graphLevelOneFue, _graphBoard, _camera, _graphRun);
                _hud.SetMessage(_graphLevelOneFue.Beat switch
                {
                    GraphLevelOneFueBeat.PromptTap => GraphLevelOneCopy.Prompt,
                    GraphLevelOneFueBeat.HealthHint => GraphLevelOneCopy.Health,
                    _ => "Tap the next node on the path."
                });
            }
            else
                _graphLevelOneChrome.Hide();

            CompleteGraphLevelOneFueIfNeeded();
        }

        void CompleteGraphLevelOneFueIfNeeded()
        {
            if (_graphLevelOneFue == null || !_graphLevelOneFue.IsComplete)
                return;

            _fueStore?.MarkSeen(GraphLevelOneFueSpec.LessonId);
            _graphLevelOneChrome.Hide();
            _graphLevelOneFue = null;
        }

        void HoldRevealThen(Action after)
        {
            StopRevealHold();
            _revealHold = StartCoroutine(HoldRevealThenRoutine(after));
        }

        IEnumerator HoldRevealThenRoutine(Action after)
        {
            _holdingReveal = true;
            yield return new WaitForSeconds(RevealHoldSeconds);
            _holdingReveal = false;
            _revealHold = null;
            after?.Invoke();
        }

        void StopRevealHold()
        {
            if (_revealHold != null)
            {
                StopCoroutine(_revealHold);
                _revealHold = null;
            }

            _holdingReveal = false;
            _radarPlaying = false;
            _zoomingOverview = false;
            _followReturnPan = false;
            _suppressScoutIntroHighlights = false;
            HideRadarMemorizeCue(immediate: true);
            HideScoutScanFue();
            HideScoutScanPlayer(immediate: true);
            if (_walker != null)
                _walker.MotionPaused = false;
        }

        ScoutScanPlayer ShowScoutScanPlayer()
        {
            var player = ScoutScanPlayer.Ensure(_hud != null ? _hud.OverlayRoot : null);
            player?.Show();
            return player;
        }

        ScoutScanPlayer ShowRadarScanPlayer()
        {
            var player = ShowScoutScanPlayer();
            player?.SetCaption(RadarMemorizeCue.Caption);
            return player;
        }

        ScoutScanPlayer TryShowTileOrGraphScanPlayer()
        {
            if (!PathPreviewSeconds.UsesScanPause(_playingLevel))
            {
                ShowRadarMemorizeCue();
                return null;
            }

            return ShowRadarScanPlayer();
        }

        void ShowRadarMemorizeCue()
        {
            var cue = RadarMemorizeCue.Ensure(_hud != null ? _hud.OverlayRoot : null);
            cue?.Show();
        }

        void HideScoutScanPlayer(bool immediate = false)
        {
            ScoutScanPlayer.HideOn(_hud != null ? _hud.OverlayRoot : null, immediate);
        }

        void BeginScoutScanFue(ScoutScanPlayer player)
        {
            _scoutScanFue = ScoutScanFueSession.TryStart(
                _playingMode,
                _playingLevel,
                _fueStore != null && _fueStore.HasSeen(ScoutScanFueSpec.LessonId));
            if (_scoutScanFue == null)
                return;

            _scoutScanChrome.Ensure(_hud);
            _scoutScanChrome.Present(_scoutScanFue, player);
        }

        IEnumerator WaitScoutScanFue(ScoutScanPlayer player)
        {
            if (_scoutScanFue == null || !_scoutScanFue.IsActive)
                yield break;

            while (_scoutScanFue != null && _scoutScanFue.IsActive)
            {
                if (_walker != null)
                    _walker.MotionPaused = true;
                TickScoutScanFue(player);
                yield return null;
            }

            if (_walker != null)
                _walker.MotionPaused = false;
        }

        void TickScoutScanFue(ScoutScanPlayer player)
        {
            if (_scoutScanFue == null || !_scoutScanFue.IsActive || player == null)
                return;

            _scoutScanFue.ObserveHold(player.HeldPaused);
            _scoutScanChrome.Present(_scoutScanFue, player);
            if (!_scoutScanFue.IsComplete)
                return;

            _fueStore?.MarkSeen(ScoutScanFueSpec.LessonId);
            player.SetCaption(ScoutScanPlayer.Caption);
            _scoutScanChrome.Hide();
            _scoutScanFue = null;
        }

        void HideScoutScanFue()
        {
            _scoutScanChrome.Hide();
            _scoutScanFue = null;
        }

        void HideRadarMemorizeCue(bool immediate = false)
        {
            if (_hud == null || _hud.OverlayRoot == null)
                return;
            var cue = _hud.OverlayRoot.GetComponentInChildren<RadarMemorizeCue>(true);
            if (cue == null)
                return;
            if (immediate)
                cue.HideImmediate();
            else
                cue.Hide();
        }

        void TearDownViews()
        {
            _completionOverview = false;
            _zoomingOverview = false;
            _followReturnPan = false;
            _suppressScoutIntroHighlights = false;
            _radarPlaying = false;
            HideRadarMemorizeCue(immediate: true);
            HideScoutScanFue();
            HideScoutScanPlayer(immediate: true);
            if (_walker != null)
                _walker.MotionPaused = false;
            var radar = transform.Find("Radar Preview");
            if (radar != null)
            {
                var preview = radar.GetComponent<RadarPathPreview>();
                preview?.Clear();
            }
            _graphViewport.Clear();
            StopArenaIntro();
            _openingCard.Hide();
            _openingCardBlocking = false;
            _levelOneChrome.Hide();
            _levelOneFue = null;
            _graphLevelOneChrome.Hide();
            _graphLevelOneFue = null;
            ReleasePlayableGraphLevel();
            HideScoutScanFue();
            ArenaEnvironment.Clear(transform);

            if (_camera != null)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = MemoryPathPalette.PlayBackground;
            }

            if (_board != null)
            {
                Destroy(_board.gameObject);
                _board = null;
            }

            if (_graphBoard != null)
            {
                _graphBoard.Clear();
                Destroy(_graphBoard.gameObject);
                _graphBoard = null;
            }

            if (_walker != null)
            {
                Destroy(_walker.gameObject);
                _walker = null;
            }
        }

        void OnDestroy()
        {
            if (UiNavigator.Current != null && UiNavigator.Current.UnhandledBack == (Action)OnUnhandledBack)
                UiNavigator.Current.UnhandledBack = null;
            StopRevealHold();
            TearDownViews();
            StopTutorial();
            _gridGame = null;
            _graphRun = null;
        }
    }
}
