using Game.Core.State;
using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class JourneyLevelSelectScreenTests
    {
        [Test]
        public void CreateTemplateIncludesHiddenLevelTile()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                var template = screen.transform.Find("Templates/LevelTile");
                Assert.That(template, Is.Not.Null);
                Assert.That(template.GetComponent<LevelTileCard>(), Is.Not.Null);
                Assert.That(screen.transform.Find("Templates").gameObject.activeSelf, Is.False);
                Assert.That(template.Find("Overlay/Lock"), Is.Not.Null);
                Assert.That(template.Find("Overlay/Stars/Star0"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void BindPaintsFigmaHeaderGridAndFooter()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column");
                Assert.That(col, Is.Not.Null);
                Assert.That(
                    col.Find("Header/Title").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Tile Arena"));
                Assert.That(col.Find("Header/Back"), Is.Not.Null);
                Assert.That(
                    col.Find("Footer/Hint").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(JourneyLevelSelectScreen.FooterCopy));
                Assert.That(col.Find("Footer/Arrow"), Is.Not.Null);

                var grid = col.Find("Scroll/Grid").GetComponent<GridLayoutGroup>();
                Assert.That(grid.constraintCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void CardsShowThumbnailBackgroundStarsOrLock()
        {
            var thumb = Texture2D.whiteTexture;
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload(thumb));

                var cleared = screen.transform.Find("Column/Scroll/Grid/L1");
                var current = screen.transform.Find("Column/Scroll/Grid/L5");
                var locked = screen.transform.Find("Column/Scroll/Grid/L6");
                Assert.That(cleared, Is.Not.Null);
                Assert.That(current, Is.Not.Null);
                Assert.That(locked, Is.Not.Null);

                var clearedThumb = cleared.Find("Thumb").GetComponent<RawImage>();
                Assert.That(clearedThumb.gameObject.activeSelf, Is.True);
                Assert.That(clearedThumb.texture, Is.EqualTo(thumb));
                Assert.That(cleared.Find("Overlay/Stars/Star0"), Is.Not.Null);
                Assert.That(cleared.Find("Overlay/Lock").gameObject.activeSelf, Is.False);
                Assert.That(cleared.Find("Overlay/Number").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("1"));
                Assert.That(cleared.GetComponent<Button>().interactable, Is.True);

                Assert.That(current.Find("Overlay/Stars").gameObject.activeSelf, Is.True);
                Assert.That(current.Find("Overlay/Lock").gameObject.activeSelf, Is.False);
                Assert.That(current.Find("Overlay/Number").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("5"));

                Assert.That(locked.Find("Overlay/Lock").gameObject.activeSelf, Is.True);
                Assert.That(locked.Find("Overlay/Stars").gameObject.activeSelf, Is.False);
                Assert.That(locked.GetComponent<Button>().interactable, Is.False);

                var fallback = screen.transform.Find("Column/Scroll/Grid/L2/Thumb").GetComponent<RawImage>();
                var defaultPhoto = Resources.Load<Texture2D>(LevelTileCard.DefaultTilePhotoResource);
                Assert.That(fallback.gameObject.activeSelf, Is.True);
                Assert.That(fallback.texture, Is.EqualTo(defaultPhoto));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void MissingMosaicShowsDefaultTilePhoto()
        {
            var defaultPhoto = Resources.Load<Texture2D>(LevelTileCard.DefaultTilePhotoResource);
            Assert.That(defaultPhoto, Is.Not.Null, "Expected Resources/Photo/tilethumnail");

            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var thumb = screen.transform.Find("Column/Scroll/Grid/L1/Thumb").GetComponent<RawImage>();
                Assert.That(thumb.gameObject.activeSelf, Is.True);
                Assert.That(thumb.texture, Is.EqualTo(defaultPhoto));
                Assert.That(thumb.color, Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void UnlockedCardInvokesOnPickAndBackInvokesOnBack()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            var picked = 0;
            var backed = 0;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.OnPick = n => picked = n;
                payload.OnBack = () => backed++;
                screen.Bind(payload);

                screen.transform.Find("Column/Scroll/Grid/L5").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Scroll/Grid/L6").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Header/Back").GetComponent<Button>().onClick.Invoke();

                Assert.That(picked, Is.EqualTo(5));
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

            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                screen.transform.SetParent(canvasRect, false);
                UiDraw.Stretch(screen.GetComponent<RectTransform>());
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column") as RectTransform;
                Assert.That(col.rect.width, Is.EqualTo(1080f).Within(1f));
                Assert.That(screen.transform.Find("Bg").GetComponent<RawImage>().texture, Is.Not.Null);

                var grid = col.Find("Scroll/Grid").GetComponent<GridLayoutGroup>();
                Assert.That(grid.cellSize.x, Is.GreaterThan(100f));
                Assert.That(grid.constraintCount, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void BindClonesHiddenTemplateAndKeepsAuthoredTweaks()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                var template = screen.transform.Find("Templates/LevelTile");
                var badge = new GameObject("Badge", typeof(RectTransform));
                badge.transform.SetParent(template, false);

                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                Assert.That(screen.transform.Find("Templates/LevelTile"), Is.SameAs(template));
                Assert.That(template.gameObject.activeSelf, Is.False);
                Assert.That(template.Find("Badge"), Is.Not.Null);
                Assert.That(screen.transform.Find("Column/Scroll/Grid/LevelTile"), Is.Null);

                var clone = screen.transform.Find("Column/Scroll/Grid/L1");
                Assert.That(clone, Is.Not.Null);
                Assert.That(clone.GetComponent<LevelTileCard>(), Is.Not.Null);
                Assert.That(clone.Find("Badge"), Is.Not.Null);
                Assert.That(clone.Find("Overlay/Lock"), Is.Not.Null);
                Assert.That(clone.Find("Mark"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ResourcePrefabBindClonesAuthoredLevelTile()
        {
            var prefab = Resources.Load<JourneyLevelSelectScreen>(MemoryPathUi.JourneyLevelSelectResourcePath);
            if (prefab == null)
                Assert.Ignore("Journey level select prefab has not been baked yet.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var template = instance.transform.Find("Templates/LevelTile");
                Assert.That(template, Is.Not.Null);

                instance.gameObject.SetActive(true);
                instance.Bind(SamplePayload());

                Assert.That(instance.transform.Find("Templates/LevelTile"), Is.Not.Null);
                Assert.That(instance.transform.Find("Templates/LevelTile").gameObject.activeSelf, Is.False);

                var clone = instance.transform.Find("Column/Scroll/Grid/L1");
                Assert.That(clone, Is.Not.Null);
                Assert.That(clone.GetComponent<LevelTileCard>(), Is.Not.Null);
                Assert.That(clone.Find("Thumb"), Is.Not.Null);
                Assert.That(clone.Find("Overlay/Lock"), Is.Not.Null);
                Assert.That(clone.Find("Overlay/Stars/Star0"), Is.Not.Null);
                Assert.That(clone.Find("Overlay/Number"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        [Test]
        public void BindDoesNotRewritePrefabLayoutOrArt()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                var col = screen.transform.Find("Column") as RectTransform;
                var layout = col.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(9, 8, 7, 6);
                var grid = screen.transform.Find("Column/Scroll/Grid").GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(140f, 160f);
                var bg = screen.transform.Find("Bg").GetComponent<RawImage>();
                var marker = Texture2D.whiteTexture;
                bg.texture = marker;

                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                Assert.That(layout.padding.left, Is.EqualTo(9));
                Assert.That(grid.cellSize, Is.EqualTo(new Vector2(140f, 160f)));
                Assert.That(bg.texture, Is.SameAs(marker));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void FirstOpenIgnoresTinyUnbuiltViewport()
        {
            var screen = JourneyLevelSelectScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                var scroll = screen.transform.Find("Column/Scroll") as RectTransform;
                scroll.anchorMin = scroll.anchorMax = new Vector2(0.5f, 0.5f);
                scroll.sizeDelta = new Vector2(100f, 100f);
                screen.Bind(SamplePayload());

                var grid = screen.transform.Find("Column/Scroll/Grid").GetComponent<GridLayoutGroup>();
                Assert.That(grid.cellSize.x, Is.GreaterThan(120f));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        static JourneyLevelSelectPayload SamplePayload(Texture thumbnail = null)
        {
            return new JourneyLevelSelectPayload
            {
                Title = "Tile Arena",
                StarsEarned = 12,
                StarsMax = 48,
                Tiles = new[]
                {
                    new JourneyLevelTileInfo
                    {
                        Number = 1,
                        Lane = LevelLane.Cleared,
                        Stars = 3,
                        Thumbnail = thumbnail
                    },
                    new JourneyLevelTileInfo { Number = 2, Lane = LevelLane.Cleared, Stars = 3 },
                    new JourneyLevelTileInfo { Number = 5, Lane = LevelLane.Current, Stars = 0 },
                    new JourneyLevelTileInfo { Number = 6, Lane = LevelLane.Locked, Stars = 0 }
                }
            };
        }
    }
}
