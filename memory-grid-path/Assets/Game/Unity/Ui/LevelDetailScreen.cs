using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nixin.Ui;

namespace Game.Unity.Ui
{
    public sealed class LevelDetailPayload
    {
        public int Level;
        public string Headline;
        public string GridLabel;
        public string StepsLabel;
        public int BestScore;
        public Texture Thumbnail;
        public Action OnBack;
        public Action OnStart;
    }

    public sealed class LevelDetailScreen : UiView<LevelDetailPayload>
    {
        public const string TitleCopy = "Level Overview";
        public const string KickerCopy = "CURRENT LEVEL";
        public const string ObjectivesCopy = "LEVEL OBJECTIVES";

        [SerializeField] Button _back;
        [SerializeField] TextMeshProUGUI _headline;
        [SerializeField] TextMeshProUGUI _grid;
        [SerializeField] TextMeshProUGUI _steps;
        [SerializeField] TextMeshProUGUI _best;
        [SerializeField] TextMeshProUGUI _startLabel;
        [SerializeField] Button _start;
        [SerializeField] RawImage _bannerThumb;
        LevelDetailPayload _payload;

        static float S(float fig) => MemoryPathMenus.S(fig);
        static int Si(float fig) => MemoryPathMenus.Si(fig);

        public static LevelDetailScreen CreateTemplate()
        {
            var go = new GameObject("LevelDetailScreen", typeof(RectTransform), typeof(LevelDetailScreen));
            var view = go.GetComponent<LevelDetailScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(LevelDetailPayload payload)
        {
            _payload = payload ?? new LevelDetailPayload();
            if (_headline == null)
                Build();

            LevelSelectScreen.WireBack(_back, () => _payload?.OnBack?.Invoke());
            _headline.text = _payload.Headline ?? ("Level " + _payload.Level);
            _grid.text = _payload.GridLabel ?? "";
            _steps.text = _payload.StepsLabel ?? "";
            _best.text = _payload.BestScore + " pts";
            _startLabel.text = "Start Level " + _payload.Level;
            _start.onClick.RemoveAllListeners();
            if (_payload.OnStart != null)
                _start.onClick.AddListener(() => _payload.OnStart());

            if (_bannerThumb != null)
            {
                var hasThumb = _payload.Thumbnail != null;
                _bannerThumb.texture = _payload.Thumbnail;
                _bannerThumb.gameObject.SetActive(hasThumb);
            }

            RefreshLayout();
        }

        void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            var column = FindChrome("Column") as RectTransform;
            if (column != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }

        void Build()
        {
            SetCloseOnBackdrop(false);
            UiDraw.Stretch(GetComponent<RectTransform>());

            var bg = new GameObject("Bg", typeof(RawImage));
            bg.transform.SetParent(transform, false);
            UiDraw.Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<RawImage>().texture = UiDraw.LoadOrCreateHorizontalGradient(
                UiDraw.GradDetailBgResource,
                MemoryPathPalette.DetailTo,
                MemoryPathPalette.DetailMid,
                MemoryPathPalette.DetailFrom);

            var col = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).transform;
            col.SetParent(transform, false);
            var colRect = col.GetComponent<RectTransform>();
            colRect.anchorMin = new Vector2(0f, 1f);
            colRect.anchorMax = new Vector2(1f, 1f);
            colRect.pivot = new Vector2(0.5f, 1f);
            colRect.anchoredPosition = Vector2.zero;
            colRect.sizeDelta = Vector2.zero;
            col.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var colLayout = col.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(colLayout, S(20), TextAnchor.UpperCenter);
            colLayout.padding = new RectOffset(Si(24), Si(24), Si(24), Si(40));

            MemoryPathMenus.NavBar(col, TitleCopy, true, out _back, out _);

            var card = UiDraw.Panel(col, "Card", MemoryPathPalette.Card);
            UiDraw.SetCornerRadius(card, S(28));
            UiDraw.Stroke(card, MemoryPathPalette.CardBorder);
            UiDraw.DropShadow(card, new Color(0f, 0f, 0f, 0.08f), new Vector2(0f, -S(10)));
            var cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(cardLayout, S(24), TextAnchor.UpperCenter);
            cardLayout.padding = new RectOffset(Si(24), Si(24), Si(24), Si(24));

            BuildBanner(card.transform);
            BuildObjectives(card.transform);
            BuildStart(card.transform);
        }

