using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class LevelDetailScreenTests
    {
        [Test]
        public void BindPaintsOverviewCopyAndStartLabel()
        {
            var screen = LevelDetailScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column");
                Assert.That(
                    col.Find("Header/Title").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(LevelDetailScreen.TitleCopy));
                Assert.That(
                    col.Find("Card/Banner/Copy/Kicker").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(LevelDetailScreen.KickerCopy));
                Assert.That(
                    col.Find("Card/Banner/Copy/Headline").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Level 5: Lost Forest"));
                Assert.That(
                    col.Find("Card/Objectives/Heading").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(LevelDetailScreen.ObjectivesCopy));
                Assert.That(
                    col.Find("Card/Objectives/Grid/Left/Cap").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Grid Size"));
                Assert.That(
                    col.Find("Card/Objectives/Grid/Val").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("6 x 8 Grid"));
                Assert.That(
                    col.Find("Card/Objectives/Steps/Val").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Walk the hidden path"));
                Assert.That(
                    col.Find("Card/Objectives/Best/Val").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("850 pts"));
                Assert.That(
                    col.Find("Card/Start/Row/Label").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Start Level 5"));
                Assert.That(col.Find("Card/Start/Row/Triangle"), Is.Not.Null);
                Assert.That(col.Find("Card/Banner/Mascot/Eyes"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void BannerShowsOptionalThumbnailAndStartInvokes()
        {
            var thumb = Texture2D.whiteTexture;
            var screen = LevelDetailScreen.CreateTemplate();
            var started = 0;
            var backed = 0;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.Thumbnail = thumb;
                payload.OnStart = () => started++;
                payload.OnBack = () => backed++;
                screen.Bind(payload);

                var bannerThumb = screen.transform.Find("Column/Card/Banner/Thumb").GetComponent<RawImage>();
                Assert.That(bannerThumb.gameObject.activeSelf, Is.True);
                Assert.That(bannerThumb.texture, Is.EqualTo(thumb));

                screen.transform.Find("Column/Card/Start").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Header/Back").GetComponent<Button>().onClick.Invoke();
                Assert.That(started, Is.EqualTo(1));
                Assert.That(backed, Is.EqualTo(1));
                Assert.That(screen.TryHandleBack(), Is.True);
                Assert.That(backed, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void LayoutFillsPortraitCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);

            var screen = LevelDetailScreen.CreateTemplate();
            try
            {
                screen.transform.SetParent(canvasRect, false);
                UiDraw.Stretch(screen.GetComponent<RectTransform>());
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column") as RectTransform;
                Assert.That(col.rect.width, Is.EqualTo(1080f).Within(1f));
                Assert.That(screen.transform.Find("Bg").GetComponent<RawImage>().texture, Is.Not.Null);
                Assert.That(col.Find("Card").GetComponent<RectTransform>().rect.width, Is.GreaterThan(900f));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void BindDoesNotRewritePrefabLayoutOrArt()
        {
            var screen = LevelDetailScreen.CreateTemplate();
            try
            {
                var col = screen.transform.Find("Column") as RectTransform;
                var layout = col.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(3, 4, 5, 6);
                var bg = screen.transform.Find("Bg").GetComponent<RawImage>();
                var marker = Texture2D.whiteTexture;
                bg.texture = marker;

                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                Assert.That(layout.padding.left, Is.EqualTo(3));
                Assert.That(bg.texture, Is.SameAs(marker));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        static LevelDetailPayload SamplePayload()
        {
            return new LevelDetailPayload
            {
                Level = 5,
                Headline = "Level 5: Lost Forest",
                GridLabel = "6 x 8 Grid",
                StepsLabel = "Walk the hidden path",
                BestScore = 850
            };
        }
    }
}
