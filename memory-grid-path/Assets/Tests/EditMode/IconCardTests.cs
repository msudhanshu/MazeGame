using Nixin.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class IconCardTests
    {
        [Test]
        public void CreateTemplateHasSlotsForChromeItemLockAndCaption()
        {
            var card = IconCard.CreateTemplate();
            try
            {
                Assert.That(card.Background, Is.Not.Null);
                Assert.That(card.Highlight, Is.Not.Null);
                Assert.That(card.Item, Is.Not.Null);
                Assert.That(card.LockOverlay, Is.Not.Null);
                Assert.That(card.CaptionLabel, Is.Not.Null);
                Assert.That(card.Button, Is.Not.Null);
                Assert.That(card.IsLocked, Is.False);
                Assert.That(card.Highlight.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ApplySetsBackgroundHighlightItemLockAndCaption()
        {
            var card = IconCard.CreateTemplate();
            var item = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            var lockIcon = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            var border = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            try
            {
                card.gameObject.SetActive(true);
                card.Apply(new IconCardLook
                {
                    BackgroundColor = Color.cyan,
                    AccentColor = Color.magenta,
                    Highlight = border,
                    HighlightColor = Color.yellow,
                    Highlighted = true,
                    BorderColor = Color.gray,
                    Item = item,
                    Lock = lockIcon,
                    Locked = true,
                    Caption = "Graph Arena",
                    CaptionColor = Color.black
                });

                Assert.That(card.Background.color, Is.EqualTo(Color.cyan));
                Assert.That(card.Accent.color, Is.EqualTo(Color.magenta));
                Assert.That(card.Item.sprite, Is.SameAs(item));
                Assert.That(card.LockOverlay.sprite, Is.SameAs(lockIcon));
                Assert.That(card.IsLocked, Is.True);
                Assert.That(card.LockScrim.color.a, Is.GreaterThan(0.4f));
                Assert.That(card.Caption, Is.EqualTo("Graph Arena"));
                Assert.That(card.CaptionLabel.color, Is.EqualTo(Color.black));
                Assert.That(card.Highlight.gameObject.activeSelf, Is.True);
                Assert.That(card.Highlight.sprite, Is.SameAs(border));
                Assert.That(card.Outline.effectColor, Is.EqualTo(Color.yellow));
                var cover = card.Item.GetComponent<AspectRatioFitter>();
                Assert.That(cover, Is.Not.Null);
                Assert.That(cover.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
                Assert.That(cover.aspectRatio, Is.EqualTo(1f).Within(0.01f));
                Assert.That(card.Accent.GetComponent<RectMask2D>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ApplyShowsPrefabHighlightWhenSelectedWithoutOverrideSprite()
        {
            var card = IconCard.CreateTemplate();
            try
            {
                card.gameObject.SetActive(true);
                card.Highlight.gameObject.SetActive(false);
                card.Apply(new IconCardLook
                {
                    Highlighted = true,
                    HighlightColor = Color.cyan,
                    Caption = "Tile Arena"
                });
                Assert.That(card.Highlight.gameObject.activeSelf, Is.False);
                Assert.That(card.Outline.effectColor, Is.EqualTo(Color.cyan));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ApplyTintsBackgroundChildNotRootWhenPresent()
        {
            var card = IconCard.CreateTemplate();
            var fill = new GameObject("Background", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(card.transform, false);
            var image = fill.GetComponent<Image>();
            image.color = Color.white;
            try
            {
                card.gameObject.SetActive(true);
                var root = card.GetComponent<Image>();
                var rootColor = root.color;
                card.Apply(new IconCardLook { BackgroundColor = Color.cyan, Caption = "X" });
                Assert.That(card.Background, Is.SameAs(image));
                Assert.That(image.color, Is.EqualTo(Color.cyan));
                Assert.That(root.color, Is.EqualTo(rootColor));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void ItemCoversArtUsingSpriteAspect()
        {
            var card = IconCard.CreateTemplate();
            var texture = new Texture2D(16, 9);
            var item = Sprite.Create(texture, new Rect(0f, 0f, 16f, 9f), new Vector2(0.5f, 0.5f));
            try
            {
                card.gameObject.SetActive(true);
                card.Apply(new IconCardLook { Item = item, Caption = "Tile Arena" });
                var cover = card.Item.GetComponent<AspectRatioFitter>();
                Assert.That(cover.aspectMode, Is.EqualTo(AspectRatioFitter.AspectMode.EnvelopeParent));
                Assert.That(cover.aspectRatio, Is.EqualTo(16f / 9f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void PrepareForLayoutReservesSpaceWithoutScaling()
        {
            var card = IconCard.CreateTemplate();
            try
            {
                card.transform.localScale = new Vector3(2f, 2f, 2f);
                card.PrepareForLayout(222f, 306f);
                var fit = card.GetComponent<LayoutElement>();
                Assert.That(fit.preferredWidth, Is.EqualTo(222f).Within(0.01f));
                Assert.That(fit.preferredHeight, Is.EqualTo(306f).Within(0.01f));
                Assert.That(card.transform.localScale, Is.EqualTo(Vector3.one));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void CreateTemplateUsesDesignFootprint()
        {
            var card = IconCard.CreateTemplate(2f);
            try
            {
                var fit = card.GetComponent<LayoutElement>();
                Assert.That(fit.preferredWidth, Is.EqualTo(IconCard.DesignWidth * 2f).Within(0.01f));
                Assert.That(fit.preferredHeight, Is.EqualTo(IconCard.DesignHeight * 2f).Within(0.01f));
                Assert.That(((RectTransform)card.transform).sizeDelta.y, Is.EqualTo(IconCard.DesignHeight * 2f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void SetClickInvokesCallback()
        {
            var card = IconCard.CreateTemplate();
            var taps = 0;
            try
            {
                card.gameObject.SetActive(true);
                card.SetClick(() => taps++);
                card.Button.onClick.Invoke();
                Assert.That(taps, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }
    }
}
