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

        TutorialSession _tutorial;
        readonly MemoryPathTutorialChrome _tutorialChrome = new MemoryPathTutorialChrome();
        readonly OpeningCardChrome _openingCard = new OpeningCardChrome();
        bool _openingCardBlocking;
        ArenaIntroPlayer _arenaIntro;
        bool _arenaIntroPlaying;
        LevelOneFueSession _levelOneFue;
        readonly MemoryPathLevelOneChrome _levelOneChrome = new MemoryPathLevelOneChrome();
        GraphLevelOneFueSession _graphLevelOneFue;
        readonly MemoryPathGraphLevelOneChrome _graphLevelOneChrome = new MemoryPathGraphLevelOneChrome();
        IFueSeenStore _fueStore;
        FueDirector _fueDirector;
        bool _tutorialReplay;

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
            UpdateOverlayFocusState();

            if (_zoomingOverview)
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
                _arenaSettings.FollowSmoothing);
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
                out var target);

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
                ShowOnce = true,
                Ensure = true,
                Replayable = false,
                Compulsory = false,
                Priority = 50,
                Eligible = () => _gridGame != null && _playingMode == GameModeId.TileArena && _playingLevel == LevelOneFueSpec.LevelNumber,
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
                Compulsory = true,
                Priority = 50,
                Eligible = () => _graphRun != null && _playingMode == GameModeId.GraphArena && _playingLevel == GraphLevelOneFueSpec.LevelNumber,
                WantsToShow = () => _graphLevelOneFue != null && _graphLevelOneFue.IsActive,
                Satisfied = () => _graphLevelOneFue != null && _graphLevelOneFue.IsComplete
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
            var from = _board.WorldPosition(_tutorial.CurrentCell);
            var wrongTo = _board.WorldPosition(target);
            var wrongTile = _board.TileAt(target);
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
                case WalkOutcome.WrongRevealed:
                    BeginTutorialMistake(wrongTile, from, wrongTo, null);
                    break;
                case WalkOutcome.RunFailed:
                    BeginTutorialMistake(wrongTile, from, wrongTo, () =>
                    {
                        ShowRetryFromMemory();
                        BeginTutorialMemoryWalk();
                    }, runFailed: true);
                    break;
                case WalkOutcome.LevelCompleted:
                    _effects.PlayLevelCompleted();
                    _walker.HopTo(destination);
                    RefreshTutorial();
                    break;
                case WalkOutcome.SessionOver:
                    BeginTutorialMistake(
                        wrongTile,
                        from,
                        wrongTo,
                        () => HoldRevealThen(() => ShowTutorialGameOver()),
                        sessionFailed: true);
                    break;
            }
        }

        void BeginTutorialMistake(
            ITileView wrong,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Action afterPainted,
            bool runFailed = false,
            bool sessionFailed = false)
        {
            StopRevealHold();
            _holdingReveal = true;
            if (sessionFailed)
            {
                wrong?.SetState(TileVisualState.Wrong);
                _effects.PlaySessionFailed();
            }
            else if (runFailed)
            {
                wrong?.SetState(TileVisualState.Wrong);
                _effects.PlayRunFailed();
            }
            else
            {
                var revealed = _tutorial.LastRevealed.HasValue ? _board.TileAt(_tutorial.LastRevealed.Value) : null;
                _effects.PlayMistake(wrong, revealed);
                PlayTutorialMistakeExtras();
            }

            _revealHold = StartCoroutine(TutorialMistakeRoutine(wrongFrom, wrongTo, afterPainted));
        }

        void PlayTutorialMistakeExtras()
        {
            if (_tutorial == null)
                return;

            if (_tutorial.Beat == TutorialBeat.RepeatedMistake)
                MemoryPathAudio.PlayLongFail();
            else if (_tutorial.Beat == TutorialBeat.UnluckyPartial)
                MemoryPathAudio.PlayWalkFail();
        }

        IEnumerator TutorialMistakeRoutine(Vector3 wrongFrom, Vector3 wrongTo, Action afterPainted)
        {
            var destination = _board.WorldPosition(_tutorial.CurrentCell);
            yield return BoardStepFeedback.FlashWrongTurnThenTravel(
                _walker,
                _board != null ? _board.Overlay : null,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                destination);
            RefreshTutorial();
            _holdingReveal = false;
            _revealHold = null;
            afterPainted?.Invoke();
        }

        void BeginTutorialMemoryWalk()
        {
            _tutorial.BeginMemoryWalk(Environment.TickCount);
            _walker.SnapTo(_board.WorldPosition(_tutorial.Start));
            ApplyCameraFraming();
            RefreshTutorial();
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

        void ShowTutorialGameOver()
        {
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.GameOver(ShowHome, () => StartTutorial(_tutorialReplay)),
                replacePopups: true);
            RefreshTutorial();
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
            _hud.SetMessage(TutorialCopy.Prompt);
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
                _graphViewport.UpdateInput(_camera, _graphBoard != null ? _graphBoard.Layout : null, graphInputAllowed);

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
            _graphLevel = null;
            _sharedGridProgress = false;

            if (!EnsureModeUnlocked(mode, out var reason))
            {
                ShowNotice(reason, "OK", ShowHome);
                return;
            }

            var modeDef = _catalog.Mode(mode);
            if (modeDef.Count < 1)
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

            var entry = modeDef.Get(levelNumber);
            JourneyUi.HideScreens();
            _hud.SetVisible(true);

            if (entry.IsGraph)
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
                _gridGame = new GridPathGame(_catalog.CreateSingleGridCatalog(entry), config, new PlayerProgress());
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
                _fueStore != null && _fueStore.HasSeen(LevelOneFueSpec.LessonId));
            _hud.SetVisible(true);
            _hud.SetMessage("On the path. Keep going!");
            MemoryPathAudio.Play(MemoryPathCue.GameStart);
            _arenaIntroPlaying = true;
            RefreshBoard();
            RefreshHud();
            PlayArenaIntro(() =>
            {
                MaybeStartScoutIntro("On the path. Keep going!");
                MaybeShowOpeningCard();
            });
        }

        void StartGraphLevel(JourneyModeDefinition modeDef, JourneyLevelEntry entry)
        {
            _gridGame = null;
            _sharedGridProgress = false;
            _graphLevel = entry.GraphLevel != null ? entry.GraphLevel : GraphLevelDefinition.CreateSampleRuntime();
            _arenaSettings = ArenaVisualSettings.CreateClassicOverride(
                modeDef.CameraMode,
                modeDef.FollowSizeFor(entry),
                modeDef.ResolvedFollowSmoothing,
                modeDef.LocksOrthographicSize(entry));
            _graphRun = GraphBoardPresenter.CreateRun(_graphLevel, Environment.TickCount);

            var boardGo = new GameObject("GraphBoard");
            boardGo.transform.SetParent(transform, false);
            _graphBoard = boardGo.AddComponent<GraphBoardView>();
            _graphFactory.OceanBackdrop = modeDef.ModeId == GameModeId.ScoutArena;
            _graphBoard.Build(_graphLevel, _graphFactory);
            _graphFactory.ApplyEnvironment(_camera, _graphBoard.Layout, transform);
            if (_playingMode == GameModeId.GraphArena)
                _graphViewport.Reset(_graphBoard.Layout, _camera.aspect);
            else
                _graphViewport.Clear();
            _walker = WalkerView.Create(
                transform,
                _graphBoard.WorldPosition(_graphRun.CurrentNode),
                DanceFloorPalette.Start,
                modeDef.WalkerScaleFor(entry));
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
            PlayArenaIntro(() =>
            {
                MaybeStartScoutIntro("Tap the next node on the path.");
                MaybeShowOpeningCard();
            });
        }

        void BuildGridBoard()
        {
            TearDownViews();
            var modeDef = _catalog.Mode(_playingMode);
            var entry = modeDef.Get(_playingLevel);
            EnsureProceduralMosaic(entry);
            _arenaSettings = JourneyVisualResolver.Resolve(
                entry,
                modeDef.CameraMode,
                _proceduralMosaic,
                modeDef.FollowSizeFor(entry),
                modeDef.ResolvedFollowSmoothing,
                modeDef.LocksOrthographicSize(entry));
            _theme = ArenaThemeResolver.Resolve(_arenaSettings);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GridBoardView>();
            _board.Build(_gridGame.CurrentLevel.Size, _theme, _arenaSettings.TileSize, _arenaSettings.TileGap);
            _theme.ApplyEnvironment(_camera, _board.Layout, transform);
            _graphViewport.Clear();
            _walker = WalkerView.Create(
                transform,
                _board.WorldPosition(_gridGame.Run.Path.Start),
                DanceFloorPalette.Start,
                modeDef.WalkerScaleFor(entry));
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
                    BoardCamera.FrameFollow(_camera, focus, _arenaSettings.FollowOrthographicSize);
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
                    _camera.aspect);
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
                    _effects.PlayCorrect(_board.TileAt(target));
                    _walker.HopTo(destination);
                    _levelOneFue?.OnCorrectStep();
                    CompleteLevelOneFueIfNeeded();
                    _hud.SetMessage("On the path. Keep going!");
                    RefreshBoard();
                    break;
                case WalkOutcome.WrongRevealed:
                    _levelOneFue?.OnWrongStep();
                    _hud.SetMessage($"Off the path — {run.LivesLeft} health left.");
                    BeginGridMistake(wrongTile, wrongFrom, wrongTo, null);
                    break;
                case WalkOutcome.RunFailed:
                    BeginGridMistake(wrongTile, wrongFrom, wrongTo, () =>
                    {
                        ShowRetryFromMemory();
                        _revealHold = StartCoroutine(RewindAfterFailedWalk());
                    }, runFailed: true);
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
                    _hud.SetMessage("Out of lives.");
                    BeginGridMistake(
                        wrongTile,
                        wrongFrom,
                        wrongTo,
                        () => HoldRevealThen(() => FinishSession(false, run.Score)),
                        sessionFailed: true);
                    break;
            }

            RefreshHud();
        }

        void BeginGridMistake(
            ITileView wrong,
            Vector3 wrongFrom,
            Vector3 wrongTo,
            Action afterPainted,
            bool runFailed = false,
            bool sessionFailed = false)
        {
            StopRevealHold();
            _holdingReveal = true;
            var run = _gridGame.Run;
            if (sessionFailed)
            {
                wrong?.SetState(TileVisualState.Wrong);
                _effects.PlaySessionFailed();
            }
            else if (runFailed)
            {
                wrong?.SetState(TileVisualState.Wrong);
                _effects.PlayRunFailed();
            }
            else
            {
                _effects.PlayMistake(wrong, _board.TileAt(run.CurrentCell));
            }

            _revealHold = StartCoroutine(GridMistakeRoutine(wrongFrom, wrongTo, afterPainted));
        }

        IEnumerator GridMistakeRoutine(Vector3 wrongFrom, Vector3 wrongTo, Action afterPainted)
        {
            var run = _gridGame.Run;
            var destination = _board.WorldPosition(run.CurrentCell);
            yield return BoardStepFeedback.FlashWrongTurnThenTravel(
                _walker,
                _board != null ? _board.Overlay : null,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                destination,
                hopPath: null,
                afterFlash: () => RefreshBoard());
            _holdingReveal = false;
            _revealHold = null;
            afterPainted?.Invoke();
        }

        void StepGraph(Nixin.Graph.Core.GraphNodeId target)
        {
            var from = _graphRun.CurrentNode;
            var wrongFrom = _graphBoard.WorldPosition(from);
            var wrongTo = _graphBoard.WorldPosition(target);
            var outcome = _graphRun.Choose(target);
            var hopPath = _graphBoard.EdgeWorldPoints(from, _graphRun.CurrentNode);

            switch (outcome)
            {
                case WalkOutcome.Advanced:
                    _walker.HopAlong(hopPath);
                    RefreshGraphBoard();
                    _effects.PlayCorrect(null);
                    _hud.SetMessage("On the path. Keep going!");
                    break;
                case WalkOutcome.WrongRevealed:
                    _graphBoard.SetState(target, GraphNodeVisualState.Wrong);
                    _effects.PlayMistake(null, null);
                    _hud.SetMessage($"Wrong junction — {_graphRun.LivesLeft} lives left.");
                    StopRevealHold();
                    _holdingReveal = true;
                    _revealHold = StartCoroutine(GraphMistakeRoutine(
                        wrongFrom,
                        wrongTo,
                        hopPath,
                        () =>
                        {
                            _holdingReveal = false;
                            _revealHold = null;
                        }));
                    break;
                case WalkOutcome.RunFailed:
                    _graphBoard.SetState(target, GraphNodeVisualState.Wrong);
                    _effects.PlayRunFailed();
                    StopRevealHold();
                    _holdingReveal = true;
                    _revealHold = StartCoroutine(GraphMistakeRoutine(
                        wrongFrom,
                        wrongTo,
                        hopPath,
                        () =>
                        {
                            ShowRetryFromMemory();
                            _revealHold = StartCoroutine(RewindAfterFailedWalk());
                        }));
                    break;
                case WalkOutcome.LevelCompleted:
                    if (_graphRun.LastRevealed.HasValue)
                    {
                        _graphBoard.SetState(target, GraphNodeVisualState.Wrong);
                        _effects.PlayMistake(null, null);
                        StopRevealHold();
                        _holdingReveal = true;
                        _revealHold = StartCoroutine(GraphMistakeRoutine(
                            wrongFrom,
                            wrongTo,
                            hopPath,
                            () =>
                            {
                                _effects.PlayLevelCompleted();
                                BeginCompletion(_graphRun.Score);
                            }));
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
                    _graphBoard.SetState(target, GraphNodeVisualState.Wrong);
                    _effects.PlaySessionFailed();
                    StopRevealHold();
                    _holdingReveal = true;
                    _revealHold = StartCoroutine(GraphMistakeRoutine(
                        wrongFrom,
                        wrongTo,
                        hopPath,
                        () =>
                        {
                            _holdingReveal = false;
                            _revealHold = null;
                            HoldRevealThen(() => FinishSession(false, _graphRun.Score));
                        }));
                    break;
            }

            RefreshHud();
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
                    _catalog.Mode(_playingMode).Count);
            }

            _journey.RememberPlayed(_playingMode, _playingLevel);
            _repository.Save(_journey);
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
                    _graphViewport.Reset(_graphBoard.Layout, _camera != null ? _camera.aspect : 1f);
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
                size = BoardCamera.ContainOrthographicSize(
                    _graphBoard.Layout.WorldWidth,
                    _graphBoard.Layout.WorldDepth,
                    _camera.aspect);
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
                    _camera.aspect);
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
            _revealHold = StartCoroutine(ScoutIntroRoutine(origin, overviewSize, settledMessage));
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
                {
                    ApplyCameraFraming();
                    RefreshBoard();
                }
                if (_graphBoard != null && _graphBoard.IsBuilt)
                    RefreshGraphBoard();
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

            var options = _graphRun.Options();
            _graphBoard.SetNodesVisible(_graphRun.CurrentNode, _graphRun.WalkedNodes, options, showAll: true);
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

        void MaybeShowOpeningCard()
        {
            if (_graphLevelOneFue != null && _graphLevelOneFue.IsActive)
                return;
            ShowOpeningCard(force: false);
        }

        void ShowOpeningCard(bool force, bool overlayHome = false)
        {
            if (!force && !OpeningCardSpec.ShouldShow(_playingLevel))
                return;
            if (!force
                && _fueStore != null
                && _fueStore.HasSeen(OpeningCardSpec.LessonIdFor(_playingLevel)))
                return;

            Transform parent = null;
            if (overlayHome)
            {
                var nav = UiNavigator.Current;
                parent = nav != null ? nav.transform : null;
            }

            if (parent == null && _hud != null)
                parent = _hud.OverlayRoot;
            if (parent == null)
                return;

            _openingCard.Ensure(parent);
            _openingCardBlocking = true;
            var level = _playingLevel;
            var markSeen = !overlayHome;
            _openingCard.Show(() =>
            {
                _openingCardBlocking = false;
                if (markSeen && OpeningCardSpec.ShouldShow(level))
                    _fueStore?.MarkSeen(OpeningCardSpec.LessonIdFor(level));
            });
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

        IEnumerator GraphMistakeRoutine(Vector3 wrongFrom, Vector3 wrongTo, IReadOnlyList<Vector3> hopPath, Action after)
        {
            yield return BoardStepFeedback.FlashWrongTurnThenTravel(
                _walker,
                _graphBoard != null ? _graphBoard.Overlay : null,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                _graphBoard.WorldPosition(_graphRun.CurrentNode),
                hopPath,
                afterFlash: () => RefreshGraphBoard());
            _holdingReveal = false;
            _revealHold = null;
            after?.Invoke();
        }

        int NextPlayable()
        {
            var progress = _journey.For(_playingMode);
            var next = _playingLevel + 1;
            if (next <= _catalog.Mode(_playingMode).Count && progress.IsUnlocked(next))
                return next;
            return _playingLevel;
        }

        void StartNextGridWalk()
        {
            _gridGame.BeginNextWalk();
            var run = _gridGame.Run;
            _walker.SnapTo(_board.WorldPosition(run.Path.Start));
            ApplyCameraFraming();
            _hud.SetMessage("On the path. Keep going!");
            RefreshBoard();
            RefreshHud();
        }

        IEnumerator RewindAfterFailedWalk()
        {
            _holdingReveal = true;
            _zoomingOverview = true;

            if (_graphRun != null && _graphRun.IsAwaitingNextWalk)
            {
                _graphRun.BeginNextWalk();
                _walker.SnapTo(_graphBoard.WorldPosition(_graphRun.CurrentNode));
                RefreshGraphBoard();
                if (_playingMode == GameModeId.GraphArena && _graphBoard != null && _graphBoard.IsBuilt)
                {
                    var aspect = _camera != null ? _camera.aspect : 1f;
                    _graphViewport.Reset(_graphBoard.Layout, aspect);
                    if (TryOverviewFrame(out var origin, out var size))
                    {
                        var fromPos = _camera != null ? _camera.transform.position : origin;
                        var fromSize = _camera != null ? _camera.orthographicSize : size;
                        var toPos = new Vector3(origin.x, BoardCamera.Height, origin.z);
                        yield return BoardStepFeedback.LerpCamera(
                            _camera,
                            fromPos,
                            fromSize,
                            toPos,
                            size,
                            BoardStepFeedback.RewindCameraSeconds);
                        _graphViewport.Apply(_camera);
                    }
                    else
                        ApplyCameraFraming();
                }
                else if (ShouldPlayScoutIntro() && _walker != null && _camera != null && _arenaSettings != null)
                {
                    yield return LerpFollowToWalker();
                }
                else
                    ApplyCameraFraming();
            }
            else if (_gridGame != null && _gridGame.Run != null && _gridGame.Run.IsAwaitingNextWalk)
            {
                _gridGame.BeginNextWalk();
                var run = _gridGame.Run;
                _walker.SnapTo(_board.WorldPosition(run.Path.Start));
                _hud.SetMessage("On the path. Keep going!");
                RefreshBoard();
                if (ShouldPlayScoutIntro() && _walker != null && _camera != null && _arenaSettings != null)
                    yield return LerpFollowToWalker();
                else
                    ApplyCameraFraming();
            }

            _hud.SetMessage("On the path. Keep going!");
            RefreshHud();
            _zoomingOverview = false;
            _holdingReveal = false;
            _revealHold = null;
        }

        IEnumerator LerpFollowToWalker()
        {
            var focus = _walker.transform.position;
            if (_board != null && _board.IsBuilt)
                focus.y = _board.Layout.Origin.y;
            else if (_graphBoard != null && _graphBoard.IsBuilt)
                focus.y = _graphBoard.Layout.Origin.y;

            var toPos = new Vector3(focus.x, BoardCamera.Height, focus.z);
            var toSize = _arenaSettings.FollowOrthographicSize;
            var fromPos = _camera.transform.position;
            var fromSize = _camera.orthographicSize;
            yield return BoardStepFeedback.LerpCamera(
                _camera,
                fromPos,
                fromSize,
                toPos,
                toSize,
                BoardStepFeedback.RewindCameraSeconds);
        }

        void ShowRetryFromMemory()
        {
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.RetryFromMemory(),
                replacePopups: true);
        }

        void ShowHome()
        {
            StopRevealHold();
            StopTutorial();
            TearDownViews();
            _gridGame = null;
            _graphRun = null;
            _graphLevel = null;
            _sharedGridProgress = false;
            _hud.SetVisible(false);
            if (!ModeExists(_selectedMode) || !GameModeUnlock.IsUnlocked(
                    _selectedMode,
                    _journey,
                    _catalog.UnlockConfig,
                    Math.Max(1, _catalog.TileArena.Count),
                    Math.Max(1, _catalog.GraphArena.Count)))
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
                StarsMax = LevelAccess.StarsPossible(Math.Max(1, mode.Count)),
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
                    Math.Max(1, _catalog.TileArena.Count),
                    Math.Max(1, _catalog.GraphArena.Count)),
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
                    Math.Max(1, _catalog.TileArena.Count),
                    Math.Max(1, _catalog.GraphArena.Count)))
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
            var tiles = new JourneyLevelTileInfo[mode.Count];
            for (var i = 0; i < tiles.Length; i++)
            {
                var number = i + 1;
                var lane = LevelAccess.Lane(progress, number);
                var entry = mode.Get(number);
                tiles[i] = new JourneyLevelTileInfo
                {
                    Number = number,
                    Lane = lane,
                    Stars = LevelAccess.StarsOn(lane),
                    Thumbnail = entry.ThumbnailSource,
                    Swatch = SwatchFor(entry)
                };
            }

            JourneyUi.Ensure().Open<JourneyLevelSelectScreen, JourneyLevelSelectPayload>(new JourneyLevelSelectPayload
            {
                Title = mode.DisplayName,
                StarsEarned = LevelAccess.StarsEarned(progress),
                StarsMax = LevelAccess.StarsPossible(Math.Max(1, mode.Count)),
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
            var mode = _catalog.Mode(_selectedMode);
            var entry = mode.Get(levelNumber);
            var progress = _journey.For(_selectedMode);
            var gridLabel = entry.IsGraph
                ? (entry.GraphLevel != null ? entry.GraphLevel.DisplayName : "Graph path")
                : entry.GridSpec.Width + " x " + entry.GridSpec.Height + " Grid";
            var steps = entry.IsGraph
                ? "Click the next node"
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
                    canAdvance ? (Action)(() => StartLevel(_playingMode, nextLevel)) : null,
                    () => StartLevel(_playingMode, currentLevel),
                    ShowHome),
                replacePopups: true);
        }

        void ShowGameOver(int level, string body)
        {
            _ = body;
            JourneyUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.GameOver(ShowHome, () => StartLevel(_playingMode, level)),
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

        bool EnsureModeUnlocked(GameModeId mode, out string reason)
        {
            var unlocked = GameModeUnlock.IsUnlocked(
                mode,
                _journey,
                _catalog.UnlockConfig,
                Math.Max(1, _catalog.TileArena.Count),
                Math.Max(1, _catalog.GraphArena.Count));
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
                : _suppressScoutIntroHighlights && IsScoutGridPlay
                    ? System.Array.Empty<GridCoord>()
                    : PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options());
            GridBoardPresenter.Refresh(
                _board,
                run,
                visibleOptions: visibleOptions,
                showChoicePaths: IsScoutGridPlay && !_suppressScoutIntroHighlights);
            RefreshLevelOneFue(run, visibleOptions);
        }

        void RefreshGraphBoard()
        {
            var visibleOptions = _graphRun == null
                ? null
                : _suppressScoutIntroHighlights && _playingMode == GameModeId.ScoutArena
                    ? System.Array.Empty<Nixin.Graph.Core.GraphNodeId>()
                    : PathOptionFilter.VisibleGraphOptions(_graphRun.WalkedNodes, _graphRun.Options());
            GraphBoardPresenter.Refresh(_graphBoard, _graphRun, false, visibleOptions);
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
            RefreshLevelOneFue(run, run == null ? null : PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options()));
        }

        void RefreshLevelOneFue(GridWalkRun run, IReadOnlyList<GridCoord> visibleOptions)
        {
            if (_arenaIntroPlaying)
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

            _levelOneChrome.Present(_levelOneFue, _board, _camera, visibleOptions, run.CurrentCell);
        }

        void CompleteLevelOneFueIfNeeded()
        {
            if (_levelOneFue == null || !_levelOneFue.IsComplete)
                return;

            _fueStore?.MarkSeen(LevelOneFueSpec.LessonId);
            _levelOneChrome.Hide();
            _levelOneFue = null;
        }

        void RefreshGraphLevelOneFue()
        {
            if (_arenaIntroPlaying)
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
                _graphLevelOneFue.ObserveGestures(_graphViewport.DidZoom, _graphViewport.DidPan);
                _graphLevelOneChrome.Present(_graphLevelOneFue);
                _hud.SetMessage(_graphLevelOneFue.Beat switch
                {
                    GraphLevelOneFueBeat.PromptZoom => GraphLevelOneCopy.Zoom,
                    GraphLevelOneFueBeat.PromptPan => GraphLevelOneCopy.Pan,
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
            _zoomingOverview = false;
            _suppressScoutIntroHighlights = false;
        }

        void TearDownViews()
        {
            _completionOverview = false;
            _zoomingOverview = false;
            _suppressScoutIntroHighlights = false;
            _graphViewport.Clear();
            StopArenaIntro();
            _openingCard.Hide();
            _openingCardBlocking = false;
            _levelOneChrome.Hide();
            _levelOneFue = null;
            _graphLevelOneChrome.Hide();
            _graphLevelOneFue = null;
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
