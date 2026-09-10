using System;
using Game.Unity.Ui;
using Nixin.Fue;
using UnityEngine;

namespace Game.Unity.Fue
{
    /// <summary>The tutorial opening center card, reused on early levels and from Settings.</summary>
    public sealed class OpeningCardChrome
    {
        FueCenterCard _card;

        public bool IsVisible => _card != null && _card.IsVisible;

        public void Ensure(Transform parent)
        {
            if (parent == null)
                return;
            if (_card != null && _card.transform.parent == parent)
                return;

            _card?.HideImmediate();
            if (_card != null)
                UnityEngine.Object.Destroy(_card.gameObject);

            _card = FueCenterCard.Create(parent);
        }

        public void Show(Action onDismissed)
        {
            if (_card == null)
                return;
            _card.HideImmediate();
            _card.transform.SetAsLastSibling();
            _card.Show(TutorialCopy.Opening, NixinFue.Narrator, onDismissed);
        }

        public void Hide()
        {
            _card?.HideImmediate();
        }
    }
}
