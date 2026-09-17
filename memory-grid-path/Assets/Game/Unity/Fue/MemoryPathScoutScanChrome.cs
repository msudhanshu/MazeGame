using Game.Core.Fue;
using Game.Unity.Ui;
using Nixin.Fue;
using UnityEngine;

namespace Game.Unity.Fue
{
    public sealed class MemoryPathScoutScanChrome
    {
        static readonly Vector2 HoldFingerOffset = new Vector2(48f, -88f);

        FuePointerHint _finger;
        GridPathHud _hud;

        public void Ensure(GridPathHud hud)
        {
            _hud = hud;
            var root = hud != null ? hud.OverlayRoot : null;
            if (root == null)
                return;

            if (_finger == null)
                _finger = FuePointerHint.Create(root);
            _finger.SetAction(FueGestureAction.Hold);
        }

        public void Present(ScoutScanFueSession session, ScoutScanPlayer player)
        {
            if (session == null || !session.IsActive || player == null)
            {
                Hide();
                return;
            }

            Ensure(_hud);
            _finger?.transform.SetAsLastSibling();
            var hold = player.HoldControl;
            if (hold != null)
                _finger?.ShowAt(hold, HoldFingerOffset);
            else
                _finger?.Hide();

            if (session.Beat == ScoutScanFueBeat.PromptHold)
                player.SetCaption(ScoutScanCopy.Hold);
            else if (session.Beat == ScoutScanFueBeat.PromptRelease)
                player.SetCaption(ScoutScanCopy.Release);
        }

        public void Hide()
        {
            _finger?.HideImmediate();
        }
    }
}
