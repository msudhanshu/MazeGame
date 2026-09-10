using System.Collections.Generic;
using Game.Core.Fue;
using Game.Unity.Ui;
using Game.Unity.View;
using Nixin.Fue;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Fue
{
    public sealed class MemoryPathLevelOneChrome
    {
        static readonly Vector2 TileFingerOffset = new Vector2(8f, -56f);
        static readonly Vector2 HealthFingerOffset = new Vector2(-12f, -78f);

        FueFocusOverlay _overlay;
        FueNarrationBanner _banner;
        FuePointerHint _healthFinger;
        FueSwipeHint _swipeHint;
        readonly List<FuePointerHint> _tileFingers = new List<FuePointerHint>();
        GridPathHud _hud;

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
            if (_healthFinger == null)
                _healthFinger = FuePointerHint.Create(root);
            if (_swipeHint == null)
                _swipeHint = FueSwipeHint.Create(root);
        }

        public void Present(
            LevelOneFueSession session,
            GridBoardView board,
            Camera camera,
            IReadOnlyList<GridCoord> visibleOptions,
            GridCoord current)
        {
            if (session == null || !session.IsActive || _hud == null)
                return;

            Ensure(_hud);
            ShowNarration(session.Beat);
            ShowSwipe(session.Beat);
            ShowHealthFinger(session.Beat);
            ShowChoiceHints(session.Beat, board, camera, visibleOptions, current);
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
            _healthFinger?.HideImmediate();
            _swipeHint?.HideImmediate();
            for (var i = 0; i < _tileFingers.Count; i++)
                _tileFingers[i].HideImmediate();
        }

        void ShowNarration(LevelOneFueBeat beat)
        {
            var copy = beat switch
            {
                LevelOneFueBeat.PromptMove => LevelOneCopy.Prompt,
                LevelOneFueBeat.HealthHint => LevelOneCopy.Health,
                _ => null
            };

            if (string.IsNullOrEmpty(copy))
            {
                _banner?.Hide();
                return;
            }

            _banner.Show(copy, null, MemoryPathPalette.Mascot);
        }

        void ShowSwipe(LevelOneFueBeat beat)
        {
            if (_swipeHint == null)
                return;

            if (beat == LevelOneFueBeat.PromptMove)
                _swipeHint.Show();
            else
                _swipeHint.Hide();
        }

        void ShowHealthFinger(LevelOneFueBeat beat)
        {
            var well = _hud != null ? _hud.HealthWell : null;
            if (well == null || _healthFinger == null)
                return;

            if (beat == LevelOneFueBeat.HealthHint)
                _healthFinger.ShowAt(well, HealthFingerOffset);
            else
                _healthFinger.Hide();
        }

        void ShowChoiceHints(
            LevelOneFueBeat beat,
            GridBoardView board,
            Camera camera,
            IReadOnlyList<GridCoord> visibleOptions,
            GridCoord current)
        {
            if (board == null || !board.IsBuilt || visibleOptions == null || visibleOptions.Count == 0)
            {
                HideChoiceHints();
                return;
            }

            var renderers = new List<Renderer>(visibleOptions.Count);
            EnsureTileFingers(visibleOptions.Count);
            for (var i = 0; i < _tileFingers.Count; i++)
            {
                if (i >= visibleOptions.Count)
                {
                    _tileFingers[i].Hide();
                    continue;
                }

                var tile = board.TileAt(visibleOptions[i]);
                var renderer = RendererOf(tile);
                if (renderer != null)
                    renderers.Add(renderer);
                _tileFingers[i].ShowAtWorld(
                    camera,
                    board.WorldPosition(visibleOptions[i]),
                    TileFingerOffset);
            }

            if (renderers.Count > 0 && beat == LevelOneFueBeat.PromptMove)
            {
                _overlay.ShowWorld(renderers, compulsory: false);
                board.Overlay?.ShowChoices(
                    board.WorldPosition(current) + Vector3.up * GridPathOverlay.Lift,
                    BuildChoicePoints(board, visibleOptions),
                    board.Layout.TileSize * 0.09f);
            }
            else
            {
                _overlay?.Hide();
                board.Overlay?.ClearChoices();
            }
        }

        static Vector3[] BuildChoicePoints(GridBoardView board, IReadOnlyList<GridCoord> options)
        {
            var points = new Vector3[options.Count];
            for (var i = 0; i < options.Count; i++)
                points[i] = board.WorldPosition(options[i]) + Vector3.up * GridPathOverlay.Lift;
            return points;
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

        static Renderer RendererOf(ITileView tile)
        {
            var behaviour = tile as MonoBehaviour;
            return behaviour != null ? behaviour.GetComponent<Renderer>() : null;
        }
    }
}
