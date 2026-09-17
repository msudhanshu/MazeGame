using System.Collections.Generic;
using Game.Core.Fue;
using Game.Core.Rules;
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
        static readonly Vector2 TapFingerSize = new Vector2(168f, 168f);

        FueFocusOverlay _overlay;
        FueNarrationBanner _banner;
        FuePointerHint _healthFinger;
        FuePointerHint _tileFinger;
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
            if (_tileFinger == null)
            {
                _tileFinger = FuePointerHint.Create(root);
                _tileFinger.SetAction(FueGestureAction.Tap);
                _tileFinger.SetSize(TapFingerSize);
            }
        }

        public void Present(
            LevelOneFueSession session,
            GridBoardView board,
            Camera camera,
            GridWalkRun run)
        {
            if (session == null || !session.IsActive || _hud == null)
                return;

            Ensure(_hud);
            ShowNarration(session.Beat);
            ShowHealthFinger(session.Beat);
            ShowChoiceHints(session.Beat, board, camera, run);
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
            _tileFinger?.HideImmediate();
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
            GridWalkRun run)
        {
            if (beat != LevelOneFueBeat.PromptMove
                || board == null
                || !board.IsBuilt
                || run == null
                || run.Step + 1 >= run.Path.Cells.Count)
            {
                HideChoiceHints();
                return;
            }

            var next = run.Path.Cells[run.Step + 1];
            var tile = board.TileAt(next);
            var renderer = RendererOf(tile);
            _tileFinger?.ShowAtWorld(camera, board.WorldPosition(next), TileFingerOffset);
            if (renderer != null)
                _overlay.ShowWorld(new List<Renderer> { renderer }, compulsory: false);
            else
                _overlay?.Hide();

            board.Overlay?.ShowChoices(
                board.WorldPosition(run.CurrentCell) + Vector3.up * GridPathOverlay.Lift,
                new[] { board.WorldPosition(next) + Vector3.up * GridPathOverlay.Lift },
                board.Layout.TileSize * 0.09f);
        }

        void HideChoiceHints()
        {
            _overlay?.Hide();
            _tileFinger?.Hide();
        }

        static Renderer RendererOf(ITileView tile)
        {
            var behaviour = tile as MonoBehaviour;
            return behaviour != null ? behaviour.GetComponent<Renderer>() : null;
        }
    }
}
