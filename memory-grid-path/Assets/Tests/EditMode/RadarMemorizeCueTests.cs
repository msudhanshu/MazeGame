using Game.Unity.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class RadarMemorizeCueTests
    {
        [Test]
        public void CueIsACenteredOneLinePrompt()
        {
            Assert.That(RadarMemorizeCue.Caption, Is.EqualTo("Try to memorize the path."));

            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var cue = RadarMemorizeCue.Ensure(parent.transform);
                Assert.That(cue, Is.Not.Null);
                Assert.That(RadarMemorizeCue.Ensure(parent.transform), Is.SameAs(cue));

                var group = cue.GetComponent<CanvasGroup>();
                Assert.That(group, Is.Not.Null);
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(group.interactable, Is.False);

                var rect = cue.GetComponent<RectTransform>();
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0.5f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0.5f)));

                cue.Show();
                var label = cue.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Is.EqualTo(RadarMemorizeCue.Caption));
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.textWrappingMode, Is.EqualTo(TMPro.TextWrappingModes.NoWrap));

                Assert.That(label.fontSize, Is.GreaterThanOrEqualTo(48f));

                var panel = cue.transform.Find("Panel").GetComponent<Image>();
                Assert.That(panel.color.a, Is.GreaterThanOrEqualTo(0.85f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
