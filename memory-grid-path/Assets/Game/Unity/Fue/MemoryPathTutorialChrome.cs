using System;
using Game.Core.Fue;
using Game.Unity.Ui;
using Game.Unity.View;
using Nixin.Fue;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Fue
{
    /// <summary>
    /// Overlay chrome for the tutorial lesson: intro card, radar watch, then a tap-finger home.
    /// </summary>
    public sealed class MemoryPathTutorialChrome
    {
        static readonly Vector2 TileFingerOffset = new Vector2(8f, -56f);
        static readonly Vector2 TapFingerSize = new Vector2(168f, 168f);

        FueFocusOverlay _overlay;
        FueNarrationBanner _banner;
        FueCenterCard _intro;
        FuePointerHint _tileFinger;
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
            if (_tileFinger == null)
            {
                _tileFinger = FuePointerHint.Create(root);
                _tileFinger.SetAction(FueGestureAction.Tap);
                _tileFinger.SetSize(TapFingerSize);
            }
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
            ApplyReadyCatcher(false);
            ApplySkipVisible(false);
            _tileFinger?.HideImmediate();
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

            GridBoardPresenter.Refresh(
                board,
                session.Run,
                visibleOptions: session.VisibleOptions(),
                showChoicePaths: session.IsPlaying);
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

        void ShowChoiceHints(TutorialSession session, GridBoardView board, Camera camera)
        {
            if (!session.IsPlaying || board == null || !board.IsBuilt)
            {
                HideChoiceHints();
                return;
            }

            var options = session.VisibleOptions();
            if (options.Count == 0)
            {
                HideChoiceHints();
                return;
            }

            var cell = options[0];
            var renderer = RendererOf(board.TileAt(cell));
            _tileFinger?.ShowAtWorld(camera, board.WorldPosition(cell), TileFingerOffset);
            if (renderer != null)
                _overlay.ShowWorld(new[] { renderer }, compulsory: true);
            else
                _overlay?.Hide();
        }

        void HideChoiceHints()
        {
            _overlay?.Hide();
            _tileFinger?.Hide();
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
                case TutorialBeat.Watching:
                    return TutorialCopy.Watch;
                case TutorialBeat.PromptChoice:
                    return TutorialCopy.Prompt;
                case TutorialBeat.SessionFailed:
                    return TutorialCopy.Watch;
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
