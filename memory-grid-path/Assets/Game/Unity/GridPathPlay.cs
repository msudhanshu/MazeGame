using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Rules;
using Game.Core.State;
using Game.Unity.Audio;
using Game.Unity.Data;
using Game.Unity.Input;
using Game.Unity.Save;
using Game.Unity.Themes;
using Game.Unity.Themes.Experimental;
using Game.Unity.Ui;
using Game.Unity.Fue;
using Game.Unity.Vfx;
using Game.Unity.View;
using Nixin.Game.Core;
using Nixin.Grid.Core;
using Nixin.Ui;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Drives one level: builds the board for the current theme, forwards taps to the rules,
    /// and repaints the tiles from whatever the rules say afterwards.
    /// </summary>
    public sealed class GridPathPlay : MonoBehaviour
    {
        [SerializeField] HomeScreen _home;
        [SerializeField] LevelSelectScreen _levelSelect;
        [SerializeField] LevelDetailScreen _levelDetail;
        [SerializeField] SettingsScreen _settings;
        [SerializeField] MemoryPathPopup _popup;
        [SerializeField] GridPathHud _hud;

        IPlayerRepository<PlayerProgress> _repository;
        ITileViewFactory _theme;
        ITileEffects _effects;
        GridPathGame _game;
        GridBoardView _board;
        WalkerView _walker;
        Camera _camera;
        ArenaVisualSettings _arenaSettings;
        readonly BoardInput _input = new BoardInput();
        readonly OpeningCardChrome _openingCard = new OpeningCardChrome();
        bool _holdingReveal;
        Coroutine _revealHold;
        MemoryPathStepCallout _stepCallout;

        const float RevealHoldSeconds = 1.25f;

        void Awake()
        {
            _repository = new PlayerPrefsProgressRepository();
            _arenaSettings = LoadArenaVisualSettings();
            _camera = Camera.main;

            var tuning = LoadTuning();
            var catalog = tuning != null ? tuning.CreateCatalog() : new LevelCatalog();
            var config = tuning != null ? tuning.CreateGameConfig() : GameConfig.Default;
            _game = new GridPathGame(catalog, config, _repository.Load());
            _hud = GridPathHud.Resolve(_hud, transform);
            _hud.SetVisible(false);
            _hud.BindExit(ShowPause);
            _effects = DanceFloorEffects.Create(transform);
            MemoryPathAudio.Ensure();
            MemoryPathUi.Ensure(_home, _levelSelect, _levelDetail, _settings, _popup);
            MemoryPathUi.Ensure().UnhandledBack = OnUnhandledBack;
            ShowHome();
        }

        void OnDestroy()
        {
            if (UiNavigator.Current != null && UiNavigator.Current.UnhandledBack == (Action)OnUnhandledBack)
                UiNavigator.Current.UnhandledBack = null;
        }

        void OnUnhandledBack()
        {
            var playing = _hud != null && _hud.IsShown && _game != null && _game.IsPlaying;
            if (DeviceBackPolicy.Resolve(false, playing) == DeviceBackAction.PausePlay)
                ShowPause();
            else
                AppShell.Minimize();
        }

        static GridPathTuning LoadTuning()
        {
#if UNITY_EDITOR
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GridPathTuning>(
                "Assets/Game/Unity/Data/GridPathTuning.asset");
            if (asset != null)
                return asset;
#endif
            return Resources.Load<GridPathTuning>("GridPathTuning");
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

        void LateUpdate()
        {
            if (_arenaSettings != null && _arenaSettings.CameraMode == ArenaCameraMode.FollowWalker)
                UpdateFollowCamera();
            else
                ApplyCameraFraming();
        }

        void UpdateFollowCamera()
        {
            if (_arenaSettings == null || _arenaSettings.CameraMode != ArenaCameraMode.FollowWalker)
                return;
            if (_board == null || !_board.IsBuilt || _walker == null || _camera == null)
                return;

            var focus = _walker.transform.position;
            focus.y = _board.Layout.Origin.y;
            BoardCamera.SmoothFollowWalker(
                _camera,
                focus,
                _arenaSettings.FollowOrthographicSize,
                _arenaSettings.FollowSmoothing);
        }

        void Update()
        {
            var run = _game != null ? _game.Run : null;
            var current = run != null ? run.CurrentCell : default;
            var visibleOptions = run != null ? PathOptionFilter.VisibleGridOptions(run.WalkedCells, run.Options()) : null;
            var hasTarget = _input.TryReadTarget(
                _camera,
                _board,
                current,
                visibleOptions,
                PlayerSettingsStore.PathDrag,
                IsPlayOption,
                out var target);
            if (_game == null || !_game.IsPlaying || InputLocked)
                return;
            if (_hud == null || !_hud.IsShown)
                return;
            if (UiNavigator.Current != null && UiNavigator.Current.IsBlocking)
                return;
            if (!hasTarget || !_game.Run.IsOption(target) || !PathOptionFilter.Contains(visibleOptions, target))
                return;

            Step(target);
        }

        bool IsPlayOption(GridCoord cell) => _game != null && _game.Run != null && _game.Run.IsOption(cell);

        bool InputLocked => _holdingReveal || (_walker != null && _walker.IsHopping);

        void StartLevel(int levelNumber)
        {
            StopRevealHold();
            TearDownBoard();

            try
            {
                _game.SelectLevel(levelNumber);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNotice("Could not open that level.", "TRY AGAIN", () => StartLevel(Math.Max(1, levelNumber)));
                return;
            }

            RefreshHud();
            MemoryPathUi.HideScreens();
            _hud.SetVisible(true);

            if (_game.CanSkip)
            {
                _hud.SetMessage("This stretch is optional.", "Play it, or skip for a small reward.");
                ShowSkip();
                return;
            }

            BeginPlay();
        }

        void BeginPlay()
        {
            try
            {
                _game.StartPlay(Environment.TickCount);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                ShowNotice("Could not build that level.", "TRY AGAIN", () => StartLevel(_game.CurrentLevel.Number));
                return;
            }

            BuildBoard();

            _hud.SetVisible(true);
            var level = _game.CurrentLevel;
            if (level.BlockedHintCount > 0)
            {
                _hud.SetMessage(
                    "Grey tiles are never on the path.",
                    $"{_game.Run.BlockedCells.Count} dead-end hints on this board.");
            }
            else
            {
                _hud.SetMessage("On the path. Keep going!");
            }
            MemoryPathAudio.Play(MemoryPathCue.GameStart);
            RefreshBoard();
            RefreshHud();
        }

        void ConfirmSkip()
        {
            _game.SkipCurrentLevel();
            _repository.Save(_game.Progress);
            StartLevel(_game.NextLevelNumber);
        }

        void BuildBoard()
        {
            TearDownBoard();

            _arenaSettings = LoadArenaVisualSettings();
            _theme = ArenaThemeResolver.Resolve(_arenaSettings);

            var boardGo = new GameObject("Board");
            boardGo.transform.SetParent(transform, false);
            _board = boardGo.AddComponent<GridBoardView>();

            var tileSize = _arenaSettings != null ? _arenaSettings.TileSize : 1f;
            var tileGap = _arenaSettings != null ? _arenaSettings.TileGap : 0f;
            _board.Build(_game.CurrentLevel.Size, _theme, tileSize, tileGap);

            _theme.ApplyEnvironment(_camera, _board.Layout, transform);

            _walker = WalkerView.Create(transform, _board.WorldPosition(_game.Run.Path.Start), DanceFloorPalette.Start);
            ApplyCameraFraming();
        }

        void ApplyCameraFraming()
        {
            if (_camera == null || _board == null || !_board.IsBuilt)
                return;

            if (_arenaSettings != null
                && (_arenaSettings.CameraMode == ArenaCameraMode.FollowWalker
                    || _arenaSettings.LockOrthographicSize)
                && _walker != null)
            {
                var focus = _walker.transform.position;
                focus.y = _board.Layout.Origin.y;
                if (_arenaSettings.CameraMode != ArenaCameraMode.FollowWalker)
                    focus = _board.Layout.Origin;
                BoardCamera.FrameFollow(_camera, focus, _arenaSettings.FollowOrthographicSize);
                return;
            }

            BoardCamera.FrameTopDown(_camera, _board.Layout, _camera.aspect, TileHudViewportInset());
        }

        float TileHudViewportInset()
        {
            if (_hud == null || !_hud.IsShown)
                return 0f;
            return _hud.TopChromeViewportHeight(_camera);
        }

        void TearDownBoard()
        {
            ArenaEnvironment.Clear(transform);

            if (_board != null)
            {
                Destroy(_board.gameObject);
                _board = null;
            }

            if (_walker != null)
            {
                Destroy(_walker.gameObject);
                _walker = null;
            }
        }

        void Step(GridCoord target)
        {
            var run = _game.Run;
            var wrongFrom = _board.WorldPosition(run.Path.Cells[run.Step]);
            var wrongTo = _board.WorldPosition(target);
            var wrongTile = run.Path.Cells[run.Step + 1] == target ? null : _board.TileAt(target);

            var outcome = _game.Choose(target);
            var glimpse = run.ConsumeGlimpse();
            var destination = _board.WorldPosition(run.CurrentCell);
            if (outcome != WalkOutcome.Advanced && outcome != WalkOutcome.LevelCompleted)
                _input.StopPathDrag();

            switch (outcome)
            {
                case WalkOutcome.Advanced:
                    _effects.PlayCorrect(_board.TileAt(target));
                    _walker.HopTo(destination);
                    if (run.WasFailedInPriorWalk(target))
                        ShowStepCallout(MemoryPathStepCueCopy.RandomRecovered(), recover: true);
                    _hud.SetMessage(glimpse != null
                        ? "Glimpse — the whole path flashed."
                        : "On the path. Keep going!");
                    RefreshBoard();
                    break;

                case WalkOutcome.WrongRevealed:
                    _hud.SetMessage($"Off the path. The real tile is lit — {run.LivesLeft} health left.");
                    BeginMistake(
                        wrongTile,
                        wrongFrom,
                        wrongTo,
                        null,
                        forgotPriorWalk: run.LastRevealed.HasValue
                            && run.WasCoveredInPriorWalk(run.LastRevealed.Value));
                    break;

                case WalkOutcome.RunFailed:
                    BeginMistake(wrongTile, wrongFrom, wrongTo, () =>
                    {
                        ShowRetryFromMemory();
                        StartNextWalk();
                    }, runFailed: true, forgotPriorWalk: run.LastRevealed.HasValue
                        && run.WasCoveredInPriorWalk(run.LastRevealed.Value));
                    break;

                case WalkOutcome.LevelCompleted:
                    if (run.LastRevealed.HasValue)
                    {
                        BeginMistake(wrongTile, wrongFrom, wrongTo, () =>
                        {
                            _effects.PlayLevelCompleted();
                            OnSessionFinished(CompletionMessage(run), completed: true);
                        });
                    }
                    else
                    {
                        _effects.PlayLevelCompleted();
                        _walker.HopTo(destination);
                        RefreshBoard();
                        OnSessionFinished(CompletionMessage(run), completed: true);
                    }

                    break;

                case WalkOutcome.SessionOver:
                    _hud.SetMessage("Out of lives.");
                    BeginMistake(
                        wrongTile,
                        wrongFrom,
                        wrongTo,
                        () => HoldRevealThen(() => OnSessionFinished(
                        $"The path faded away. You reached step {run.FurthestStep} of {run.TotalSteps}.",
                        completed: false)), sessionFailed: true);
                    break;
            }

            if (glimpse != null)
                PlayGlimpse(glimpse);

            RefreshHud();
        }

        void BeginMistake(
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
            if (sessionFailed)
            {
                wrong?.SetState(TileVisualState.WrongIntense);
                wrong?.Flash(TileVisualState.WrongIntense, DanceFloorEffects.WrongIntenseFlashSeconds);
                _effects.PlaySessionFailed();
            }
            else if (runFailed)
            {
                wrong?.SetState(forgotPriorWalk ? TileVisualState.WrongIntense : TileVisualState.Wrong);
                wrong?.Flash(
                    forgotPriorWalk ? TileVisualState.WrongIntense : TileVisualState.Wrong,
                    forgotPriorWalk
                        ? DanceFloorEffects.WrongIntenseFlashSeconds
                        : DanceFloorEffects.WrongFlashSeconds);
                _effects.PlayRunFailed();
                if (forgotPriorWalk)
                    ShowStepCallout(MemoryPathStepCueCopy.RandomForgot(), recover: false);
            }
            else
            {
                _effects.PlayMistake(wrong, RevealedTile(_game.Run), forgotPriorWalk);
                if (forgotPriorWalk)
                    ShowStepCallout(MemoryPathStepCueCopy.RandomForgot(), recover: false);
            }

            _revealHold = StartCoroutine(MistakeRoutine(wrongFrom, wrongTo, afterPainted));
        }

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

        IEnumerator MistakeRoutine(Vector3 wrongFrom, Vector3 wrongTo, Action afterPainted)
        {
            var destination = _board.WorldPosition(_game.Run.CurrentCell);
            yield return BoardStepFeedback.FlashWrongTurnThenTravel(
                _walker,
                _board != null ? _board.Overlay : null,
                wrongFrom + Vector3.up * GridPathOverlay.Lift,
                wrongTo + Vector3.up * GridPathOverlay.Lift,
                destination,
                hopPath: null,
                afterFlash: RefreshBoard);
            _holdingReveal = false;
            _revealHold = null;
            afterPainted?.Invoke();
        }

        IEnumerator CelebrateThenPopup(Action show)
        {
            while (_walker != null && _walker.IsHopping)
                yield return null;

            if (_camera != null && _board != null && _board.IsBuilt)
            {
                var origin = _board.Layout.Origin;
                var size = BoardCamera.OrthographicSize(_board.Layout, _camera.aspect);
                origin.z += BoardCamera.TopHudFocusOffset(0f, size);
                var targetPos = new Vector3(origin.x, BoardCamera.Height, origin.z);
                if (!BoardCamera.NearlyMatchesOverview(_camera, targetPos, size))
                {
                    yield return BoardStepFeedback.LerpCamera(
                        _camera,
                        _camera.transform.position,
                        _camera.orthographicSize,
                        targetPos,
                        size,
                        GridPathOverlay.CelebrateZoomSeconds);
                }
                else if (GridPathOverlay.AlreadyOverviewPopupDelay > 0f)
                    yield return new WaitForSeconds(GridPathOverlay.AlreadyOverviewPopupDelay);
            }

            show?.Invoke();
        }

        void OnSessionFinished(string message, bool completed)
        {
            _repository.Save(_game.Progress);
            _hud.SetMessage(completed ? "Splendid Memory!" : "Out of lives!", message);

            var level = _game.CurrentLevel.Number;
            var next = _game.NextLevelNumber;
            if (completed)
            {
                StartCoroutine(CelebrateThenPopup(() => ShowComplete(_game.Run.Score, next, level)));
                return;
            }

            ShowGameOver(level, message);
        }

        void ShowSkip()
        {
            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Skip(_game.Config.SkipRewardPoints, BeginPlay, ConfirmSkip),
                replacePopups: true);
        }

        void ShowNotice(string body, string actionLabel, Action onAction)
        {
            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Notice("Something went sideways", body, actionLabel, onAction),
                replacePopups: true);
        }

        void ShowComplete(int score, int nextLevel, int currentLevel)
        {
            var canAdvance = nextLevel > currentLevel;
            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Complete(
                    score,
                    canAdvance,
                    canAdvance ? (Action)(() => StartLevel(nextLevel)) : null,
                    () => StartLevel(currentLevel),
                    ShowHome),
                replacePopups: true);
        }

        void ShowGameOver(int level, string body)
        {
            _ = body;
            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.GameOver(ShowHome, () => StartLevel(level)),
                replacePopups: true);
        }

        void ShowPause()
        {
            if (!_hud.IsShown)
                return;

            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.Pause(null, () => StartLevel(_game.CurrentLevel.Number), ShowLevelSelect, ConfirmQuitToMenu),
                replacePopups: true);
        }

        void ConfirmQuitToMenu()
        {
            // Bypass exit confirmation for now; keep MemoryPathPopups.Exit in place.
            ShowHome();
        }

        void ShowHome()
        {
            StopRevealHold();
            TearDownBoard();
            _hud.SetVisible(false);
            MemoryPathUi.Ensure().Open<HomeScreen, HomePayload>(new HomePayload
            {
                Level = _game.Progress.HighestUnlockedLevel,
                StarsEarned = LevelAccess.StarsEarned(_game.Progress),
                StarsMax = LevelAccess.StarsPossible(_game.Catalog.Count),
                CareerScore = _game.Progress.CareerScore,
                OnPlay = () => StartLevel(_game.Progress.HighestUnlockedLevel),
                OnLevels = ShowLevelSelect,
                OnSettings = ShowSettings
            });
        }

        void ShowLevelSelect()
        {
            StopRevealHold();
            TearDownBoard();
            _hud.SetVisible(false);

            var tiles = new LevelTileInfo[_game.Catalog.Count];
            for (var i = 0; i < tiles.Length; i++)
            {
                var number = i + 1;
                var lane = LevelAccess.Lane(_game.Progress, number);
                tiles[i] = new LevelTileInfo
                {
                    Number = number,
                    Lane = lane,
                    Stars = LevelAccess.StarsOn(lane)
                };
            }

            MemoryPathUi.Ensure().Open<LevelSelectScreen, LevelSelectPayload>(new LevelSelectPayload
            {
                StarsEarned = LevelAccess.StarsEarned(_game.Progress),
                StarsMax = LevelAccess.StarsPossible(_game.Catalog.Count),
                Tiles = tiles,
                OnBack = ShowHome,
                OnPick = ShowLevelDetail
            });
        }

        void ShowLevelDetail(int levelNumber)
        {
            var level = _game.Catalog.Get(levelNumber);
            MemoryPathUi.Ensure().Open<LevelDetailScreen, LevelDetailPayload>(new LevelDetailPayload
            {
                Level = level.Number,
                Headline = LevelTitles.Headline(level.Number),
                GridLabel = level.Size.Width + " x " + level.Size.Height + " Grid",
                StepsLabel = level.Shape.MinLength + "–" + level.Shape.MaxLength + " steps",
                BestScore = _game.Progress.BestScoreFor(level.Number),
                OnBack = ShowLevelSelect,
                OnStart = () => StartLevel(level.Number)
            });
        }

        void ShowSettings()
        {
            MemoryPathUi.Ensure().Open<SettingsScreen, SettingsPayload>(new SettingsPayload
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
                OnReplayTutorial = ReplayHelpFue,
                OnReplayOpening = ReplayHelpFue
            });
        }

        void ReplayHelpFue()
        {
            var playing = _hud != null && _hud.IsShown;
            if (UiNavigator.Current != null)
                UiNavigator.Current.Close();
            Transform parent = playing && _hud != null
                ? _hud.OverlayRoot
                : UiNavigator.Current != null ? UiNavigator.Current.transform : null;
            if (parent == null)
                return;
            _openingCard.Ensure(parent);
            _openingCard.Show(null);
        }

        void RefreshBoard()
        {
            GridBoardPresenter.Refresh(_board, _game?.Run);
        }

        void RefreshHud()
        {
            var run = _game.Run;
            var level = _game.CurrentLevel;
            if (level == null)
                return;

            _hud.SetStats(
                level.Number,
                run?.Score ?? 0,
                run?.Step ?? 0,
                run?.TotalSteps ?? 0);

            var livesPerRun = run?.Config.LivesPerRun ?? level.LivesPerRun;
            var runsPerSession = run?.Config.RunsPerSession ?? level.RunsPerSession;
            if (run != null)
                _hud.SetHealth(run.RunNumber, run.LivesLeft, livesPerRun, runsPerSession);
            else
                _hud.SetHealth(1, livesPerRun, livesPerRun, runsPerSession);
        }

        string CompletionMessage(GridWalkRun run)
        {
            var message = $"Path complete. Score {run.Score}. Career {_game.Progress.CareerScore}.";
            if (_game.Progress.ConsumeSkipGrantNotice())
            {
                // Skeleton: replace with a skip-unlock animation later.
                message += " Skip unlocked — you may skip the next few levels.";
            }

            return message;
        }

        void PlayGlimpse(IReadOnlyList<GridCoord> cells)
        {
            var tiles = new ITileView[cells.Count];
            for (var i = 0; i < cells.Count; i++)
                tiles[i] = _board.TileAt(cells[i]);

            _effects.PlayPathGlimpse(tiles);
        }

        ITileView RevealedTile(GridWalkRun run)
        {
            return run.LastRevealed.HasValue ? _board.TileAt(run.LastRevealed.Value) : null;
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
        }

        void StartNextWalk()
        {
            _game.BeginNextWalk();
            var run = _game.Run;
            _walker.SnapTo(_board.WorldPosition(run.Path.Start));
            ApplyCameraFraming();
            _hud.SetMessage("On the path. Keep going!");
            RefreshBoard();
            RefreshHud();
        }

        void ShowRetryFromMemory()
        {
            MemoryPathUi.Ensure().Open<MemoryPathPopup, MemoryPathPopupPayload>(
                MemoryPathPopups.RetryFromMemory(),
                replacePopups: true);
        }
    }
}
