using Game.Core.Fue;
using Game.Unity.Ui;
using Nixin.Fue;
using UnityEngine;

namespace Game.Unity.Fue
{
    public sealed class MemoryPathGraphLevelOneChrome
    {
        FueNarrationBanner _banner;
        FuePinchHint _pinchHint;
        FuePanHint _panHint;
        GridPathHud _hud;

        public void Ensure(GridPathHud hud)
        {
            _hud = hud;
            var root = hud != null ? hud.OverlayRoot : null;
            if (root == null)
                return;

            if (_banner == null)
                _banner = FueNarrationBanner.Create(root);
            if (_pinchHint == null)
                _pinchHint = FuePinchHint.Create(root);
            if (_panHint == null)
                _panHint = FuePanHint.Create(root);
        }

        public void Present(GraphLevelOneFueSession session)
        {
            if (session == null || !session.IsActive || _hud == null)
                return;

            Ensure(_hud);
            _pinchHint?.transform.SetAsLastSibling();
            _panHint?.transform.SetAsLastSibling();
            _banner?.transform.SetAsLastSibling();
            ShowNarration(session.Beat);
            ShowPinch(session.Beat);
            ShowPan(session.Beat);
        }

        public void Hide()
        {
            _banner?.HideImmediate();
            _pinchHint?.HideImmediate();
            _panHint?.HideImmediate();
        }

        void ShowNarration(GraphLevelOneFueBeat beat)
        {
            var copy = beat switch
            {
                GraphLevelOneFueBeat.PromptZoom => GraphLevelOneCopy.Zoom,
                GraphLevelOneFueBeat.PromptPan => GraphLevelOneCopy.Pan,
                _ => null
            };

            if (string.IsNullOrEmpty(copy))
            {
                _banner?.Hide();
                return;
            }

            _banner.Show(copy, NixinFue.Narrator, Color.white);
        }

        void ShowPinch(GraphLevelOneFueBeat beat)
        {
            if (_pinchHint == null)
                return;

            if (beat == GraphLevelOneFueBeat.PromptZoom)
                _pinchHint.Show();
            else
                _pinchHint.Hide();
        }

        void ShowPan(GraphLevelOneFueBeat beat)
        {
            if (_panHint == null)
                return;

            if (beat == GraphLevelOneFueBeat.PromptPan)
                _panHint.Show();
            else
                _panHint.Hide();
        }
    }
}
