using Game.Unity.Ui;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class UiDrawTests
    {
        [Test]
        public void LabelUsesTextMeshProNotLegacyText()
        {
            var root = new GameObject("UiDrawRoot");
            try
            {
                var label = UiDraw.Label(root.transform, "Title", "Sharp", 28, FontStyle.Bold, Color.white);
                Assert.That(label, Is.Not.Null);
                Assert.That(label, Is.InstanceOf<TextMeshProUGUI>());
                Assert.That(label.GetComponent<Text>(), Is.Null);
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.extraPadding, Is.True);
                Assert.That(label.richText, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RoundedSpriteIsLargeEnoughToSliceWithoutMush()
        {
            Assert.That(UiDraw.Rounded.rect.width, Is.GreaterThanOrEqualTo(128f));
            Assert.That(UiDraw.Circle.rect.width, Is.GreaterThanOrEqualTo(128f));
        }

        [Test]
        public void LoadOrCreateGradientFallsBackWhenAssetMissing()
        {
            var tex = UiDraw.LoadOrCreateVerticalGradient(
                "Ui/Art/DoesNotExist",
                Color.red,
                Color.green,
                Color.blue);
            Assert.That(tex, Is.Not.Null);
            Assert.That(tex.height, Is.GreaterThan(1));
        }

        [Test]
        public void OutfitLabelUsesWeightedFontAsset()
        {
            var root = new GameObject("UiDrawRoot");
            try
            {
                var label = UiDraw.Label(root.transform, "Title", "Outfit", 18f, UiWeight.ExtraBold, Color.white);
                Assert.That(label.font, Is.Not.Null);
                Assert.That(label.fontStyle, Is.EqualTo(FontStyles.Normal));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HorizontalGradientCreatesReadableTexture()
        {
            var tex = UiDraw.HorizontalGradient(Color.red, Color.green, Color.blue);
            Assert.That(tex, Is.Not.Null);
            Assert.That(tex.width, Is.GreaterThan(1));
        }

        [Test]
        public void HasLiveAtlasIsFalseForNullFont()
        {
            Assert.That(TmpEmojiFallback.HasLiveAtlas(null), Is.False);
        }

        [Test]
        public void StripBrokenFallbacksRemovesNullEmojiAssets()
        {
            if (TMP_Settings.instance == null)
                Assert.Ignore("TMP Settings not imported.");

            var list = TMP_Settings.emojiFallbackTextAssets;
            if (list == null)
            {
                list = new System.Collections.Generic.List<TMP_Asset>();
                TMP_Settings.emojiFallbackTextAssets = list;
            }

            list.Add(null);
            TmpEmojiFallback.StripBrokenFallbacks();
            Assert.That(list, Does.Not.Contain(null));
            for (var i = 0; i < list.Count; i++)
            {
                var font = list[i] as TMP_FontAsset;
                if (font != null)
                    Assert.That(TmpEmojiFallback.HasLiveAtlas(font), Is.True);
            }
        }

        [Test]
        public void DoorEmojiLabelRebuildDoesNotThrowMissingAtlas()
        {
            var root = new GameObject("UiDrawEmojiRoot", typeof(Canvas));
            try
            {
                var label = UiDraw.Label(
                    root.transform,
                    "Title",
                    "Leaving so soon? \U0001F6AA",
                    28,
                    FontStyle.Bold,
                    Color.white);
                Assert.DoesNotThrow(() => label.ForceMeshUpdate(true));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
