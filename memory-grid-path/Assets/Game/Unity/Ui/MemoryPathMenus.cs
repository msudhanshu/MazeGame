using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Nixin.Icons;

namespace Game.Unity.Ui
{
    /// <summary>Shared Figma chrome for pastel menu screens (nav bar, scale, icons).</summary>
    public static class MemoryPathMenus
    {
        public const float FigmaWidth = 402f;
        public const float ScreenWidth = 720f;

        public static float S(float fig) => fig * (ScreenWidth / FigmaWidth);

        public static int Si(float fig) => Mathf.RoundToInt(S(fig));

        public static Image NavBar(
            Transform parent,
            string title,
            bool amberBack,
            out Button back,
            out TextMeshProUGUI titleLabel)
        {
            var header = UiDraw.Panel(parent, "Header", MemoryPathPalette.NavFrost);
            UiDraw.SetCornerRadius(header, S(16));
            UiDraw.Stroke(header, MemoryPathPalette.CardBorder);
            UiDraw.Fit(header, 0f, S(64));
            var row = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(row, 0f, TextAnchor.MiddleCenter, expandWidth: true);
            row.padding = new RectOffset(Si(16), Si(16), Si(12), Si(12));

            var backFill = amberBack ? Hex(0xFFF7ED) : Color.white;
            var backStroke = amberBack ? MemoryPathPalette.BannerFrom : MemoryPathPalette.CardBorder;
            var backImage = UiDraw.Panel(header.transform, "Back", backFill, UiDraw.Circle);
            UiDraw.Stroke(backImage, backStroke);
            UiDraw.DropShadow(backImage, new Color(0f, 0f, 0f, 0.05f), new Vector2(0f, -S(2)));
            UiDraw.Fit(backImage, S(40), S(40));
            back = backImage.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;

            var chevron = LayoutIcon(backImage.transform, "Icon", NixinIcons.ChevronLeft, S(20));
            chevron.color = MemoryPathPalette.Ink;
            Center(chevron.rectTransform, S(20));

            titleLabel = UiDraw.Label(header.transform, "Title", title, S(20), UiWeight.Bold, MemoryPathPalette.Ink);
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var titleFit = titleLabel.gameObject.AddComponent<LayoutElement>();
            titleFit.flexibleWidth = 1f;
            titleFit.minHeight = S(24);

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(header.transform, false);
            UiDraw.Fit(spacer.transform, S(40), S(40));
            return header;
        }

        public static Image LayoutIcon(Transform parent, string name, string resource, float size)
        {
            var image = UiDraw.Icon(parent, name, UiDraw.ResourceSprite(resource), size);
            UiDraw.Fit(image, size, size);
            return image;
        }

        public static void Center(RectTransform rect, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
        }

        public static void PaintHorizontalBg(Transform root, Color left, Color mid, Color right)
        {
            var bg = root.Find("Bg");
            if (bg == null)
                return;
            var raw = bg.GetComponent<RawImage>();
            if (raw != null)
                raw.texture = UiDraw.HorizontalGradient(left, mid, right);
        }

        static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }
    }
}
