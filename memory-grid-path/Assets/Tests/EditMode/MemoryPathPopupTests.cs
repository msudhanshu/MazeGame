using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class MemoryPathPopupTests
    {
        [Test]
        public void PlayLevelLabelUsesTheLevelNumber()
        {
            Assert.That(MemoryPathPopups.PlayLevelLabel(4), Is.EqualTo("Play Level 4"));
            Assert.That(MemoryPathPopups.PlayLevelLabel(0), Is.EqualTo("Play Tutorial"));
        }

        [Test]
        public void ExitPayloadMatchesFigmaCopyAndChrome()
        {
            var payload = MemoryPathPopups.Exit(5, null, null);
            Assert.That(payload.Chrome, Is.EqualTo(MemoryPathPopupChrome.Exit));
            Assert.That(payload.Panel, Is.EqualTo(MemoryPathPalette.PopupCream));
            Assert.That(payload.Kicker, Is.EqualTo("Exit Confirmation"));
            Assert.That(payload.Title, Is.EqualTo("Leaving so soon?"));
            Assert.That(payload.Message, Does.Contain("Level 5"));
            Assert.That(payload.Buttons, Has.Length.EqualTo(2));
            Assert.That(payload.Buttons[0].Label, Is.EqualTo("Stay"));
            Assert.That(payload.Buttons[0].Fill, Is.EqualTo(MemoryPathPalette.Stay));
            Assert.That(payload.Buttons[0].Border, Is.EqualTo(MemoryPathPalette.StayBorder));
            Assert.That(payload.Buttons[1].Label, Is.EqualTo("Leave"));
            Assert.That(payload.Buttons[1].Fill, Is.EqualTo(MemoryPathPalette.Leave));
        }

        [Test]
        public void CompletePayloadUsesGoldTitleStarsAndStackedButtons()
        {
            var payload = MemoryPathPopups.Complete(950, true, null, null);
            Assert.That(payload.Chrome, Is.EqualTo(MemoryPathPopupChrome.Complete));
            Assert.That(payload.TitleColor, Is.EqualTo(MemoryPathPalette.TitleGold));
            Assert.That(payload.Stars, Is.EqualTo(3));
            Assert.That(payload.ScoreLine, Is.EqualTo("950 pts"));
            Assert.That(payload.StackButtons, Is.True);
            Assert.That(payload.Buttons[0].Label, Is.EqualTo("Next Level"));
            Assert.That(payload.Buttons[0].Fill, Is.EqualTo(MemoryPathPalette.Next));
            Assert.That(payload.Buttons[1].Label, Is.EqualTo("Play Level 1"));
            Assert.That(payload.Buttons[1].Border, Is.EqualTo(MemoryPathPalette.ReplayBorder));
            Assert.That(payload.Buttons[2].Label, Is.EqualTo("Main Menu"));
            Assert.That(payload.Buttons[2].Height, Is.LessThan(payload.Buttons[0].Height));
            Assert.That(payload.Buttons[2].LabelColor, Is.EqualTo(MemoryPathPalette.HudMuted));
            Assert.That(payload.BurstSparkles, Is.True);
            var two = MemoryPathPopups.Complete(10, true, 4, null, null, null, 2);
            Assert.That(two.Stars, Is.EqualTo(2));
            var last = MemoryPathPopups.Complete(1, false, 4, null, null, null);
            Assert.That(last.Buttons[0].Label, Is.EqualTo("Play Level 4"));
            Assert.That(last.Buttons[1].Label, Is.EqualTo("Main Menu"));
        }

        [Test]
        public void PlayerFacingPopupCopyHasNoEmoji()
        {
            var payloads = new[]
            {
                MemoryPathPopups.Exit(1, null, null),
                MemoryPathPopups.Complete(10, true, null, null),
                MemoryPathPopups.Complete(10, false, null, null),
                MemoryPathPopups.GameOver(null, null),
                MemoryPathPopups.Pause(null, null, null),
                MemoryPathPopups.Skip(5, null, null),
                MemoryPathPopups.Notice("Hang on", "Body", "OK", null)
            };

            for (var i = 0; i < payloads.Length; i++)
                AssertNoEmoji(payloads[i]);
        }

        static void AssertNoEmoji(MemoryPathPopupPayload payload)
        {
            AssertNoEmoji(payload.Kicker);
            AssertNoEmoji(payload.Title);
            AssertNoEmoji(payload.Message);
            AssertNoEmoji(payload.ScoreLabel);
            AssertNoEmoji(payload.ScoreLine);
            var buttons = payload.Buttons;
            if (buttons == null)
                return;
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    AssertNoEmoji(buttons[i].Label);
            }
        }

        static void AssertNoEmoji(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            for (var i = 0; i < value.Length; i++)
            {
                var code = char.ConvertToUtf32(value, i);
                if (char.IsSurrogate(value[i]))
                    i++;
                Assert.That(IsEmojiCode(code), Is.False, value);
            }
        }

        static bool IsEmojiCode(int code)
        {
            return code >= 0x1F300
                || (code >= 0x2300 && code <= 0x23FF)
                || (code >= 0x2600 && code <= 0x27BF);
        }

        [Test]
        public void FailAndPausePayloadsMatchFigma()
        {
            var fail = MemoryPathPopups.GameOver(null, null);
            Assert.That(fail.Kicker, Is.EqualTo("Game Over"));
            Assert.That(fail.Glyph, Is.EqualTo(MemoryPathGlyph.Heart));
            Assert.That(fail.Panel, Is.EqualTo(MemoryPathPalette.PopupFail));
            Assert.That(fail.Title, Is.EqualTo("You Failed!"));
            Assert.That(fail.StackButtons, Is.True);
            Assert.That(fail.Buttons[0].Label, Is.EqualTo("Play Level 1"));
            Assert.That(fail.Message, Does.Contain("new path"));
            Assert.That(fail.Buttons[1].Label, Is.EqualTo("Main Menu"));
            Assert.That(fail.Buttons[1].Height, Is.LessThan(fail.Buttons[0].Height));
            Assert.That(fail.Buttons[1].LabelColor, Is.EqualTo(MemoryPathPalette.HudMuted));
            Assert.That(fail.MessageSize, Is.GreaterThanOrEqualTo(22f));
            Assert.That(fail.PanelWidth, Is.GreaterThan(MemoryPathPopup.PanelWidth));

            var pause = MemoryPathPopups.Pause(null, null, null);
            Assert.That(pause.Kicker, Is.EqualTo("Game Paused"));
            Assert.That(pause.Glyph, Is.EqualTo(MemoryPathGlyph.None));
            Assert.That(pause.Panel, Is.EqualTo(MemoryPathPalette.PopupPause));
            Assert.That(pause.Title, Is.Empty);
            Assert.That(pause.StackButtons, Is.True);
            Assert.That(pause.Buttons, Has.Length.EqualTo(4));
            Assert.That(pause.Buttons[0].Label, Is.EqualTo("Resume Path"));
            Assert.That(pause.Buttons[1].Label, Is.EqualTo("Restart"));
            Assert.That(pause.Buttons[2].Label, Is.EqualTo("Select Level"));
            Assert.That(pause.Buttons[3].Label, Is.EqualTo("Quit to Menu"));
        }

        [Test]
        public void ExitPopupUsesCompactPanelAndRowButtons()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                popup.gameObject.SetActive(true);
                popup.Bind(MemoryPathPopups.Exit(5, null, null));

                var panel = popup.transform.Find("Panel") as RectTransform;
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.sizeDelta.x, Is.EqualTo(MemoryPathPopup.PanelWidth));

                var kicker = panel.Find("Kicker").GetComponent<TextMeshProUGUI>();
                Assert.That(kicker.text, Is.EqualTo("EXIT CONFIRMATION"));
                Assert.That(kicker.characterSpacing, Is.GreaterThan(0f));

                var title = panel.Find("Body/Title").GetComponent<TextMeshProUGUI>();
                Assert.That(title.text, Is.EqualTo("Leaving so soon?"));
                Assert.That(title.alignment, Is.EqualTo(TextAlignmentOptions.MidlineLeft));

                Assert.That(panel.Find("Row").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Stack").gameObject.activeSelf, Is.False);
                Assert.That(panel.Find("Row/Btn0").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Row/Btn1").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Row/Btn0/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Stay"));
                Assert.That(panel.Find("Body/Glyph").gameObject.activeSelf, Is.False);
                Assert.That(panel.Find("Close").gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void CompletePopupStacksButtonsAndShowsScore()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                popup.gameObject.SetActive(true);
                popup.Bind(MemoryPathPopups.Complete(950, true, null, null));

                var panel = popup.transform.Find("Panel");
                Assert.That(panel.Find("Row").gameObject.activeSelf, Is.False);
                Assert.That(panel.Find("Stack").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Stack/Btn0/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Next Level"));
                Assert.That(panel.Find("Stack/Btn1/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Play Level 1"));
                Assert.That(panel.Find("Stack/Btn2/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Main Menu"));
                Assert.That(
                    panel.Find("Stack/Btn2").GetComponent<LayoutElement>().preferredHeight,
                    Is.LessThan(panel.Find("Stack/Btn0").GetComponent<LayoutElement>().preferredHeight));
                Assert.That(panel.Find("Body/Stars").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Body/ScoreChip").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Body/ScoreChip/Score").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("950 pts"));
                Assert.That(panel.Find("Body/Title").GetComponent<TextMeshProUGUI>().color, Is.EqualTo(MemoryPathPalette.TitleGold));
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void CompletePopupKeepsEmptyStarsDim()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                popup.gameObject.SetActive(true);
                popup.Bind(MemoryPathPopups.Complete(10, true, 4, null, null, null, 2));

                var stars = popup.transform.Find("Panel/Body/Stars");
                Assert.That(stars.gameObject.activeSelf, Is.True);
                var icons = new System.Collections.Generic.List<Image>();
                for (var i = 0; i < stars.childCount; i++)
                {
                    var child = stars.GetChild(i);
                    if (child.name != "StarIcon")
                        continue;
                    Assert.That(child.gameObject.activeSelf, Is.True);
                    icons.Add(child.GetComponent<Image>());
                }

                Assert.That(icons.Count, Is.EqualTo(3));
                Assert.That(icons[0].color.a, Is.EqualTo(1f).Within(0.01f));
                Assert.That(icons[1].color.a, Is.EqualTo(1f).Within(0.01f));
                Assert.That(icons[2].color.a, Is.LessThan(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void FailPopupShowsHeartGlyphAndPauseShowsBars()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                popup.gameObject.SetActive(true);
                popup.Bind(MemoryPathPopups.GameOver(null, null));
                var glyph = popup.transform.Find("Panel/Body/Glyph");
                Assert.That(glyph.gameObject.activeSelf, Is.True);
                Assert.That(glyph.Find("Inner/Heart").gameObject.activeSelf, Is.True);
                Assert.That(glyph.Find("Inner/Pause").gameObject.activeSelf, Is.False);
                Assert.That(popup.transform.Find("Panel/Row").gameObject.activeSelf, Is.False);
                Assert.That(popup.transform.Find("Panel/Stack").gameObject.activeSelf, Is.True);
                Assert.That(popup.transform.Find("Panel/Stack/Btn0/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Play Level 1"));
                Assert.That(popup.transform.Find("Panel/Stack/Btn1/Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("Main Menu"));

                popup.Bind(MemoryPathPopups.Pause(null, null, null));
                Assert.That(glyph.gameObject.activeSelf, Is.False);
                Assert.That(popup.transform.Find("Panel/Body/Title").gameObject.activeSelf, Is.False);
                Assert.That(popup.transform.Find("Panel/Stack/Btn2").gameObject.activeSelf, Is.True);
                Assert.That(popup.transform.Find("Panel/Stack/Btn3").gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void PanelWidthScalesFromFigmaCard()
        {
            Assert.That(MemoryPathPopup.S(322), Is.EqualTo(720f).Within(0.01f));
            Assert.That(MemoryPathPopup.S(20), Is.EqualTo(720f * 20f / 322f).Within(0.01f));
        }

        [Test]
        public void DeviceBackClosesThePopup()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                popup.gameObject.SetActive(true);
                popup.Bind(MemoryPathPopups.Pause(null, null, null));
                Assert.That(popup.HasBackHandler, Is.True);
                Assert.That(popup.TryHandleBack(), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void CreateTemplateHasBuiltLayout()
        {
            var popup = MemoryPathPopup.CreateTemplate();
            try
            {
                Assert.That(popup.HasBuiltLayout, Is.True);
                Assert.That(popup.transform.Find("Panel"), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(popup.gameObject);
            }
        }

        [Test]
        public void ResolvePopupReturnsAPopup()
        {
            var resolved = MemoryPathUi.ResolvePopup();
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.GetComponent<RectTransform>(), Is.Not.Null);
        }

        [Test]
        public void ResolveReturnsEachScreenType()
        {
            Assert.That(
                MemoryPathUi.Resolve<HomeScreen>(null, MemoryPathUi.HomeResourcePath, HomeScreen.CreateTemplate),
                Is.Not.Null);
            Assert.That(
                MemoryPathUi.Resolve<SettingsScreen>(null, MemoryPathUi.SettingsResourcePath, SettingsScreen.CreateTemplate),
                Is.Not.Null);
            Assert.That(
                MemoryPathUi.Resolve<JourneyHomeScreen>(null, MemoryPathUi.JourneyHomeResourcePath, JourneyHomeScreen.CreateTemplate),
                Is.Not.Null);
        }

        [Test]
        public void ResourcePrefabBindsWithoutRebuildWhenPresent()
        {
            var prefab = Resources.Load<MemoryPathPopup>(MemoryPathUi.PopupResourcePath);
            if (prefab == null)
                Assert.Ignore("Popup prefab has not been baked yet.");

            Assert.That(prefab.HasBuiltLayout, Is.True);

            var instance = Object.Instantiate(prefab);
            try
            {
                instance.gameObject.SetActive(true);
                Assert.That(instance.HasBuiltLayout, Is.True);
                instance.Bind(MemoryPathPopups.Exit(2, null, null));
                var panel = instance.transform.Find("Panel") as RectTransform;
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.Find("Kicker").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("EXIT CONFIRMATION"));
            }
            finally
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }
    }
}
