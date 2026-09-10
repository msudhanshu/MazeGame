using System;
using System.Collections.Generic;
using Game.Core.Fue;
using Game.Unity.Ui;
using Game.Unity.View;
using Nixin.Fue;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Fue
{
    /// <summary>
    /// Overlay chrome for the tutorial lesson: intro card, neighbour glow, fingers, narration.
    /// </summary>
    public sealed class MemoryPathTutorialChrome
    {
        static readonly Vector2 TileFingerOffset = new Vector2(8f, -56f);
        static readonly Vector2 HealthFingerOffset = new Vector2(-12f, -78f);
        static readonly Vector2 HealthCalloutOffset = new Vector2(0f, -8f);

        FueFocusOverlay _overlay;
        FueNarrationBanner _banner;
        FueCenterCard _intro;
        FuePointerHint _healthFinger;
        FueCallout _healthCallout;
        readonly List<FuePointerHint> _tileFingers = new List<FuePointerHint>();
        Button _skip;
        Button _readyCatcher;
        GridPathHud _hud;
        Action _onSkip;
        Action _onIntroDismissed;
        Action _onReadyDismissed;

        public Action OnSkip
        {
            get => _onSkip;
            set
            {
                _onSkip = value;
                ApplySkipVisible(show: _skip != null && _skip.gameObject.activeSelf);
            }
        }

        public Action OnIntroDismissed
        {
            get => _onIntroDismissed;
            set => _onIntroDismissed = value;
        }

        public Action OnReadyDismissed
        {
            get => _onReadyDismissed;
            set => _onReadyDismissed = value;
        }

        public void Ensure(GridPathHud hud)
        {
            _hud = hud;
            var root = hud != null ? hud.OverlayRoot : null;
            if (root == null)
                return;

            if (_overlay == null)
                _overlay = FueFocusOverlay.Create(root);
            if (_banner == null)
                _banner = FueNarrationBanner.Create(root);
            if (_intro == null)
                _intro = FueCenterCard.Create(root);
            if (_healthFinger == null)
                _healthFinger = FuePointerHint.Create(root);
            if (_healthCallout == null)
                _healthCallout = FueCallout.Create(root);
            if (_skip == null)
                BuildSkip(root);
            if (_readyCatcher == null)
                BuildReadyCatcher(root);
        }

        public void Present(TutorialSession session, GridBoardView board, Camera camera)
        {
            if (session == null || _hud == null)
                return;

            Ensure(_hud);
            PaintBoard(session, board);
            if (session.Beat == TutorialBeat.Intro)
            {
                HideChoiceHints();
                _healthFinger?.Hide();
                _healthCallout?.HideImmediate();
                _banner?.HideImmediate();
                ApplySkipVisible(false);
                ApplyReadyCatcher(false);
                ShowIntro();
                return;
            }

            HideIntro();
            ShowNarration(session.Beat);
            if (_banner != null)
                _banner.transform.SetAsLastSibling();
            var waitingForReadyTap = session.Beat == TutorialBeat.Completed;
            ApplySkipVisible(_onSkip != null && !waitingForReadyTap);
            ShowHealthFinger(session);
            ShowHealthCallout(session);
            ShowChoiceHints(session, board, camera);
            ApplyReadyCatcher(waitingForReadyTap);
            if (_skip != null)
                _skip.transform.SetAsLastSibling();
        }

        public void ClearTileHighlights(GridBoardView board = null)
        {
            HideChoiceHints();
            board?.Overlay?.ClearChoices();
        }

        public void Hide()
        {
            _overlay?.HideImmediate();
            _banner?.HideImmediate();
            _intro?.HideImmediate();
            _healthFinger?.HideImmediate();
            _healthCallout?.HideImmediate();
            ApplyReadyCatcher(false);
            ApplySkipVisible(false);
            for (var i = 0; i < _tileFingers.Count; i++)
                _tileFingers[i].HideImmediate();
        }

        void ShowIntro()
        {
            if (_intro == null)
                return;
            _intro.transform.SetAsLastSibling();
            _intro.Show(TutorialCopy.Opening, NixinFue.Narrator, HandleIntroDismissed);
        }

        void HideIntro()
        {
            _intro?.HideImmediate();
        }

        void HandleIntroDismissed()
        {
            if (_onIntroDismissed != null)
                _onIntroDismissed();
        }

        void ApplyReadyCatcher(bool show)
        {
            if (_readyCatcher == null)
                return;
            _readyCatcher.gameObject.SetActive(show);
            if (!show)
                return;
            _readyCatcher.transform.SetAsLastSibling();
            if (_banner != null)
                _banner.transform.SetAsLastSibling();
        }

        void BuildReadyCatcher(Transform parent)
        {
            var go = new GameObject("ReadyCatcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.01f);
            image.raycastTarget = true;
            UiDraw.Stretch(go.GetComponent<RectTransform>());
            _readyCatcher = go.GetComponent<Button>();
            _readyCatcher.transition = Selectable.Transition.None;
            _readyCatcher.onClick.AddListener(HandleReadyDismissed);
            go.SetActive(false);
        }

        void HandleReadyDismissed()
        {
            if (_onReadyDismissed != null)
                _onReadyDismissed();
        }

        static void PaintBoard(TutorialSession session, GridBoardView board)
        {
            if (board == null || !board.IsBuilt)
                return;

            if (session.MemoryRun != null)
            {
                GridBoardPresenter.Refresh(
                    board,
                    session.MemoryRun,
                    visibleOptions: session.VisibleOptions(),
                    showChoicePaths: session.IsPlaying);
                return;
            }

            board.SetAll(TileVisualState.Idle);
            if (session.CurrentCell != session.Goal)
                board.SetState(session.Goal, TileVisualState.Goal);
            foreach (var walked in session.WalkedCells)
                board.SetState(walked, TileVisualState.Walked);
            if (session.LastRevealed.HasValue)
                board.SetState(session.LastRevealed.Value, TileVisualState.Walked);
            if (session.Step == 0)
                board.SetState(session.Start, TileVisualState.Start);

            var options = session.IsPlaying ? session.VisibleOptions() : System.Array.Empty<GridCoord>();
            for (var i = 0; i < options.Count; i++)
                board.SetState(options[i], TileVisualState.Candidate);

            if (session.IsPlaying && options.Count > 0)
            {
                var points = new Vector3[options.Count];
                for (var i = 0; i < options.Count; i++)
                    points[i] = board.WorldPosition(options[i]) + Vector3.up * GridPathOverlay.Lift;
                board.Overlay?.ShowChoices(
                    board.WorldPosition(session.CurrentCell) + Vector3.up * GridPathOverlay.Lift,
                    points,
                    board.Layout.TileSize * 0.09f);
            }
            else
            {
                board.Overlay?.ClearChoices();
            }

            board.Overlay?.ShowHomeAt(board.WorldPosition(session.Goal), board.Layout.TileSize);
        }

        void ShowNarration(TutorialBeat beat)
        {
            var copy = CopyFor(beat);
            if (string.IsNullOrEmpty(copy))
            {
                _banner?.Hide();
                return;
            }

            _banner.Show(copy, NixinFue.Narrator, Color.white);
        }

        void ShowHealthFinger(TutorialSession session)
        {
            var well = _hud != null ? _hud.HealthWell : null;
            if (well == null || _healthFinger == null)
                return;

            if (session.Beat == TutorialBeat.UnluckyPartial || session.Beat == TutorialBeat.UnluckyRunOver)
                _healthFinger.ShowAt(well, HealthFingerOffset);
            else
                _healthFinger.Hide();
        }

        void ShowHealthCallout(TutorialSession session)
        {
            var well = _hud != null ? _hud.HealthWell : null;
            if (well == null || _healthCallout == null)
                return;

            if (session.Beat == TutorialBeat.UnluckyPartial)
                _healthCallout.ShowNear(well, HealthCalloutOffset, TutorialCopy.HealthLostCallout);
            else
                _healthCallout.Hide();
        }

        void ShowChoiceHints(TutorialSession session, GridBoardView board, Camera camera)
        {
            if (!session.IsPlaying || board == null || !board.IsBuilt)
            {
                HideChoiceHints();
                return;
            }

            var options = session.VisibleOptions();
            var renderers = new List<Renderer>(options.Count);
            EnsureTileFingers(options.Count);
            for (var i = 0; i < _tileFingers.Count; i++)
            {
                if (i >= options.Count)
                {
                    _tileFingers[i].Hide();
                    continue;
                }

                var tile = board.TileAt(options[i]);
                var renderer = RendererOf(tile);
                if (renderer != null)
                    renderers.Add(renderer);
                _tileFingers[i].ShowAtWorld(
                    camera,
                    board.WorldPosition(options[i]),
                    TileFingerOffset);
            }

            if (renderers.Count > 0)
                _overlay.ShowWorld(
                    renderers,
                    compulsory: session.Beat != TutorialBeat.UnluckyPartial
                        && session.Beat != TutorialBeat.UnluckyRunOver);
            else
                _overlay.Hide();
        }

        void HideChoiceHints()
        {
            _overlay?.Hide();
            for (var i = 0; i < _tileFingers.Count; i++)
                _tileFingers[i].Hide();
        }

        void EnsureTileFingers(int count)
        {
            var root = _hud != null ? _hud.OverlayRoot : null;
            if (root == null)
                return;
            while (_tileFingers.Count < count)
                _tileFingers.Add(FuePointerHint.Create(root));
        }

        void BuildSkip(Transform parent)
        {
            var skipGo = new GameObject("SkipTutorial", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(parent, false);
            var image = skipGo.GetComponent<Image>();
            image.color = new Color(0.16f, 0.24f, 0.36f, 0.96f);
            image.raycastTarget = true;
            _skip = skipGo.GetComponent<Button>();
            var rect = skipGo.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-18f, 18f);
            rect.sizeDelta = new Vector2(220f, 56f);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(skipGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGo.GetComponent<Text>();
            label.text = "Skip Tutorial";
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = 22;
            label.raycastTarget = false;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null)
                label.font = font;
            _skip.onClick.AddListener(HandleSkip);
            skipGo.SetActive(false);
        }

        void HandleSkip()
        {
            if (_onSkip != null)
                _onSkip();
        }

        void ApplySkipVisible(bool show)
        {
            if (_skip != null)
                _skip.gameObject.SetActive(show && _onSkip != null);
        }

        static Renderer RendererOf(ITileView tile)
        {
            var behaviour = tile as MonoBehaviour;
            return behaviour != null ? behaviour.GetComponent<Renderer>() : null;
        }

        static string CopyFor(TutorialBeat beat)
        {
            switch (beat)
            {
                case TutorialBeat.PromptChoice:
                    return TutorialCopy.Prompt;
                case TutorialBeat.Lucky:
                    return TutorialCopy.Lucky;
                case TutorialBeat.UnluckyPartial:
                    return TutorialCopy.UnluckyPartial;
                case TutorialBeat.UnluckyRunOver:
                    return TutorialCopy.UnluckyRunOver;
                case TutorialBeat.Remembered:
                    return TutorialCopy.Remembered;
                case TutorialBeat.RepeatedMistake:
                    return TutorialCopy.RepeatedMistake;
                case TutorialBeat.SessionFailed:
                    return TutorialCopy.NewPath;
                case TutorialBeat.Completed:
                    return TutorialCopy.Ready;
                case TutorialBeat.Advanced:
                    return TutorialCopy.KeepGoing;
                default:
                    return null;
            }
        }
    }
}
