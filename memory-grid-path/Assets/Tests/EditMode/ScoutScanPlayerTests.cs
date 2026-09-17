using Game.Unity.Fue;
using Game.Unity.Ui;
using Game.Unity.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class ScoutScanPlayerTests
    {
        [Test]
        public void OverlayLooksLikeAHoldToPauseVideoPlayer()
        {
            Assert.That(
                ScoutScanPlayer.Caption,
                Is.EqualTo("Observe and Memorize the path and its landmarks."));

            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var player = ScoutScanPlayer.Ensure(parent.transform);
                Assert.That(player, Is.Not.Null);
                Assert.That(ScoutScanPlayer.Ensure(parent.transform), Is.SameAs(player));

                player.Show();
                var caption = player.transform.Find("Caption/Label").GetComponent<TMPro.TextMeshProUGUI>();
                Assert.That(caption.text, Is.EqualTo(ScoutScanPlayer.Caption));
                Assert.That(caption.textWrappingMode, Is.EqualTo(TMPro.TextWrappingModes.Normal));

                var fill = player.transform.Find("Dock/Progress/Track/Fill").GetComponent<Image>();
                Assert.That(fill.type, Is.EqualTo(Image.Type.Filled));
                Assert.That(player.HeldPaused, Is.False);
                var hold = player.transform.Find("Hold");
                Assert.That(hold, Is.Not.Null);
                var holdRect = hold.GetComponent<RectTransform>();
                Assert.That(holdRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(holdRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(player.transform.Find("Hold/Pause").gameObject.activeSelf, Is.True);
                Assert.That(player.transform.Find("Hold/Play").gameObject.activeSelf, Is.False);

                player.SetProgress(8f, 20f);
                Assert.That(fill.fillAmount, Is.EqualTo(0.4f).Within(0.001f));
                var clock = player.transform.Find("Dock/Progress/Clock").GetComponent<TMPro.TextMeshProUGUI>();
                Assert.That(clock.text, Is.EqualTo(ScoutRotationMove.FormatClock(8f) + " / " + ScoutRotationMove.FormatClock(20f)));

                player.SetHeld(true);
                Assert.That(player.HeldPaused, Is.True);
                player.SetCaption(ScoutScanCopy.Hold);
                Assert.That(caption.text, Is.EqualTo(ScoutScanCopy.Hold));
                Assert.That(player.HoldControl, Is.SameAs(holdRect));
                Assert.That(player.transform.Find("Hold/Pause").gameObject.activeSelf, Is.False);
                Assert.That(player.transform.Find("Hold/Play").gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
