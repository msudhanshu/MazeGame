using Nixin.Fue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class FuePointerHintTests
    {
        [Test]
        public void DefaultsToTapAndCanSwitchToHold()
        {
            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hint = FuePointerHint.Create(parent.transform);
                Assert.That(hint.Action, Is.EqualTo(FueGestureAction.Tap));
                hint.SetHoldAndRelease(true);
                Assert.That(hint.Action, Is.EqualTo(FueGestureAction.Hold));
                hint.SetAction(FueGestureAction.Tap);
                Assert.That(hint.Action, Is.EqualTo(FueGestureAction.Tap));
                hint.SetSize(new Vector2(168f, 168f));
                Assert.That(hint.GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(168f, 168f)));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void BuildsANonBlockingFinger()
        {
            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var hint = FuePointerHint.Create(parent.transform);
                Assert.That(hint, Is.Not.Null);
                Assert.That(hint.GetComponent<CanvasGroup>().blocksRaycasts, Is.False);
                Assert.That(hint.transform.Find("Ripple"), Is.Null);
                Assert.That(hint.transform.Find("Finger"), Is.Not.Null);
                Assert.That(hint.GetComponentInChildren<Image>(true), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
