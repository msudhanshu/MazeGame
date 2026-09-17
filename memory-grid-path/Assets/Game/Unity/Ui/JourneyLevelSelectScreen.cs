using System;
using Game.Core.State;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nixin.Ui;
using Nixin.Icons;

namespace Game.Unity.Ui
{
    public sealed class JourneyLevelTileInfo
    {
        public int Number;
        public LevelLane Lane;
        public int Stars;
        public Texture Thumbnail;
        public Color Swatch = MemoryPathPalette.Teal;
    }

    public sealed class JourneyLevelSelectPayload
    {
        public string Title = "Select Level";
        public int StarsEarned;
        public int StarsMax;
        public JourneyLevelTileInfo[] Tiles;
        public Action OnBack;
        public Action<int> OnPick;
    }

    public sealed class JourneyLevelSelectScreen : UiView<JourneyLevelSelectPayload>
    {
        public const string TitleCopy = "Select Level";
        public const string FooterCopy = "Scroll for more chapters";
        public const int Columns = 3;
        public const float MinLaidOutWidth = 200f;
        public const float StarPanelFig = 28f;
        public const float CaptionFig = 36f;

        [SerializeField] Button _back;
        [SerializeField] TextMeshProUGUI _headerTitle;
        [SerializeField] Transform _grid;
        [SerializeField] GameObject _tileCard;
        JourneyLevelSelectPayload _payload;

        static float S(float fig) => MemoryPathMenus.S(fig);
        static int Si(float fig) => MemoryPathMenus.Si(fig);

