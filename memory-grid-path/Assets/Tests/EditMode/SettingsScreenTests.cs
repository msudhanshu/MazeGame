using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class SettingsScreenTests
    {
        [Test]
        public void BindPaintsFigmaCopyAndToggles()
        {
            var screen = SettingsScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column");
                Assert.That(col, Is.Not.Null);
                Assert.That(
                    col.Find("Header/Title").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.TitleCopy));
                Assert.That(
                    col.Find("System/Heading").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.SystemCopy));
                Assert.That(
                    col.Find("System/Sfx/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Sound Effects"));
                Assert.That(
                    col.Find("System/Sfx/Copy/B").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Cute chime noises when stepping"));
                Assert.That(
                    col.Find("System/Music/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Relaxing Music"));
                Assert.That(
                    col.Find("System/Haptics/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Tactile Vibration"));
                Assert.That(
                    col.Find("System/PathDrag/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo("Slide Along Path"));

                Assert.That(col.Find("System/Sfx/Track").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.Ocean));
                Assert.That(col.Find("System/Haptics/Track").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.LockedTile));
                Assert.That(col.Find("System/PathDrag/Track").GetComponent<Image>().color, Is.EqualTo(MemoryPathPalette.Ocean));
                Assert.That(col.Find("Help"), Is.Not.Null);
                Assert.That(col.Find("Help").gameObject.activeSelf, Is.True);
                Assert.That(col.Find("Help/ReplayTutorial/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.ReplayTutorialCopy));
                Assert.That(col.Find("Help/ReplayOpening/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.OpeningReminderCopy));
                Assert.That(col.Find("Themes"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void FooterShowsStudioBrandAndContact()
        {
            var screen = SettingsScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var footer = screen.transform.Find("Column/Footer");
                Assert.That(
                    footer.Find("Brand/Names/Studio").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.StudioCopy));
                Assert.That(
                    footer.Find("Brand/Names/Tag").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.StudioTagCopy));
                Assert.That(
                    footer.Find("Contact/Cap").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.ContactCopy));
                Assert.That(
                    footer.Find("Contact/Email").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.EmailCopy));
                Assert.That(
                    footer.Find("Copyright").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.CopyrightCopy));
                Assert.That(footer.Find("Brand/Logo/Mark"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void TogglesAndBackInvokePayload()
        {
            var screen = SettingsScreen.CreateTemplate();
            var sfx = 0;
            var music = 0;
            var haptics = 0;
            var pathDrag = 0;
            var backed = 0;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.OnSoundEffects = _ => sfx++;
                payload.OnMusic = _ => music++;
                payload.OnHaptics = _ => haptics++;
                payload.OnPathDrag = _ => pathDrag++;
                payload.OnBack = () => backed++;
                screen.Bind(payload);

                screen.transform.Find("Column/System/Sfx").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/System/Music").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/System/Haptics").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/System/PathDrag").GetComponent<Button>().onClick.Invoke();
                screen.transform.Find("Column/Header/Back").GetComponent<Button>().onClick.Invoke();

                Assert.That(sfx, Is.EqualTo(1));
                Assert.That(music, Is.EqualTo(1));
                Assert.That(haptics, Is.EqualTo(1));
                Assert.That(pathDrag, Is.EqualTo(1));
                Assert.That(backed, Is.EqualTo(1));

                Assert.That(screen.TryHandleBack(), Is.True);
                Assert.That(backed, Is.EqualTo(2));
                Assert.That(payload.SoundEffects, Is.False);
                Assert.That(payload.Haptics, Is.True);
                Assert.That(payload.PathDrag, Is.False);
                Assert.That(
                    screen.transform.Find("Column/System/Sfx/Track").GetComponent<Image>().color,
                    Is.EqualTo(MemoryPathPalette.LockedTile));
                Assert.That(
                    screen.transform.Find("Column/System/PathDrag/Track").GetComponent<Image>().color,
                    Is.EqualTo(MemoryPathPalette.LockedTile));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void ReplayTutorialRowInvokesPayload()
        {
            var screen = SettingsScreen.CreateTemplate();
            var replayed = 0;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.OnReplayTutorial = () => replayed++;
                screen.Bind(payload);

                var help = screen.transform.Find("Column/Help");
                Assert.That(help.gameObject.activeSelf, Is.True);
                Assert.That(
                    help.Find("Heading").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.HelpCopy));
                Assert.That(
                    help.Find("ReplayTutorial/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.ReplayTutorialCopy));
                help.Find("ReplayTutorial").GetComponent<Button>().onClick.Invoke();
                Assert.That(replayed, Is.EqualTo(1));
                Assert.That(help.Find("ReplayOpening").gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void OpeningReminderRowInvokesPayload()
        {
            var screen = SettingsScreen.CreateTemplate();
            var opened = 0;
            try
            {
                screen.gameObject.SetActive(true);
                var payload = SamplePayload();
                payload.OnReplayOpening = () => opened++;
                screen.Bind(payload);

                var help = screen.transform.Find("Column/Help");
                Assert.That(help.gameObject.activeSelf, Is.True);
                Assert.That(
                    help.Find("ReplayOpening/Copy/T").GetComponent<TextMeshProUGUI>().text,
                    Is.EqualTo(SettingsScreen.OpeningReminderCopy));
                help.Find("ReplayOpening").GetComponent<Button>().onClick.Invoke();
                Assert.That(opened, Is.EqualTo(1));
                Assert.That(help.Find("ReplayOpening").gameObject.activeSelf, Is.True);
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

            var screen = SettingsScreen.CreateTemplate();
            try
            {
                screen.transform.SetParent(canvasRect, false);
                UiDraw.Stretch(screen.GetComponent<RectTransform>());
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                var col = screen.transform.Find("Column") as RectTransform;
                Assert.That(col.rect.width, Is.EqualTo(1080f).Within(1f));
                Assert.That(screen.transform.Find("Bg").GetComponent<RawImage>().texture, Is.Not.Null);
                Assert.That(col.Find("Footer").GetComponent<RectTransform>().anchoredPosition.y, Is.LessThan(-200f));
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void BindDoesNotRewritePrefabLayoutOrArt()
        {
            var screen = SettingsScreen.CreateTemplate();
            try
            {
                var col = screen.transform.Find("Column") as RectTransform;
                var layout = col.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(4, 5, 6, 7);
                var bg = screen.transform.Find("Bg").GetComponent<RawImage>();
                var marker = Texture2D.whiteTexture;
                bg.texture = marker;

                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());

                Assert.That(layout.padding.left, Is.EqualTo(4));
                Assert.That(bg.texture, Is.SameAs(marker));
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void BindOnResourcesPrefabCreatesMissingPathDragRow()
        {
            var prefab = Resources.Load<SettingsScreen>(MemoryPathUi.SettingsResourcePath);
            Assert.That(prefab, Is.Not.Null);
            var screen = Object.Instantiate(prefab);
            try
            {
                screen.gameObject.SetActive(true);
                Assert.DoesNotThrow(() => screen.Bind(SamplePayload()));
                Assert.That(screen.transform.Find("Column/System/PathDrag/Track"), Is.Not.Null);
                Assert.That(
                    screen.transform.Find("Column/System/PathDrag/Track").GetComponent<Image>().color,
                    Is.EqualTo(MemoryPathPalette.Ocean));
                var help = screen.transform.Find("Column/Help");
                Assert.That(help, Is.Not.Null);
                Assert.That(help.gameObject.activeSelf, Is.True);
                Assert.That(help.GetSiblingIndex(), Is.LessThan(screen.transform.Find("Column/Footer").GetSiblingIndex()));
                Assert.That(help.Find("ReplayTutorial"), Is.Not.Null);
                Assert.That(help.Find("ReplayOpening"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        [Test]
        public void SoundToggleTrackStaysACompactSwitch()
        {
            var screen = SettingsScreen.CreateTemplate();
            try
            {
                screen.gameObject.SetActive(true);
                screen.Bind(SamplePayload());
                var col = screen.transform.Find("Column") as RectTransform;
                LayoutRebuilder.ForceRebuildLayoutImmediate(col);
                var track = screen.transform.Find("Column/System/Sfx/Track") as RectTransform;
                var fit = track.GetComponent<LayoutElement>();
                Assert.That(fit.flexibleWidth, Is.EqualTo(0f));
                Assert.That(fit.preferredWidth, Is.EqualTo(MemoryPathMenus.S(SettingsScreen.ToggleTrackWidth)).Within(0.5f));
                Assert.That(track.rect.width, Is.LessThan(120f));
                Assert.That(
                    screen.transform.Find("Column/System/Sfx").GetComponent<HorizontalLayoutGroup>().childForceExpandWidth,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(screen.gameObject);
            }
        }

        static SettingsPayload SamplePayload()
        {
            return new SettingsPayload
            {
                SoundEffects = true,
                Music = true,
                Haptics = false,
                PathDrag = true,
                ThemeId = PlayerSettingsStore.OceanTheme
            };
        }
    }
}
