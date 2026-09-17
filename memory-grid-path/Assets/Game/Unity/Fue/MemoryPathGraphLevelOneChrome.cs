using System.Collections.Generic;
using Game.Core.Fue;
using Game.Core.Rules;
using Game.Unity.Graph;
using Game.Unity.Ui;
using Game.Unity.View;
using Nixin.Fue;
using UnityEngine;

namespace Game.Unity.Fue
{
    public sealed class MemoryPathGraphLevelOneChrome
    {
        static readonly Vector2 NodeFingerOffset = new Vector2(8f, -56f);
        static readonly Vector2 HealthFingerOffset = new Vector2(-12f, -78f);
        static readonly Vector2 TapFingerSize = new Vector2(168f, 168f);

        FueFocusOverlay _overlay;
        FueNarrationBanner _banner;
        FuePointerHint _healthFinger;
        FuePointerHint _nodeFinger;
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
            if (_nodeFinger == null)
            {
                _nodeFinger = FuePointerHint.Create(root);
                _nodeFinger.SetAction(FueGestureAction.Tap);
                _nodeFinger.SetSize(TapFingerSize);
            }
        }

        public void Present(
            GraphLevelOneFueSession session,
            GraphBoardView board,
            Camera camera,
            GraphWalkRun run)
        {
            if (session == null || !session.IsActive || _hud == null)
                return;

            Ensure(_hud);
            ShowNarration(session.Beat);
            ShowHealthFinger(session.Beat);
            ShowNextNodeHint(session.Beat, board, camera, run);
        }

        public void Hide()
        {
            _overlay?.HideImmediate();
            _banner?.HideImmediate();
            _healthFinger?.HideImmediate();
            _nodeFinger?.HideImmediate();
        }

        void ShowNarration(GraphLevelOneFueBeat beat)
        {
            var copy = beat switch
            {
                GraphLevelOneFueBeat.PromptTap => GraphLevelOneCopy.Prompt,
                GraphLevelOneFueBeat.HealthHint => GraphLevelOneCopy.Health,
                _ => null
            };

            if (string.IsNullOrEmpty(copy))
            {
                _banner?.Hide();
                return;
            }

            _banner.Show(copy, NixinFue.Narrator, Color.white);
        }

        void ShowHealthFinger(GraphLevelOneFueBeat beat)
        {
            var well = _hud != null ? _hud.HealthWell : null;
            if (well == null || _healthFinger == null)
                return;

            if (beat == GraphLevelOneFueBeat.HealthHint)
                _healthFinger.ShowAt(well, HealthFingerOffset);
            else
                _healthFinger.Hide();
        }

        void ShowNextNodeHint(
            GraphLevelOneFueBeat beat,
            GraphBoardView board,
            Camera camera,
            GraphWalkRun run)
        {
            if ((beat != GraphLevelOneFueBeat.PromptTap && beat != GraphLevelOneFueBeat.HealthHint)
                || board == null
                || !board.IsBuilt
                || run == null
                || run.IsOver
                || run.Step + 1 >= run.Path.Nodes.Count)
            {
                HideNodeHint();
                return;
            }

            var next = run.Path.Nodes[run.Step + 1];
            var world = board.WorldPosition(next);
            _nodeFinger?.ShowAtWorld(camera, world, NodeFingerOffset);

            var renderer = RendererOf(board.NodeAt(next));
            if (renderer != null)
                _overlay.ShowWorld(new List<Renderer> { renderer }, compulsory: false);
            else
                _overlay?.Hide();
        }

        void HideNodeHint()
        {
            _overlay?.Hide();
            _nodeFinger?.Hide();
        }

        static Renderer RendererOf(IGraphNodeView node)
        {
            var behaviour = node as MonoBehaviour;
            return behaviour != null ? behaviour.GetComponent<Renderer>() : null;
        }
    }
}