        public static JourneyLevelSelectScreen CreateTemplate()
        {
            var go = new GameObject("JourneyLevelSelectScreen", typeof(RectTransform), typeof(JourneyLevelSelectScreen));
            var view = go.GetComponent<JourneyLevelSelectScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(JourneyLevelSelectPayload payload)
        {
            _payload = payload ?? new JourneyLevelSelectPayload();
            if (_grid == null)
                Build();
            UpgradeLegacyGrid();

            LevelSelectScreen.WireBack(_back, () => _payload?.OnBack?.Invoke());
            if (_headerTitle != null)
                _headerTitle.text = string.IsNullOrEmpty(_payload.Title) ? TitleCopy : _payload.Title;

            var template = ResolveTileTemplate();
            ClearSpawnedTiles(template);
            var tiles = _payload.Tiles ?? Array.Empty<JourneyLevelTileInfo>();
            for (var i = 0; i < tiles.Length; i++)
                SpawnTile(template, tiles[i]);

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

        public static Vector2 CellSizeForCanvas(float width)
        {
            var gap = S(12);
            var cellW = (width - Si(24) * 2 - gap * (Columns - 1)) / Columns;
            return new Vector2(cellW, S(StarPanelFig) + cellW + S(CaptionFig));
        }

        void ClearSpawnedTiles(LevelTileCard template)
        {
            if (_grid == null)
                return;

            var templateGo = template != null ? template.gameObject : null;
            for (var i = _grid.childCount - 1; i >= 0; i--)
            {
                var child = _grid.GetChild(i).gameObject;
                if (child == templateGo)
                    continue;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        LevelTileCard ResolveTileTemplate()
        {
            if (_tileCard == null)
            {
                var authored = transform.Find("Templates/LevelTile");
                if (authored != null)
                    _tileCard = authored.gameObject;
            }

            if (_tileCard == null)
                _tileCard = BuildTileCard(EnsureTemplates()).gameObject;

            if (!HasReworkedCardLayout(_tileCard.transform))
            {
                var parent = _tileCard.transform.parent != null ? _tileCard.transform.parent : EnsureTemplates();
                _tileCard.name = "LevelTile.Retired";
                _tileCard.SetActive(false);
                if (Application.isPlaying)
                    Destroy(_tileCard);
                else
                    DestroyImmediate(_tileCard);
                _tileCard = BuildTileCard(parent).gameObject;
            }

            _tileCard.SetActive(false);
            var card = _tileCard.GetComponent<LevelTileCard>();
            if (card == null)
                card = _tileCard.AddComponent<LevelTileCard>();
            card.EnsureSlots();
            return card;
        }

        static bool HasReworkedCardLayout(Transform tile)
        {
            return tile != null
                && tile.Find("Caption") != null
                && tile.Find("Art/LockOverlay") != null
                && tile.Find("Stars/Star0") != null;
        }

        void UpgradeLegacyGrid()
        {
            if (_grid == null)
                return;
            var grid = _grid.GetComponent<GridLayoutGroup>();
            if (grid == null || grid.constraintCount == Columns)
                return;

            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.cellSize = CellSizeForCanvas(1080f);
        }

        Transform EnsureTemplates()
        {
            var templates = transform.Find("Templates");
            if (templates != null)
                return templates;

            var go = new GameObject("Templates");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            return go.transform;
        }

        void SpawnTile(LevelTileCard template, JourneyLevelTileInfo info)
        {
            var card = Instantiate(template, _grid, false);
            card.gameObject.SetActive(true);
            card.name = "L" + info.Number;
            card.Apply(info);
            var pick = info.Number;
            if (info.Lane != LevelLane.Locked && _payload.OnPick != null)
                card.SetClick(() => _payload.OnPick(pick));
            else
                card.SetClick(null);
        }

        Image BuildTileCard(Transform parent)
        {
            var tile = UiDraw.Panel(parent, "LevelTile", MemoryPathPalette.ClearedTile);
            UiDraw.SetCornerRadius(tile, S(16));
            UiDraw.Stroke(tile, MemoryPathPalette.ClearedBorder);
            UiDraw.DropShadow(tile, new Color(0f, 0f, 0f, 0.05f), new Vector2(0f, -S(2)));
            tile.gameObject.AddComponent<Button>().targetGraphic = tile;
            var mask = tile.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var stack = tile.gameObject.AddComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(stack, S(4), TextAnchor.UpperCenter);
            stack.padding = new RectOffset(Si(6), Si(6), Si(6), Si(6));

            var starsPanel = UiDraw.Panel(tile.transform, "Stars", MemoryPathPalette.ScoreChip);
            starsPanel.raycastTarget = false;
            UiDraw.SetCornerRadius(starsPanel, S(10));
            UiDraw.Fit(starsPanel, 0f, S(StarPanelFig));
            var starRow = starsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(starRow, S(4), TextAnchor.MiddleCenter);
            starRow.padding = new RectOffset(Si(4), Si(4), Si(2), Si(2));
            for (var i = 0; i < 3; i++)
            {
                var star = MemoryPathMenus.LayoutIcon(starsPanel.transform, "Star" + i, NixinIcons.StarEmpty, S(14));
                star.raycastTarget = false;
            }

            var art = new GameObject("Art", typeof(RectTransform), typeof(LayoutElement), typeof(RectMask2D));
            art.transform.SetParent(tile.transform, false);
            var artFit = art.GetComponent<LayoutElement>();
            artFit.flexibleHeight = 1f;
            artFit.minHeight = S(40);

            var thumb = new GameObject("Thumb", typeof(RawImage)).GetComponent<RawImage>();
            thumb.transform.SetParent(art.transform, false);
            UiDraw.Stretch(thumb.rectTransform);
            thumb.raycastTarget = false;

            var lockRoot = new GameObject("LockOverlay", typeof(RectTransform)).transform;
            lockRoot.SetParent(art.transform, false);
            UiDraw.Stretch(lockRoot.GetComponent<RectTransform>());
            lockRoot.gameObject.SetActive(false);

            var scrim = UiDraw.Panel(lockRoot, "Scrim", new Color(0.12f, 0.14f, 0.18f, 0.55f));
            scrim.raycastTarget = false;
            UiDraw.Stretch(scrim.rectTransform);

            var lockIcon = MemoryPathMenus.LayoutIcon(lockRoot, "Icon", NixinIcons.Lock, S(48));
            lockIcon.raycastTarget = false;
            var lockFit = lockIcon.GetComponent<LayoutElement>();
            if (lockFit != null)
                lockFit.ignoreLayout = true;
            MemoryPathMenus.Center(lockIcon.rectTransform, S(48));

            var caption = UiDraw.Label(
                tile.transform,
                "Caption",
                "Level 1",
                S(14),
                UiWeight.ExtraBold,
                MemoryPathPalette.Ink);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;
            caption.raycastTarget = false;
            UiDraw.Fit(caption, 0f, S(CaptionFig));

            var card = tile.gameObject.GetComponent<LevelTileCard>() ?? tile.gameObject.AddComponent<LevelTileCard>();
            card.EnsureSlots();
            return tile;
        }

        void Build()
        {
            SetCloseOnBackdrop(false);
            UiDraw.Stretch(GetComponent<RectTransform>());

            var bg = new GameObject("Bg", typeof(RawImage));
            bg.transform.SetParent(transform, false);
            UiDraw.Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<RawImage>().texture = UiDraw.LoadOrCreateHorizontalGradient(
                UiDraw.GradSelectBgResource,
                MemoryPathPalette.SelectTo,
                MemoryPathPalette.SelectMid,
                MemoryPathPalette.SelectFrom);

            var col = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            col.SetParent(transform, false);
            var colRect = col.GetComponent<RectTransform>();
            colRect.anchorMin = Vector2.zero;
            colRect.anchorMax = Vector2.one;
            colRect.pivot = new Vector2(0.5f, 1f);
            colRect.offsetMin = Vector2.zero;
            colRect.offsetMax = Vector2.zero;
            var layout = col.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(16), TextAnchor.UpperCenter);
            layout.padding = new RectOffset(Si(24), Si(24), Si(24), Si(40));
            layout.childForceExpandHeight = false;

            MemoryPathMenus.NavBar(col, TitleCopy, false, out _back, out _headerTitle);

            var scrollGo = new GameObject("Scroll", typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(col, false);
            var scrollImage = scrollGo.GetComponent<Image>();
            scrollImage.color = new Color(1f, 1f, 1f, 0.01f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = false;
            var scrollFit = scrollGo.GetComponent<LayoutElement>();
            scrollFit.flexibleHeight = 1f;
            scrollFit.minHeight = S(200);

            var content = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollGo.transform, false);
            _grid = content.transform;
            var grid = content.GetComponent<GridLayoutGroup>();
            var cell = CellSizeForCanvas(1080f);
            grid.cellSize = cell;
            grid.spacing = new Vector2(S(12), S(12));
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = scrollGo.GetComponent<RectTransform>();

            BuildFooter(col);
            ResolveTileTemplate();
        }

        static void BuildFooter(Transform col)
        {
            var footer = UiDraw.Panel(col, "Footer", MemoryPathPalette.NavFrost);
            UiDraw.SetCornerRadius(footer, S(16));
            UiDraw.Stroke(footer, MemoryPathPalette.CardBorder);
            var stack = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(stack, S(4), TextAnchor.MiddleCenter);
            stack.padding = new RectOffset(Si(12), Si(12), Si(20), Si(12));
            UiDraw.Label(footer.transform, "Hint", FooterCopy, S(14), UiWeight.SemiBold, MemoryPathPalette.ButtonInk);
            MemoryPathMenus.LayoutIcon(footer.transform, "Arrow", NixinIcons.ArrowDown, S(24));
        }
    }
}
