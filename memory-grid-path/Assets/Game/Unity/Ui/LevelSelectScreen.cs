using System;
using Game.Core.State;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nixin.Ui;

namespace Game.Unity.Ui
{
    public sealed class LevelTileInfo
    {
        public int Number;
        public LevelLane Lane;
        public int Stars;
    }

    public sealed class LevelSelectPayload
    {
        public int StarsEarned;
        public int StarsMax;
        public LevelTileInfo[] Tiles;
        public Action OnBack;
        public Action<int> OnPick;
    }

    public sealed class LevelSelectScreen : UiView<LevelSelectPayload>
    {
        [SerializeField] Button _back;
        [SerializeField] TextMeshProUGUI _progress;
        [SerializeField] Transform _grid;
        LevelSelectPayload _payload;

        public static LevelSelectScreen CreateTemplate()
        {
            var go = new GameObject("LevelSelectScreen", typeof(RectTransform), typeof(LevelSelectScreen));
            var view = go.GetComponent<LevelSelectScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(LevelSelectPayload payload)
        {
            _payload = payload ?? new LevelSelectPayload();
            if (_grid == null)
                Build();

            WireBack(_back, () => _payload?.OnBack?.Invoke());
            _progress.text = _payload.StarsEarned + " / " + _payload.StarsMax;
            for (var i = _grid.childCount - 1; i >= 0; i--)
            {
                var child = _grid.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            var tiles = _payload.Tiles ?? Array.Empty<LevelTileInfo>();
            for (var i = 0; i < tiles.Length; i++)
                MakeTile(tiles[i]);
        }

        void MakeTile(LevelTileInfo info)
        {
            var locked = info.Lane == LevelLane.Locked;
            var cleared = info.Lane == LevelLane.Cleared;
            var fill = cleared ? MemoryPathPalette.ClearedTile : MemoryPathPalette.LockedTile;
            var border = cleared ? MemoryPathPalette.ClearedBorder : MemoryPathPalette.LockedBorder;
            var tile = UiDraw.Button(_grid, "L" + info.Number, fill, "", 28, MemoryPathPalette.Ink);
            UiDraw.Stroke(tile.image, border);
            var layout = tile.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 4;
            layout.padding = new RectOffset(6, 6, 10, 10);

            var mark = locked ? "X" : (info.Stars > 0 ? new string('*', info.Stars) : "* * *");
            var markColor = locked ? MemoryPathPalette.Body : (info.Stars > 0 ? MemoryPathPalette.LevelsButton : MemoryPathPalette.HudMuted);
            UiDraw.Label(tile.transform, "Mark", mark, 16, FontStyle.Bold, markColor);
            UiDraw.Label(tile.transform, "Num", info.Number.ToString(), 28, FontStyle.Bold, locked ? MemoryPathPalette.Body : MemoryPathPalette.Ink);

            var number = info.Number;
            tile.onClick.RemoveAllListeners();
            if (!locked && _payload.OnPick != null)
                tile.onClick.AddListener(() => _payload.OnPick(number));
        }

        void Build()
        {
            SetCloseOnBackdrop(false);
            UiDraw.Stretch(GetComponent<RectTransform>());

            var bg = new GameObject("Bg", typeof(RawImage));
            bg.transform.SetParent(transform, false);
            UiDraw.Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<RawImage>().texture = UiDraw.VerticalGradient(
                MemoryPathPalette.MenuTo, new Color(1f, 0.93f, 0.84f), MemoryPathPalette.MenuFrom);

            var header = MakeHeader(transform, "Select Level", null);
            _back = header.transform.Find("Back").GetComponent<Button>();

            var progress = UiDraw.Panel(transform, "Progress", MemoryPathPalette.Card);
            UiDraw.Stroke(progress, MemoryPathPalette.CardBorder);
            var pRect = progress.rectTransform;
            pRect.anchorMin = new Vector2(0.5f, 1f);
            pRect.anchorMax = new Vector2(0.5f, 1f);
            pRect.pivot = new Vector2(0.5f, 1f);
            pRect.anchoredPosition = new Vector2(0f, -150f);
            pRect.sizeDelta = new Vector2(720f, 64f);
            var pLayout = progress.gameObject.AddComponent<HorizontalLayoutGroup>();
            pLayout.padding = new RectOffset(24, 24, 12, 12);
            pLayout.childAlignment = TextAnchor.MiddleCenter;
            pLayout.childForceExpandWidth = true;
            UiDraw.Label(progress.transform, "Cap", "Journey Progress", 22, FontStyle.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            _progress = UiDraw.Label(progress.transform, "Val", "0 / 0", 22, FontStyle.Bold, MemoryPathPalette.LevelsButton, TextAnchor.MiddleRight);

            var scrollGo = new GameObject("Scroll", typeof(Image), typeof(Mask), typeof(ScrollRect));
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.5f, 0f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.offsetMin = new Vector2(-360f, 80f);
            scrollRect.offsetMax = new Vector2(360f, -230f);
            scrollGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(scrollGo.transform, false);
            _grid = content.transform;
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160f, 170f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = scrollRect;

            var headerRect = header.rectTransform;
            headerRect.anchorMin = new Vector2(0.5f, 1f);
            headerRect.anchorMax = new Vector2(0.5f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = new Vector2(0f, -24f);
            headerRect.sizeDelta = new Vector2(720f, 72f);
        }

        internal static void WireBack(Button back, Action onBack)
        {
            if (back == null)
                return;
            back.onClick.RemoveAllListeners();
            if (onBack != null)
                back.onClick.AddListener(() => onBack());

            var view = back.GetComponentInParent<UiView>(true);
            view?.SetBackHandler(onBack);
        }

        internal static Image MakeHeader(Transform parent, string title, Action onBack)
        {
            var header = UiDraw.Panel(parent, "Header", MemoryPathPalette.Card);
            UiDraw.Stroke(header, MemoryPathPalette.CardBorder);
            var back = UiDraw.Button(header.transform, "Back", Color.white, "<", 28, MemoryPathPalette.Ink);
            var backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.anchoredPosition = new Vector2(12f, 0f);
            backRect.sizeDelta = new Vector2(56f, 56f);
            WireBack(back, onBack);
            var label = UiDraw.Label(header.transform, "Title", title, 28, FontStyle.Bold, MemoryPathPalette.Ink);
            UiDraw.Stretch(label.rectTransform);
            return header;
        }
    }
}
