using System;
using System.Collections.Generic;
using Nixin.Icons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    public enum UiWeight
    {
        Regular,
        SemiBold,
        Bold,
        ExtraBold,
        Black
    }

    /// <summary>Runtime uGUI pieces shared by the HUD, screens, and popups.</summary>
    public static class UiDraw
    {
        const float SlicedCorner = 36f;

        public const string SliceRoundedResource = "Ui/Art/SliceRounded";
        public const string SliceCircleResource = "Ui/Art/SliceCircle";
        public const string GradHomeBgResource = "Ui/Art/GradHomeBg";
        public const string GradHomePlayResource = "Ui/Art/GradHomePlay";
        public const string GradSelectBgResource = "Ui/Art/GradSelectBg";
        public const string GradSettingsBgResource = "Ui/Art/GradSettingsBg";
        public const string GradDetailBgResource = "Ui/Art/GradDetailBg";
        public const string GradDetailBannerResource = "Ui/Art/GradDetailBanner";
        public const string GradDetailStartResource = "Ui/Art/GradDetailStart";

        static Sprite _rounded;
        static Sprite _circle;
        static Sprite _heart;
        static TMP_FontAsset _font;
        static readonly Dictionary<UiWeight, TMP_FontAsset> _weights = new Dictionary<UiWeight, TMP_FontAsset>();
        static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null)
                    return _font;

                if (TMP_Settings.defaultFontAsset != null)
                    return _font = TMP_Settings.defaultFontAsset;

                var packaged = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (packaged != null)
                    return _font = packaged;

                _font = TMP_FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
                if (_font != null)
                    _font.hideFlags = HideFlags.HideAndDontSave;
                return _font;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null)
                    _rounded = Resources.Load<Sprite>(SliceRoundedResource) ?? MakeSliced(256, 36);
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = Resources.Load<Sprite>(SliceCircleResource) ?? MakeSliced(256, 128);
                return _circle;
            }
        }

        public static Sprite Heart
        {
            get
            {
                if (_heart == null)
                    _heart = ResourceSprite("MemoryPath/heart") ?? MakeHeart(128);
                return _heart;
            }
        }

        public static void ClearGeneratedCache()
        {
            _rounded = null;
            _circle = null;
            _heart = null;
        }

        public static Sprite MakeSlicedSprite(int size, int radius) => MakeSliced(size, radius);

        public static bool UsesCircleSprite(string name) => IsCircleTarget(name);

        public static void ConfigureCanvas(Canvas canvas, CanvasScaler scaler)
        {
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Vertical(VerticalLayoutGroup layout, float spacing, TextAnchor align = TextAnchor.UpperCenter)
        {
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        public static void Horizontal(
            HorizontalLayoutGroup layout,
            float spacing,
            TextAnchor align = TextAnchor.MiddleCenter,
            bool expandWidth = false)
        {
            layout.spacing = spacing;
            layout.childAlignment = align;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = expandWidth;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;
        }

        public static void RestoreGeneratedArt(Transform root, Color menuTo, Color menuMid, Color menuFrom)
        {
            if (root == null)
                return;

            var bg = root.Find("Bg") as Transform;
            if (bg != null)
            {
                var raw = bg.GetComponent<RawImage>();
                if (raw != null)
                    raw.texture = VerticalGradient(menuTo, menuMid, menuFrom);
            }

            RestoreSlicedSprites(root);

            var playGrad = root.Find("SafeArea/Column/Body/Content/Hero/Play/Fill/Grad")
                ?? root.Find("Column/Body/Content/Hero/Play/Fill/Grad")
                ?? root.Find("SafeArea/Column/Hero/Play/Fill/Grad")
                ?? root.Find("Column/Hero/Play/Fill/Grad");
            if (playGrad != null)
            {
                var raw = playGrad.GetComponent<RawImage>();
                if (raw != null)
                    raw.texture = VerticalGradient(
                        MemoryPathPalette.HomePlayFrom,
                        Color.Lerp(MemoryPathPalette.HomePlayFrom, MemoryPathPalette.HomePlayTo, 0.5f),
                        MemoryPathPalette.HomePlayTo);
            }
        }

        public static void RestoreSlicedSprites(Transform root)
        {
            if (root == null)
                return;

            var images = root.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null)
                    continue;

                if (IsCircleTarget(image.gameObject.name))
                {
                    image.sprite = Circle;
                    image.type = Image.Type.Sliced;
                    continue;
                }

                if (image.sprite == null || image.sprite.texture == null)
                {
                    image.sprite = Rounded;
                    image.type = Image.Type.Sliced;
                }
            }
        }

        public static void RepairLayoutGroups(Transform root)
        {
            if (root == null)
                return;

            var verticals = root.GetComponentsInChildren<VerticalLayoutGroup>(true);
            for (var i = 0; i < verticals.Length; i++)
            {
                verticals[i].childControlWidth = true;
                verticals[i].childControlHeight = true;
            }

            var horizontals = root.GetComponentsInChildren<HorizontalLayoutGroup>(true);
            for (var i = 0; i < horizontals.Length; i++)
            {
                horizontals[i].childControlWidth = true;
                horizontals[i].childControlHeight = true;
            }
        }

        public static LayoutElement Fit(Component target, float width, float height)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                fit.minWidth = width;
                fit.preferredWidth = width;
                fit.flexibleWidth = 0f;
            }

            if (height > 0f)
            {
                fit.minHeight = height;
                fit.preferredHeight = height;
                fit.flexibleHeight = 0f;
            }

            return fit;
        }

        public static void ApplyEmojiSupport(TMP_Text text)
        {
            if (text == null)
                return;

            text.richText = true;
            TmpEmojiFallback.Ensure();
            if (TMP_Settings.instance == null)
                return;

            if (TMP_Settings.defaultSpriteAsset != null)
                text.spriteAsset = TMP_Settings.defaultSpriteAsset;
        }

        static bool IsCircleTarget(string name)
        {
            return name == "Play" || name == "Fill" || name == "Knob" || name == "Dot"
                || name == "Mascot" || name == "Back";
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite ?? Rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static TextMeshProUGUI Label(
            Transform parent,
            string name,
            string value,
            int size,
            FontStyle style,
            Color color,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = Font;
            text.fontSize = size;
            text.fontStyle = ToTmpStyle(style);
            text.color = color;
            text.alignment = ToTmpAlign(align);
            text.text = value;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.extraPadding = true;
            text.richText = true;
            ApplyEmojiSupport(text);
            return text;
        }

        public static Button Button(Transform parent, string name, Color fill, string label, int fontSize, Color labelColor)
        {
            var image = Panel(parent, name, fill);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(fill, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(fill, Color.black, 0.18f);
            button.colors = colors;

            var text = Label(image.transform, "Label", label, fontSize, FontStyle.Bold, labelColor);
            Stretch(text.rectTransform);
            return button;
        }

        public static Outline Stroke(Graphic graphic, Color color)
        {
            var outline = graphic.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return outline;
        }

        public static Shadow DropShadow(Graphic graphic, Color color, Vector2 distance)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
            return shadow;
        }

        public static void SetCornerRadius(Image image, float radiusUi)
        {
            if (image == null)
                return;
            image.pixelsPerUnitMultiplier = SlicedCorner / Mathf.Max(1f, radiusUi);
        }

        public static TMP_FontAsset FontOf(UiWeight weight)
        {
            if (_weights.TryGetValue(weight, out var cached) && cached != null)
                return cached;

            var resource = WeightResource(weight);
            var source = Resources.Load<Font>("Fonts/" + resource);
            TMP_FontAsset asset = null;
            if (source != null)
            {
                try
                {
                    asset = TMP_FontAsset.CreateFontAsset(source);
                }
                catch
                {
                    asset = null;
                }
            }

            if (asset == null)
                asset = Font;
            else
            {
                asset.hideFlags = HideFlags.HideAndDontSave;
                if (TMP_Settings.defaultFontAsset != null && asset != TMP_Settings.defaultFontAsset)
                {
                    if (asset.fallbackFontAssetTable == null)
                        asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
                    if (!asset.fallbackFontAssetTable.Contains(TMP_Settings.defaultFontAsset))
                        asset.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
                }
            }
            _weights[weight] = asset;
            return asset;
        }

        public static TextMeshProUGUI Label(
            Transform parent,
            string name,
            string value,
            float size,
            UiWeight weight,
            Color color,
            TextAnchor align = TextAnchor.MiddleCenter)
        {
            var text = Label(parent, name, value, Mathf.RoundToInt(size), FontStyle.Normal, color, align);
            text.fontSize = size;
            text.font = FontOf(weight);
            text.fontStyle = FontStyles.Normal;
            text.richText = true;
            ApplyEmojiSupport(text);
            return text;
        }

        public static Sprite ResourceSprite(string path)
        {
            if (_sprites.TryGetValue(path, out var cached) && cached != null)
                return cached;

            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null && !path.StartsWith(NixinIcons.Folder, StringComparison.Ordinal))
                sprite = Resources.Load<Sprite>(NixinIcons.Folder + System.IO.Path.GetFileName(path));
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex == null && !path.StartsWith(NixinIcons.Folder, StringComparison.Ordinal))
                    tex = Resources.Load<Texture2D>(NixinIcons.Folder + System.IO.Path.GetFileName(path));
                if (tex == null)
                    return null;

                try
                {
                    sprite = Sprite.Create(
                        tex,
                        new Rect(0f, 0f, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                    sprite.hideFlags = HideFlags.HideAndDontSave;
                }
                catch
                {
                    return null;
                }
            }

            _sprites[path] = sprite;
            return sprite;
        }

        public static Image Icon(Transform parent, string name, Sprite sprite, float size)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = new Vector2(size, size);
            return image;
        }

        static string WeightResource(UiWeight weight)
        {
            switch (weight)
            {
                case UiWeight.Regular:
                    return "Outfit-Regular";
                case UiWeight.SemiBold:
                    return "Outfit-SemiBold";
                case UiWeight.ExtraBold:
                    return "Outfit-ExtraBold";
                case UiWeight.Black:
                    return "Outfit-Black";
                default:
                    return "Outfit-Bold";
            }
        }

        public static Image Dot(Transform parent, Color color, float size)
        {
            var image = Panel(parent, "Dot", color, Circle);
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(size, size);
            image.raycastTarget = false;
            return image;
        }

        public static Texture2D VerticalGradient(Color top, Color mid, Color bottom)
        {
            const int height = 256;
            var tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            tex.name = "UiGradient";
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (var y = 0; y < height; y++)
            {
                var t = y / (height - 1f);
                var color = t < 0.5f
                    ? Color.Lerp(bottom, mid, t * 2f)
                    : Color.Lerp(mid, top, (t - 0.5f) * 2f);
                tex.SetPixel(0, y, color);
            }

            tex.Apply(false, false);
            return tex;
        }

        public static Texture LoadOrCreateVerticalGradient(string resource, Color top, Color mid, Color bottom)
        {
            var baked = Resources.Load<Texture2D>(resource);
            if (baked != null)
                return baked;
            return VerticalGradient(top, mid, bottom);
        }

        public static Texture LoadOrCreateHorizontalGradient(string resource, Color left, Color mid, Color right)
        {
            var baked = Resources.Load<Texture2D>(resource);
            if (baked != null)
                return baked;
            return HorizontalGradient(left, mid, right);
        }

        public static Texture2D HorizontalGradient(Color left, Color mid, Color right)
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.name = "UiGradientH";
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (var x = 0; x < width; x++)
            {
                var t = x / (width - 1f);
                var color = t < 0.5f
                    ? Color.Lerp(left, mid, t * 2f)
                    : Color.Lerp(mid, right, (t - 0.5f) * 2f);
                tex.SetPixel(x, 0, color);
            }

            tex.Apply(false, false);
            return tex;
        }

        static FontStyles ToTmpStyle(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        static TextAlignmentOptions ToTmpAlign(TextAnchor align)
        {
            switch (align)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }

        static Sprite MakeSliced(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "UiSlice";
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var mid = (size - 1) * 0.5f;
            var r = radius;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(Mathf.Abs(x - mid) - (mid - r), 0f);
                    var dy = Mathf.Max(Mathf.Abs(y - mid) - (mid - r), 0f);
                    var dist = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(r + 0.5f - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            var border = Mathf.Min(radius, size / 2);
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        static Sprite MakeHeart(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "UiHeart",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    var px = (u - 0.5f) * 2.55f;
                    var py = (v - 0.42f) * 2.55f;
                    var x2 = px * px;
                    var y2 = py * py;
                    var a = x2 + y2 - 1f;
                    var f = a * a * a - x2 * py * py * py;
                    var alpha = 1f - Mathf.SmoothStep(-0.035f, 0.035f, f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply(false, false);
            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.Tight);
            sprite.name = "UiHeart";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
