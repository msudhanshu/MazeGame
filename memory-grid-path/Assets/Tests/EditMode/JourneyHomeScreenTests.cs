using Game.Core.State;
using Game.Unity.Ui;
using Nixin.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class JourneyHomeScreenTests
    {
        [Test]
        public void BindClonesIconCardForEachMode()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());
                var tile = HomeFind(screen, "Modes/Panel/Row/TileArena");
                Assert.That(tile, Is.Not.Null);
                Assert.That(tile.GetComponent<IconCard>(), Is.Not.Null);
                Assert.That(tile.Find("Art/Glyph"), Is.Not.Null);
                Assert.That(tile.Find("LockOverlay"), Is.Not.Null);
                Assert.That(tile.Find("Highlight"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void LayoutScalesFromFigmaPhoneFrame()
        {
            Assert.That(JourneyHomeScreen.S(402), Is.EqualTo(720f).Within(0.01f));
            Assert.That(JourneyHomeScreen.S(24), Is.EqualTo(720f * 24f / 402f).Within(0.01f));
        }

        [Test]
        public void BindPaintsFigmaCopyStatsAndChrome()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column");
                Assert.That(col, Is.Not.Null);

                var title = col.Find("Brand/Title").GetComponent<TextMeshProUGUI>();
                Assert.That(title.text, Is.EqualTo(JourneyHomeScreen.TitleCopy));
                Assert.That(title.color, Is.EqualTo(MemoryPathPalette.HomeInk));

                var tag = col.Find("Brand/Tag").GetComponent<TextMeshProUGUI>();
                Assert.That(tag.text, Is.EqualTo(JourneyHomeScreen.TagCopy));

                var modeTitle = HomeFind(screen, "GameMode/Title").GetComponent<TextMeshProUGUI>();
                Assert.That(modeTitle.text, Is.EqualTo("Tile Arena"));

                Assert.That(col.Find("Stats/LEVEL/Val").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("3"));
                Assert.That(col.Find("Stats/STARS/Val").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("6/9"));
                Assert.That(col.Find("Stats/STARS/Val").GetComponent<TextMeshProUGUI>().color, Is.EqualTo(MemoryPathPalette.HomeStar));
                Assert.That(col.Find("Stats/SCORE/Val").GetComponent<TextMeshProUGUI>().text, Is.EqualTo(4375.ToString("N0")));
                Assert.That(col.Find("Stats/SCORE/Val").GetComponent<TextMeshProUGUI>().color, Is.EqualTo(MemoryPathPalette.HomeScore));

                Assert.That(HomeFind(screen, "Hero/Play/Stack/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("PLAY"));
                Assert.That(col.Find("LevelWrap/Levels/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo(JourneyHomeScreen.LevelsCopy));
                Assert.That(HomeFind(screen, "Modes/Heading").GetComponent<TextMeshProUGUI>().text, Is.EqualTo(JourneyHomeScreen.ModesCopy));
                Assert.That(HomeFind(screen, "Modes/Heading").GetComponent<TextMeshProUGUI>().color, Is.EqualTo(MemoryPathPalette.HomeInk));
                Assert.That(
                    HomeFind(screen, "Modes/Heading").GetComponent<TextMeshProUGUI>().fontSize,
                    Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.ModeHeadingSize)).Within(0.01f));
                Assert.That(col.Find("Header/Volume"), Is.Not.Null);
                Assert.That(col.Find("Header/Settings"), Is.Not.Null);
                var volume = col.Find("Header/Volume").GetComponent<LayoutElement>();
                Assert.That(volume.preferredWidth, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.HeaderButtonSize)).Within(0.01f));
                Assert.That(volume.preferredHeight, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.HeaderButtonSize)).Within(0.01f));
                var settings = col.Find("Header/Settings").GetComponent<LayoutElement>();
                Assert.That(settings.preferredWidth, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.HeaderButtonSize)).Within(0.01f));
                var volumeIcon = col.Find("Header/Volume/Icon") as RectTransform;
                Assert.That(volumeIcon.sizeDelta.x, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.HeaderIconSize)).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ModeCardsSitInFullWidthFrostPanel()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var panel = HomeFind(screen, "Modes/Panel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.HomeModePanel));
                Assert.That(panel.GetComponent<Outline>().effectColor, Is.EqualTo(MemoryPathPalette.HomeFrostBorder));

                var row = panel.Find("Row").GetComponent<HorizontalLayoutGroup>();
                Assert.That(row.childForceExpandWidth, Is.True);
                Assert.That(row.childForceExpandHeight, Is.False);

                var rowFit = panel.Find("Row").GetComponent<LayoutElement>();
                Assert.That(rowFit.flexibleWidth, Is.EqualTo(1f));

                var tile = panel.Find("Row/TileArena");
                var fit = tile.GetComponent<LayoutElement>();
                Assert.That(fit.flexibleWidth, Is.EqualTo(1f));
                Assert.That(fit.flexibleHeight, Is.EqualTo(0f));
                Assert.That(fit.preferredWidth, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.ModeCardWidth)).Within(0.01f));
                Assert.That(fit.preferredHeight, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.ModeCardHeight)).Within(0.01f));
                Assert.That(tile.localScale, Is.EqualTo(Vector3.one));
                var cardImage = tile.GetComponent<Image>();
                Assert.That(cardImage.sprite, Is.EqualTo(UiDraw.Rounded));
                Assert.That(cardImage.type, Is.EqualTo(Image.Type.Sliced));
                Assert.That(
                    cardImage.pixelsPerUnitMultiplier,
                    Is.EqualTo(36f / JourneyHomeScreen.S(JourneyHomeScreen.ModeCardCorner)).Within(0.01f));

                var template = screen.transform.Find("Templates/IconCard");
                Assert.That(template, Is.Not.Null);
                Assert.That(template.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ModeCardsMatchSelectedAndLockedFigmaStates()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var tile = HomeFind(screen, "Modes/Panel/Row/TileArena");
                var graph = HomeFind(screen, "Modes/Panel/Row/GraphArena");
                var cozy = HomeFind(screen, "Modes/Panel/Row/ScoutArena");
                Assert.That(tile.Find("Text/Cap").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Tile Arena"));
                Assert.That(graph.Find("Text/Cap").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Graph Arena"));
                Assert.That(cozy.Find("Text/Cap").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Cozy Path"));

                Assert.That(tile.GetComponent<IconCard>(), Is.Not.Null);
                Assert.That(tile.GetComponent<IconCard>().Caption, Is.EqualTo("Tile Arena"));
                Assert.That(tile.Find("Highlight").gameObject.activeSelf, Is.False);
                Assert.That(graph.Find("Highlight").gameObject.activeSelf, Is.False);
                Assert.That(tile.GetComponent<Outline>().effectColor, Is.EqualTo(MemoryPathPalette.HomeModeSelectedRing));
                Assert.That(
                    tile.GetComponent<Outline>().effectDistance.x,
                    Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.SelectedModeOutline)).Within(0.01f));
                Assert.That(tile.GetComponent<Outline>().useGraphicAlpha, Is.False);
                Assert.That(graph.GetComponent<Outline>().effectColor, Is.EqualTo(MemoryPathPalette.HomeFrostBorder));
                Assert.That(tile.GetComponent<Mask>().enabled, Is.False);
                Assert.That(graph.GetComponent<Mask>().enabled, Is.True);
                Assert.That(cozy.Find("LockOverlay"), Is.Not.Null);
                Assert.That(cozy.Find("LockOverlay").gameObject.activeSelf, Is.True);
                Assert.That(tile.Find("LockOverlay").gameObject.activeSelf, Is.False);
                Assert.That(tile.Find("Art").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.HomeModeSelected));
                Assert.That(graph.Find("Art").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.HomeGraph));
                Assert.That(cozy.Find("Art").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.HomeLockedHeader));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ButtonsInvokePayloadActions()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            var play = 0;
            var levels = 0;
            var settings = 0;
            var volume = 0;
            GameModeId? picked = null;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.OnPlay = () => play++;
                payload.OnLevels = () => levels++;
                payload.OnSettings = () => settings++;
                payload.OnVolume = () => volume++;
                payload.OnSelectMode = id => picked = id;
                screen.Bind(payload);

                HomeFind(screen, "Hero/Play").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/LevelWrap/Levels").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Header/Settings").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Header/Volume").GetComponent<Button>().onClick.Invoke();
                HomeFind(screen, "Modes/Panel/Row/GraphArena").GetComponent<Button>().onClick.Invoke();

                Assert.That(play, Is.EqualTo(1));
                Assert.That(levels, Is.EqualTo(1));
                Assert.That(settings, Is.EqualTo(1));
                Assert.That(volume, Is.EqualTo(1));
                Assert.That(picked, Is.EqualTo(GameModeId.GraphArena));
                Assert.That(payload.SoundOn, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ModeCardUsesPayloadIconWhenProvided()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.Modes[0].Icon = sprite;
                screen.Bind(payload);

                var glyph = HomeFind(screen, "Modes/Panel/Row/TileArena/Art/Glyph").GetComponent<Image>();
                Assert.That(glyph.sprite, Is.SameAs(sprite));
                var cover = glyph.GetComponent<AspectRatioFitter>();
                Assert.That(cover, Is.Not.Null);
                Assert.That(cover.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
                Assert.That(glyph.transform.parent.GetComponent<RectMask2D>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void GameModeTitleShowsSelectedModeBelowPlayButton()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                screen.Bind(payload);

                var modeTitleTransform = HomeFind(screen, "GameMode/Title");
                Assert.That(modeTitleTransform, Is.Not.Null);

                var modeTitleText = modeTitleTransform.GetComponent<TextMeshProUGUI>();
                Assert.That(modeTitleText.text, Is.EqualTo("Tile Arena"));
                Assert.That(modeTitleText.fontSize, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.ModeTitleSize)).Within(0.01f));
                Assert.That(screen.ModeTitle, Is.SameAs(modeTitleText));

                // Switch selected mode in payload to Graph Arena
                payload.Modes[0].Selected = false;
                payload.Modes[1].Selected = true;
                screen.Bind(payload);

                Assert.That(modeTitleText.text, Is.EqualTo("Graph Arena"));

                // Switch selected mode in payload to Cozy Path
                payload.Modes[1].Selected = false;
                payload.Modes[2].Selected = true;
                screen.Bind(payload);

                Assert.That(modeTitleText.text, Is.EqualTo("Cozy Path"));

                // When explicit SelectedModeTitle is set on payload
                payload.SelectedModeTitle = "Custom Mode";
                screen.Bind(payload);

                Assert.That(modeTitleText.text, Is.EqualTo("Custom Mode"));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void BindKeepsHomeStackOrderAcrossModes()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                screen.Bind(payload);
                AssertHomeStack(screen);

                payload.Modes[0].Selected = false;
                payload.Modes[1].Selected = true;
                payload.SelectedModeTitle = "Graph Arena";
                screen.Bind(payload);
                AssertHomeStack(screen);

                payload.Modes[1].Selected = false;
                payload.Modes[2].Selected = true;
                payload.SelectedModeTitle = "Scout Arena";
                screen.Bind(payload);
                AssertHomeStack(screen);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        static void AssertHomeStack(JourneyHomeScreen screen)
        {
            var col = screen.transform.Find("Column");
            var brand = col.Find("Brand");
            var chip = col.Find("LevelWrap");
            var stats = col.Find("Stats");
            var body = col.Find(JourneyHomeScreen.BodyName);
            var content = body.Find(JourneyHomeScreen.BodyContentName) ?? body;
            Assert.That(body.GetSiblingIndex(), Is.EqualTo(brand.GetSiblingIndex() + 1));
            Assert.That(chip.GetSiblingIndex(), Is.EqualTo(body.GetSiblingIndex() + 1));
            Assert.That(stats.GetSiblingIndex(), Is.EqualTo(chip.GetSiblingIndex() + 1));
            Assert.That(content.Find("GameMode").GetSiblingIndex(), Is.EqualTo(content.Find("Hero").GetSiblingIndex() + 1));
            Assert.That(content.Find("Modes").GetSiblingIndex(), Is.EqualTo(content.Find("GameMode").GetSiblingIndex() + 1));
        }

        [Test]
        public void ModeTitleBounceScaleOvershootsThenSettles()
        {
            Assert.That(JourneyHomeScreen.ModeTitleBounceScale(0f), Is.EqualTo(0f).Within(0.05f));
            Assert.That(JourneyHomeScreen.ModeTitleBounceScale(1f), Is.EqualTo(1f).Within(0.01f));
            Assert.That(JourneyHomeScreen.ModeTitleBounceScale(0.75f), Is.GreaterThan(1f));
        }

        static JourneyHomePayload SamplePayload()
        {
            return new JourneyHomePayload
            {
                Level = 3,
                StarsEarned = 6,
                StarsMax = 9,
                CareerScore = 4375,
                SoundOn = true,
                Modes = new[]
                {
                    new JourneyModeIconInfo
                    {
                        Id = GameModeId.TileArena,
                        Title = "Tile Arena",
                        Unlocked = true,
                        Selected = true,
                        Tint = MemoryPathPalette.Teal
                    },
                    new JourneyModeIconInfo
                    {
                        Id = GameModeId.GraphArena,
                        Title = "Graph Arena",
                        Unlocked = true,
                        Selected = false,
                        Tint = MemoryPathPalette.HomeGraph
                    },
                    new JourneyModeIconInfo
                    {
                        Id = GameModeId.ScoutArena,
                        Title = "Cozy Path",
                        Unlocked = false,
                        Selected = false,
                        Tint = MemoryPathPalette.Mascot
                    }
                }
            };
        }

        [Test]
        public void LayoutStacksBrandHeroChipAndStatsWithoutOverlap()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1080f, 1920f);

            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.transform.SetParent(canvasRect, false);
                UiDraw.Stretch(screen.GetComponent<RectTransform>());
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column") as RectTransform;
                Assert.That(col, Is.Not.Null);
                Assert.That(col.rect.width, Is.EqualTo(1080f).Within(1f));

                var brand = col.Find("Brand") as RectTransform;
                var body = col.Find(JourneyHomeScreen.BodyName) as RectTransform;
                var gameMode = HomeFind(screen, "GameMode") as RectTransform;
                var hero = HomeFind(screen, "Hero") as RectTransform;
                var modes = HomeFind(screen, "Modes") as RectTransform;
                var chip = col.Find("LevelWrap") as RectTransform;
                var stats = col.Find("Stats") as RectTransform;
                var modesPanel = HomeFind(screen, "Modes/Panel") as RectTransform;
                Assert.That(brand.rect.height, Is.GreaterThan(80f));
                Assert.That(gameMode, Is.Not.Null);
                Assert.That(gameMode.rect.height, Is.GreaterThan(20f));
                Assert.That(hero.rect.height, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.HeroHeight)).Within(2f));
                Assert.That(WorldTop(body), Is.LessThan(WorldBottom(brand) + 1f));
                Assert.That(WorldTop(hero), Is.LessThan(WorldBottom(brand) + 1f));
                Assert.That(WorldTop(gameMode), Is.LessThan(WorldBottom(hero) + 1f));
                Assert.That(WorldTop(modes), Is.LessThan(WorldBottom(gameMode) + 1f));
                Assert.That(WorldTop(chip), Is.LessThan(WorldBottom(modes) + 1f));
                Assert.That(WorldTop(stats), Is.LessThan(WorldBottom(chip) + 1f));
                Assert.That(stats.rect.width, Is.GreaterThan(900f));
                Assert.That(modesPanel, Is.Not.Null);
                Assert.That(modesPanel.rect.width, Is.EqualTo(stats.rect.width).Within(1f));

                var play = HomeFind(screen, "Hero/Play").GetComponent<Image>();
                Assert.That(play.sprite, Is.Not.Null);
                Assert.That(play.rectTransform.sizeDelta.x, Is.EqualTo(JourneyHomeScreen.S(JourneyHomeScreen.PlayButtonSize)).Within(1f));
                Assert.That(
                    screen.transform.Find("Bg").GetComponent<RawImage>().texture,
                    Is.Not.Null);
                Assert.That(
                    col.Find("Brand/Title").GetComponent<TextMeshProUGUI>().overflowMode,
                    Is.EqualTo(TextOverflowModes.Overflow));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void BindDoesNotRewritePrefabLayoutOrArt()
        {
            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                var col = screen.transform.Find("Column") as RectTransform;
                var layout = col.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(11, 22, 33, 44);
                col.anchoredPosition = new Vector2(6f, -9f);
                var bg = screen.transform.Find("Bg").GetComponent<RawImage>();
                var marker = Texture2D.whiteTexture;
                bg.texture = marker;

                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                Assert.That(layout.padding.left, Is.EqualTo(11));
                Assert.That(layout.padding.top, Is.EqualTo(33));
                Assert.That(col.anchoredPosition, Is.EqualTo(new Vector2(6f, -9f)));
                Assert.That(bg.texture, Is.SameAs(marker));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ResourcePrefabBindsWithoutRebuildWhenPresent()
        {
            var prefab = Resources.Load<JourneyHomeScreen>(MemoryPathUi.JourneyHomeResourcePath);
            if (prefab == null)
                Assert.Ignore("Journey home prefab has not been baked yet.");

            var instance = Object.Instantiate(prefab);
            try
            {
                instance.gameObject.SetActive(true);
                instance.Bind(SamplePayload());
                Assert.That(instance.transform.Find("Column/Brand/Title"), Is.Not.Null);
                Assert.That(
                    instance.transform.Find("Column/Brand/Title").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(JourneyHomeScreen.TitleCopy));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        [Test]
        public void StatsStayVisibleOnShortPhoneFrame()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 1280f);

            var screen = JourneyHomeScreen.CreateTemplate();
            try
            {
                screen.transform.SetParent(canvasRect, false);
                UiDraw.Stretch(screen.GetComponent<RectTransform>());
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var screenRect = screen.GetComponent<RectTransform>();
                var stats = screen.transform.Find("Column/Stats") as RectTransform;
                var scroll = screen.GetComponent<ScrollRect>();
                Assert.That(stats, Is.Not.Null);
                Assert.That(scroll, Is.Not.Null);
                scroll.verticalNormalizedPosition = 0f;
                Canvas.ForceUpdateCanvases();
                Assert.That(WorldBottom(stats), Is.GreaterThan(WorldBottom(screenRect) - 1f));
                Assert.That(WorldTop(stats), Is.LessThan(WorldTop(screenRect) + 1f));
                Assert.That(WorldBottom(stats), Is.GreaterThan(WorldBottom(screenRect) + 8f));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        static Transform HomeFind(Component screen, string path)
        {
            return screen.transform.Find(
                    "Column/" + JourneyHomeScreen.BodyName + "/" + JourneyHomeScreen.BodyContentName + "/" + path)
                ?? screen.transform.Find("Column/" + JourneyHomeScreen.BodyName + "/" + path)
                ?? screen.transform.Find("Column/" + path);
        }

        static float WorldTop(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var top = corners[0].y;
            for (var i = 1; i < 4; i++)
                top = Mathf.Max(top, corners[i].y);
            return top;
        }

        static float WorldBottom(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var bottom = corners[0].y;
            for (var i = 1; i < 4; i++)
                bottom = Mathf.Min(bottom, corners[i].y);
            return bottom;
        }
    }
}