        void BuildBanner(Transform card)
        {
            var banner = UiDraw.Panel(card, "Banner", Color.white);
            UiDraw.SetCornerRadius(banner, S(20));
            var mask = banner.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            var bannerLayout = banner.gameObject.AddComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(bannerLayout, S(12), TextAnchor.MiddleCenter);
            bannerLayout.padding = new RectOffset(Si(20), Si(20), Si(20), Si(20));

            var gradGo = new GameObject("Grad", typeof(RawImage), typeof(LayoutElement));
            gradGo.transform.SetParent(banner.transform, false);
            UiDraw.Stretch(gradGo.GetComponent<RectTransform>());
            gradGo.GetComponent<LayoutElement>().ignoreLayout = true;
            var grad = gradGo.GetComponent<RawImage>();
            grad.raycastTarget = false;
            grad.texture = UiDraw.LoadOrCreateHorizontalGradient(
                UiDraw.GradDetailBannerResource,
                MemoryPathPalette.BannerFrom,
                MemoryPathPalette.BannerVia,
                MemoryPathPalette.BannerTo);
            gradGo.transform.SetAsFirstSibling();

            var thumbGo = new GameObject("Thumb", typeof(RawImage), typeof(LayoutElement));
            thumbGo.transform.SetParent(banner.transform, false);
            _bannerThumb = thumbGo.GetComponent<RawImage>();
            UiDraw.Stretch(_bannerThumb.rectTransform);
            thumbGo.GetComponent<LayoutElement>().ignoreLayout = true;
            _bannerThumb.raycastTarget = false;
            _bannerThumb.color = new Color(1f, 1f, 1f, 0.35f);
            _bannerThumb.gameObject.SetActive(false);
            thumbGo.transform.SetSiblingIndex(1);

            var mascot = UiDraw.Panel(banner.transform, "Mascot", MemoryPathPalette.BannerTo, UiDraw.Circle);
            mascot.raycastTarget = false;
            UiDraw.Fit(mascot, S(64), S(64));
            MemoryPathMenus.LayoutIcon(mascot.transform, "Eyes", "Home/eyes", S(18));
            MemoryPathMenus.Center(mascot.transform.Find("Eyes").GetComponent<RectTransform>(), S(18));

            var copy = new GameObject("Copy", typeof(VerticalLayoutGroup)).transform;
            copy.SetParent(banner.transform, false);
            var copyLayout = copy.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(copyLayout, S(4), TextAnchor.MiddleCenter);
            copyLayout.childForceExpandWidth = false;
            var kicker = UiDraw.Label(copy, "Kicker", KickerCopy, S(13), UiWeight.Bold, MemoryPathPalette.BannerKicker);
            kicker.characterSpacing = 1f;
            kicker.textWrappingMode = TextWrappingModes.NoWrap;
            _headline = UiDraw.Label(copy, "Headline", "", S(24), UiWeight.ExtraBold, MemoryPathPalette.Ink);
            _headline.textWrappingMode = TextWrappingModes.NoWrap;
        }

        void BuildObjectives(Transform card)
        {
            var section = new GameObject("Objectives", typeof(VerticalLayoutGroup)).transform;
            section.SetParent(card, false);
            var layout = section.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(14), TextAnchor.UpperLeft);

            var heading = UiDraw.Label(section, "Heading", ObjectivesCopy, S(14), UiWeight.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            heading.textWrappingMode = TextWrappingModes.NoWrap;

            _grid = ObjectiveRow(section, "Grid", "Home/obj-grid", "Grid Size");
            _steps = ObjectiveRow(section, "Steps", "Home/compass", "Required Steps");
            _best = ObjectiveRow(section, "Best", "Home/trophy", "Best Score");
        }

        static TextMeshProUGUI ObjectiveRow(Transform parent, string name, string icon, string caption)
        {
            var row = UiDraw.Panel(parent, name, Color.white);
            UiDraw.SetCornerRadius(row, S(16));
            UiDraw.Stroke(row, MemoryPathPalette.CardBorder);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(layout, S(8), TextAnchor.MiddleCenter, expandWidth: true);
            layout.padding = new RectOffset(Si(16), Si(16), Si(12), Si(12));

            var left = new GameObject("Left", typeof(HorizontalLayoutGroup), typeof(LayoutElement)).transform;
            left.SetParent(row.transform, false);
            var leftLayout = left.GetComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(leftLayout, S(8), TextAnchor.MiddleLeft);
            leftLayout.childForceExpandWidth = false;
            left.GetComponent<LayoutElement>().flexibleWidth = 1f;
            MemoryPathMenus.LayoutIcon(left, "Icon", icon, S(18));
            var cap = UiDraw.Label(left, "Cap", caption, S(15), UiWeight.SemiBold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            cap.textWrappingMode = TextWrappingModes.NoWrap;

            return UiDraw.Label(row.transform, "Val", "", S(15), UiWeight.ExtraBold, MemoryPathPalette.LevelsButton, TextAnchor.MiddleRight);
        }

        void BuildStart(Transform card)
        {
            var start = UiDraw.Panel(card, "Start", Color.white);
            UiDraw.SetCornerRadius(start, S(20));
            UiDraw.Stroke(start, Color.white);
            UiDraw.DropShadow(start, new Color(0f, 0f, 0f, 0.08f), new Vector2(0f, -S(10)));
            UiDraw.Fit(start, 0f, S(64));
            var mask = start.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            _start = start.gameObject.AddComponent<Button>();
            _start.targetGraphic = start;

            var gradGo = new GameObject("Grad", typeof(RawImage), typeof(LayoutElement));
            gradGo.transform.SetParent(start.transform, false);
            UiDraw.Stretch(gradGo.GetComponent<RectTransform>());
            gradGo.GetComponent<LayoutElement>().ignoreLayout = true;
            var grad = gradGo.GetComponent<RawImage>();
            grad.raycastTarget = false;
            grad.texture = UiDraw.LoadOrCreateHorizontalGradient(
                UiDraw.GradDetailStartResource,
                MemoryPathPalette.BannerTo,
                Color.Lerp(MemoryPathPalette.BannerTo, MemoryPathPalette.BannerVia, 0.5f),
                MemoryPathPalette.BannerVia);

            var row = new GameObject("Row", typeof(HorizontalLayoutGroup)).transform;
            row.SetParent(start.transform, false);
            UiDraw.Stretch(row.GetComponent<RectTransform>());
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(rowLayout, S(12), TextAnchor.MiddleCenter);
            rowLayout.padding = new RectOffset(Si(24), Si(24), Si(16), Si(16));
            rowLayout.childForceExpandWidth = false;
            MemoryPathMenus.LayoutIcon(row, "Triangle", "Home/triangle", S(18));
            _startLabel = UiDraw.Label(row, "Label", "Start", S(18), UiWeight.ExtraBold, Color.white);
            _startLabel.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
