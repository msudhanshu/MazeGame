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
                Assert.That(bar.sizeDelta.x, Is.EqualTo(GridPathHud.BarWidth));
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
        public void RemainingLifeFillsFromTheBottomOfTheColumn()
        {
            var hud = GridPathHud.Create(null);
            try
            {
                hud.SetHealth(1, 1, 3, 5);

                var tray = hud.transform.Find("Canvas/SafeArea/Bar/Row/Health/Bars/Bar0");
                Assert.That(tray, Is.Not.Null);
                var bottom = tray.Find("Seg0").GetComponent<Image>();
                var mid = tray.Find("Seg1").GetComponent<Image>();
                var top = tray.Find("Seg2").GetComponent<Image>();
                Assert.That(bottom.color, Is.EqualTo(MemoryPathPalette.HealthFill));
                Assert.That(mid.color, Is.EqualTo(MemoryPathPalette.HealthEmpty));
                Assert.That(top.color, Is.EqualTo(MemoryPathPalette.HealthEmpty));
                Assert.That(bottom.transform.GetSiblingIndex(), Is.GreaterThan(top.transform.GetSiblingIndex()));
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
