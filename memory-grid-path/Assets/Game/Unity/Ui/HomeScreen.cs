using System;
using Game.Core.State;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nixin.Ui;

namespace Game.Unity.Ui
{
    public sealed class HomePayload
    {
        public int Level;
        public int StarsEarned;
        public int StarsMax;
        public int CareerScore;
        public string ModeTitle;
        public Action OnPlay;
        public Action OnLevels;
        public Action OnSettings;
    }

    public sealed class HomeScreen : UiView<HomePayload>
    {
        [SerializeField] TextMeshProUGUI _modeTitle;
        [SerializeField] TextMeshProUGUI _level;
        [SerializeField] TextMeshProUGUI _stars;
        [SerializeField] TextMeshProUGUI _score;
        [SerializeField] Button _play;
        [SerializeField] Button _levels;
        [SerializeField] Button _settings;
        HomePayload _payload;

        public TextMeshProUGUI ModeTitle => _modeTitle;

        public static HomeScreen CreateTemplate()
        {
            var go = new GameObject("HomeScreen", typeof(RectTransform), typeof(HomeScreen));
            var view = go.GetComponent<HomeScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(HomePayload payload)
        {
            _payload = payload ?? new HomePayload();
            if (_level == null)
                Build();

            if (_modeTitle != null)
                _modeTitle.text = string.IsNullOrEmpty(_payload.ModeTitle) ? "Tile Arena" : _payload.ModeTitle;

            _level.text = Mathf.Max(1, _payload.Level).ToString();
            _stars.text = _payload.StarsEarned + "/" + _payload.StarsMax;
            _score.text = _payload.CareerScore.ToString("N0");
            Bind(_play, _payload.OnPlay);
            Bind(_levels, _payload.OnLevels);
            Bind(_settings, _payload.OnSettings);
        }

        static void Bind(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        void Build()
        {
            SetCloseOnBackdrop(false);
            var root = GetComponent<RectTransform>();
            UiDraw.Stretch(root);

            var bg = new GameObject("Bg", typeof(RawImage));
            bg.transform.SetParent(transform, false);
            UiDraw.Stretch(bg.GetComponent<RectTransform>());
            var raw = bg.GetComponent<RawImage>();
            raw.texture = UiDraw.LoadOrCreateVerticalGradient(
                UiDraw.GradHomeBgResource,
                MemoryPathPalette.MenuTo,
                MemoryPathPalette.MenuMid,
                MemoryPathPalette.MenuFrom);
            raw.raycastTarget = true;

            var col = new GameObject("Column", typeof(VerticalLayoutGroup)).transform;
            col.SetParent(transform, false);
            var colRect = col.GetComponent<RectTransform>();
            colRect.anchorMin = new Vector2(0.5f, 0.5f);
            colRect.anchorMax = new Vector2(0.5f, 0.5f);
            colRect.sizeDelta = new Vector2(720f, 920f);
            var layout = col.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 28;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(0, 0, 20, 20);

            var title = UiDraw.Label(col, "Title", "*  Memory Path  *", 52, FontStyle.Bold, MemoryPathPalette.Ink);
            Size(title, 72);
            var tag = UiDraw.Label(col, "Tag", "A soothing puzzle journey", 24, FontStyle.Normal, MemoryPathPalette.Ink);
            Size(tag, 36);

            var stage = UiDraw.Panel(col, "Stage", new Color(1f, 1f, 1f, 0.35f), UiDraw.Circle);
            Size(stage.rectTransform, 200);
            var inner = UiDraw.Panel(stage.transform, "Inner", new Color(1f, 1f, 1f, 0.45f), UiDraw.Circle);
            UiDraw.Stretch(inner.rectTransform);
            inner.rectTransform.offsetMin = new Vector2(22, 22);
            inner.rectTransform.offsetMax = new Vector2(-22, -22);
            var mascot = UiDraw.Panel(inner.transform, "Mascot", MemoryPathPalette.Mascot, UiDraw.Circle);
            UiDraw.Stretch(mascot.rectTransform);
            mascot.rectTransform.offsetMin = new Vector2(18, 18);
            mascot.rectTransform.offsetMax = new Vector2(-18, -18);
            var eyes = UiDraw.Label(mascot.transform, "Eyes", "o  o", 28, FontStyle.Bold, Color.white);
            UiDraw.Stretch(eyes.rectTransform);

            var stats = UiDraw.Panel(col, "Stats", MemoryPathPalette.Card);
            UiDraw.Stroke(stats, MemoryPathPalette.CardBorder);
            Size(stats.rectTransform, 110);
            var statsRow = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            statsRow.childAlignment = TextAnchor.MiddleCenter;
            statsRow.childForceExpandWidth = true;
            _level = Stat(stats.transform, "LEVEL", "1", MemoryPathPalette.Teal);
            _stars = Stat(stats.transform, "STARS", "0/0", MemoryPathPalette.Teal);
            _score = Stat(stats.transform, "SCORE", "0", MemoryPathPalette.ScoreOrange);

            _modeTitle = UiDraw.Label(col, "ModeTitle", "Tile Arena", 24, FontStyle.Bold, MemoryPathPalette.Ink);
            Size(_modeTitle, 36);

            _play = UiDraw.Button(col, "Play", MemoryPathPalette.PlayButton, "Play Journey", 32, Color.white);
            Size(_play.transform, 88);
            var secondary = new GameObject("Row", typeof(HorizontalLayoutGroup)).transform;
            secondary.SetParent(col, false);
            Size(secondary, 72);
            var secLayout = secondary.GetComponent<HorizontalLayoutGroup>();
            secLayout.spacing = 18;
            secLayout.childForceExpandWidth = true;
            secLayout.childForceExpandHeight = true;
            _levels = UiDraw.Button(secondary, "Levels", MemoryPathPalette.LevelsButton, "Levels", 26, MemoryPathPalette.Ink);
            _settings = UiDraw.Button(secondary, "Settings", MemoryPathPalette.SettingsButton, "Settings", 26, MemoryPathPalette.Ink);
        }

        static TextMeshProUGUI Stat(Transform parent, string caption, string value, Color accent)
        {
            var cell = new GameObject(caption, typeof(VerticalLayoutGroup)).transform;
            cell.SetParent(parent, false);
            var layout = cell.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            UiDraw.Label(cell, "Cap", caption, 16, FontStyle.Bold, MemoryPathPalette.Ink);
            return UiDraw.Label(cell, "Val", value, 28, FontStyle.Bold, accent);
        }

        static void Size(Component target, float height)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = height;
            fit.preferredHeight = height;
        }
    }
}
