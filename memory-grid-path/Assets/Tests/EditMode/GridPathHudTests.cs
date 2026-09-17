using System.Linq;
using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class GridPathHudTests
    {
        [Test]
        public void HudBuildsAndAcceptsHealthPaint()
        {
            var hud = GridPathHud.Create(null);
            try
            {
                hud.SetStats(3, 125, 5, 17);
                hud.SetStats("T", 0, 1, 8);
                hud.SetHealth(3, 2, 3, 5);
                hud.SetMessage("On the path. Keep going!");
                Assert.That(hud.transform.Find("Canvas/SafeArea/HintSub").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(GridPathHud.DefaultHintSub));
                hud.FlashHint();
                hud.PulseHealth();
                Assert.That(hud.IsShown, Is.True);
                Assert.That(hud.HealthWell, Is.Not.Null);
                Assert.That(hud.GetComponentInChildren<TextMeshProUGUI>(), Is.Not.Null);
                Assert.That(hud.GetComponentInChildren<Text>(), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(hud.gameObject);
            }
        }

        [Test]
        public void HudMatchesFigmaBarLayout()
        {
            var hud = GridPathHud.Create(null);
            try
            {
                hud.SetStats(19, 125, 5, 17);

                var bar = hud.transform.Find("Canvas/SafeArea/Bar") as RectTransform;
                Assert.That(bar, Is.Not.Null);
                Assert.That(bar.anchorMin.x, Is.EqualTo(0f));
                Assert.That(bar.anchorMax.x, Is.EqualTo(1f));
                Assert.That(bar.sizeDelta.x, Is.EqualTo(-2f * GridPathHud.BarSidePad).Within(0.5f));
                Assert.That(bar.sizeDelta.y, Is.EqualTo(GridPathHud.S(84)).Within(0.5f));
                Assert.That(hud.transform.Find("Canvas/SafeArea").GetComponent<Nixin.Ui.SafeAreaFitter>(), Is.Not.Null);

                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Pause"), Is.Not.Null);
                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Pause").GetComponent<Button>(), Is.Not.Null);
                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Level"), Is.Not.Null);
                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Health"), Is.Not.Null);
                Assert.That(hud.HealthWell, Is.Not.Null);
                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Score"), Is.Not.Null);
                Assert.That(hud.transform.Find("Canvas/SafeArea/Bar/Row/Steps"), Is.Not.Null);

                var texts = hud.GetComponentsInChildren<TextMeshProUGUI>(true).Select(t => t.text).ToArray();
                Assert.That(texts, Does.Contain("LEVEL"));
                Assert.That(texts, Does.Contain("SCORE"));
                Assert.That(texts, Does.Contain("19"));
                Assert.That(texts, Does.Contain("125"));
                Assert.That(texts, Does.Contain("5/17"));
                Assert.That(texts, Does.Not.Contain("X"));
                Assert.That(texts, Does.Not.Contain("Lv"));
                Assert.That(texts, Does.Not.Contain("Steps"));
            }
            finally
            {
                Object.DestroyImmediate(hud.gameObject);
            }
        }

        [Test]
        public void CurrentHeartFillsSpentFromTheBottom()
        {
            var hud = GridPathHud.Create(null);
            try
            {
                hud.SetHealth(1, 1, 3, 5);

                var heart = hud.transform.Find("Canvas/SafeArea/Bar/Row/Health/Bars/Heart0");
                Assert.That(heart, Is.Not.Null);
                var spent = heart.Find("Spent").GetComponent<Image>();
                var fill = heart.Find("Fill").GetComponent<Image>();
                Assert.That(spent.fillAmount, Is.EqualTo(2f / 3f).Within(0.001f));
                var expected = Color.Lerp(MemoryPathPalette.HeartDim, Color.white, 1f / 3f);
                Assert.That(fill.color.r, Is.EqualTo(expected.r).Within(0.01f));
                Assert.That(fill.color.g, Is.EqualTo(expected.g).Within(0.01f));
                Assert.That(fill.color.b, Is.EqualTo(expected.b).Within(0.01f));
                Assert.That(fill.color.a, Is.EqualTo(1f).Within(0.001f));
                Assert.That(fill.sprite, Is.Not.Null);
                Assert.That(fill.preserveAspect, Is.True);
                var well = heart.parent.parent.GetComponent<Image>();
                Assert.That(well, Is.Not.Null);
                Assert.That(well.enabled, Is.True);
                Assert.That(well.color, Is.EqualTo(MemoryPathPalette.HudHealthWellBorder));
                var wellFill = heart.parent.parent.Find("Fill").GetComponent<Image>();
                Assert.That(wellFill.enabled, Is.True);
                Assert.That(wellFill.gameObject.activeSelf, Is.True);
                Assert.That(wellFill.color, Is.EqualTo(MemoryPathPalette.HudHealthWell));
                var tex = fill.sprite.texture;
                Assert.That(tex, Is.Not.Null);
                Assert.That(tex.GetPixel(0, 0).a, Is.LessThan(0.1f));
                Assert.That(tex.GetPixel(tex.width - 1, 0).a, Is.LessThan(0.1f));
                Assert.That(tex.GetPixel(tex.width / 2, tex.height / 2).a, Is.GreaterThan(0.9f));
            }
            finally
            {
                Object.DestroyImmediate(hud.gameObject);
            }
        }

        [Test]
        public void LosingALifeOnTheSameWalkShouldPulseHealth()
        {
            Assert.That(GridPathHud.ShouldPulseHealth(1, 3, 1, 2), Is.True);
            Assert.That(GridPathHud.ShouldPulseHealth(1, 2, 1, 2), Is.False);
            Assert.That(GridPathHud.ShouldPulseHealth(1, 0, 2, 3), Is.False);
        }

        [Test]
        public void HudCanvasScalerConfiguredForPortraitMatchWidth()
        {
            var hud = GridPathHud.Create(null);
            try
            {
                var scaler = hud.GetComponentInChildren<CanvasScaler>(true);
                Assert.That(scaler, Is.Not.Null);
                Assert.That(scaler.referenceResolution.x, Is.EqualTo(1080f));
                Assert.That(scaler.referenceResolution.y, Is.EqualTo(1920f));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0f));

                var bar = hud.transform.Find("Canvas/SafeArea/Bar") as RectTransform;
                Assert.That(bar, Is.Not.Null);
                Assert.That(bar.anchoredPosition.y, Is.EqualTo(-GridPathHud.S(16)).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(hud.gameObject);
            }
        }

        [Test]
        public void OccupiedTopViewportCountsTheBarAndGap()
        {
            Assert.That(GridPathHud.OccupiedTopViewport(1920f, 1920f, 1700f, 20f), Is.EqualTo(240f / 1920f).Within(0.0001f));
            Assert.That(GridPathHud.OccupiedTopViewport(1920f, 0f, 1700f, 20f), Is.EqualTo(0f));
        }
    }
}
