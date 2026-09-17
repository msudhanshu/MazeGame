using System;
using System.Collections;
using Game.Core.State;
using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using UnityEngine.UI;
using Nixin.Icons;
using Nixin.Ui;

namespace Game.Unity.Ui
{
    public sealed class JourneyModeIconInfo
    {
        public GameModeId Id;
        public string Title;
        public Sprite Icon;
        public bool Unlocked;
        public bool Selected;
        public Color Tint = MemoryPathPalette.Teal;
    }

    public sealed class JourneyHomePayload
    {
        public int Level;
        public int StarsEarned;
        public int StarsMax;
        public int CareerScore;
        public bool SoundOn = true;
        public JourneyModeIconInfo[] Modes;
        public string SelectedModeTitle;
        public Action<GameModeId> OnSelectMode;
        public Action OnPlay;
        public Action OnLevels;
        public Action OnSettings;
        public Action OnVolume;
    }

    public sealed class JourneyHomeScreen : UiView<JourneyHomePayload>
    {
        public const float FigmaWidth = 402f;
        public const float ScreenWidth = 720f;

        public const string TitleCopy = "Memory Game: Remember the Path Home";
        public const string TagCopy = "Watch the radar. Walk it from memory.";
        public const string LevelsCopy = "Show Level";
        public const string ModesCopy = "SELECT GAME MODE";

        public const float ModeCardWidth = IconCard.DesignWidth;
        public const float ModeCardHeight = IconCard.DesignHeight;
        public const float HeaderButtonSize = 42f;
        public const float HeaderIconSize = 22f;
        public const float ModeCardCorner = 16f;
        public const float PlayButtonSize = 204f;
        public const float PlayIconSize = 68f;
        public const float PlayLabelSize = 22f;
        public const float HeroHeight = 310f;
        public const float ModeTitleSize = 26f;
        public const float ModeTitleHeight = 36f;
        public const float ModeHeadingSize = 15f;
        public const float SelectedModeOutline = 6f;
        public const string BodyName = "Body";
        public const string BodyContentName = "Content";

        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _tag;
        [SerializeField] TextMeshProUGUI _modeTitle;
        [SerializeField] TextMeshProUGUI _level;
        [SerializeField] TextMeshProUGUI _stars;
        [SerializeField] TextMeshProUGUI _score;
        [SerializeField] Transform _iconRow;
        [SerializeField] Button _play;
        [SerializeField] Button _levels;
        [SerializeField] Button _settings;
        [SerializeField] Button _volume;
        [SerializeField] Image _volumeIcon;
        [FormerlySerializedAs("_modeCard")]
        [SerializeField] IconCard _iconCard;
        JourneyHomePayload _payload;
        Coroutine _modeTitleBounce;
        string _shownModeTitle;
        bool _pendingModeTitleBounce;

        public TextMeshProUGUI ModeTitle => _modeTitle;

        public static float S(float fig) => fig * (ScreenWidth / FigmaWidth);

        static int Si(float fig) => Mathf.RoundToInt(S(fig));

        public static JourneyHomeScreen CreateTemplate()
        {
            var go = new GameObject("JourneyHomeScreen", typeof(RectTransform), typeof(JourneyHomeScreen));
            var view = go.GetComponent<JourneyHomeScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(JourneyHomePayload payload)
        {
            _payload = payload ?? new JourneyHomePayload();
            if (_title == null)
                Build();

            if (_title != null)
                _title.text = TitleCopy;
            if (_tag != null)
                _tag.text = TagCopy;

            ApplyModeTitle(ResolveSelectedModeTitle());

            _level.text = Mathf.Max(1, _payload.Level).ToString();
            _stars.text = _payload.StarsEarned + "/" + _payload.StarsMax;
            _score.text = _payload.CareerScore.ToString("N0");
            Bind(_play, _payload.OnPlay);
            Bind(_levels, _payload.OnLevels);
            Bind(_settings, _payload.OnSettings);
            Bind(_volume, ToggleVolume);
            ApplyVolume();
            ApplyHeaderButtons();
            ApplyPlayButton();
            ApplyModeHeading();
            EnsureHomeBody();
            ApplyHomeStackOrder();
            ConfigureIconRow();
            RebuildIcons();
            RefreshLayout();
        }

        string ResolveSelectedModeTitle()
        {
            if (!string.IsNullOrEmpty(_payload?.SelectedModeTitle))
                return _payload.SelectedModeTitle;

            var modes = _payload?.Modes;
            if (modes != null)
            {
                for (var i = 0; i < modes.Length; i++)
                {
                    if (modes[i] != null && modes[i].Selected)
                    {
                        return string.IsNullOrEmpty(modes[i].Title)
                            ? modes[i].Id.ToString()
                            : modes[i].Title;
                    }
                }

                for (var i = 0; i < modes.Length; i++)
                {
                    if (modes[i] != null && modes[i].Unlocked)
                    {
                        return string.IsNullOrEmpty(modes[i].Title)
                            ? modes[i].Id.ToString()
                            : modes[i].Title;
                    }
                }

                if (modes.Length > 0 && modes[0] != null)
                {
                    return string.IsNullOrEmpty(modes[0].Title)
                        ? modes[0].Id.ToString()
                        : modes[0].Title;
                }
            }

            return "Tile Arena";
        }

        void ApplyModeTitle(string title)
        {
            EnsureModeTitle();
            if (_modeTitle == null)
                return;

            StyleModeTitle(_modeTitle);
            var previous = _shownModeTitle;
            _modeTitle.text = title;
            _shownModeTitle = title;
            if (title == previous)
                return;

            if (isActiveAndEnabled)
                PlayModeTitleBounce();
            else
                _pendingModeTitleBounce = true;
        }

        void OnEnable()
        {
            if (!_pendingModeTitleBounce)
                return;
            PlayModeTitleBounce();
        }

        void OnDisable()
        {
            if (_modeTitleBounce != null)
            {
                StopCoroutine(_modeTitleBounce);
                _modeTitleBounce = null;
            }

            if (_modeTitle == null)
                return;
            _modeTitle.rectTransform.localScale = Vector3.one;
            _modeTitle.color = MemoryPathPalette.HomeInk;
        }

        void StyleModeTitle(TextMeshProUGUI label)
        {
            if (label == null)
                return;
            label.font = UiDraw.FontOf(UiWeight.Black);
            label.fontStyle = FontStyles.Normal;
            label.fontSize = S(ModeTitleSize);
            label.color = MemoryPathPalette.HomeInk;
            Size(label, S(ModeTitleHeight));
            var section = label.transform.parent;
            if (section != null)
                Size(section, S(ModeTitleHeight));
        }

        void PlayModeTitleBounce()
        {
            _pendingModeTitleBounce = false;
            if (_modeTitle == null)
                return;

            if (_modeTitleBounce != null)
            {
                StopCoroutine(_modeTitleBounce);
                _modeTitleBounce = null;
            }

            _modeTitle.rectTransform.localScale = Vector3.one;
            _modeTitle.color = MemoryPathPalette.HomeInk;
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            _modeTitleBounce = StartCoroutine(BounceModeTitle());
        }

        IEnumerator BounceModeTitle()
        {
            var label = _modeTitle;
            if (label == null)
                yield break;

            var rect = label.rectTransform;
            var rest = MemoryPathPalette.HomeInk;
            var flash = MemoryPathPalette.HomeModeSelectedRing;
            const float duration = 0.45f;
            var elapsed = 0f;
            label.color = flash;
            rect.localScale = Vector3.one * 0.72f;
            while (elapsed < duration && label != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                rect.localScale = Vector3.one * ModeTitleBounceScale(t);
                label.color = Color.Lerp(flash, rest, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            if (label != null)
            {
                label.color = rest;
                label.rectTransform.localScale = Vector3.one;
            }

            _modeTitleBounce = null;
        }

        public static float ModeTitleBounceScale(float t)
        {
            const float c1 = 2.2f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        void EnsureModeTitle()
        {
            if (_modeTitle != null)
                return;

            var existing = FindHome("GameMode/Title");
            if (existing != null)
            {
                _modeTitle = existing.GetComponent<TextMeshProUGUI>();
                return;
            }

            var content = HomeContent();
            if (content == null)
                return;

            var hero = content.Find("Hero");
            var section = new GameObject("GameMode", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).transform;
            section.SetParent(content, false);
            if (hero != null)
                section.SetSiblingIndex(hero.GetSiblingIndex() + 1);

            MakeModeTitleLabel(section);
        }

        void ToggleVolume()
        {
            _payload.SoundOn = !_payload.SoundOn;
            ApplyVolume();
            _payload.OnVolume?.Invoke();
        }

        void ApplyVolume()
        {
            if (_volumeIcon == null)
                return;
            var on = _payload == null || _payload.SoundOn;
            _volumeIcon.color = new Color(1f, 1f, 1f, on ? 1f : 0.35f);
        }

        void RefreshLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            var column = FindChrome("Column") as RectTransform;
            if (column != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            var content = HomeContent() as RectTransform;
            if (content != null && content != column)
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        static void Bind(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            if (action != null)
                button.onClick.AddListener(() => action());
        }

        void RebuildIcons()
        {
            EnsureModeCard();
            for (var i = _iconRow.childCount - 1; i >= 0; i--)
            {
                var child = _iconRow.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            var modes = _payload.Modes ?? Array.Empty<JourneyModeIconInfo>();
            for (var i = 0; i < modes.Length; i++)
                SpawnModeCard(modes[i]);

            if (_iconRow is RectTransform row)
                LayoutRebuilder.ForceRebuildLayoutImmediate(row);
        }

        void EnsureModeCard()
        {
            var templates = EnsureTemplates();
            RemoveStaleModeCard(templates);

            var existing = templates.Find("IconCard");
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            _iconCard = IconCard.CreateTemplate(S(1f));
            _iconCard.name = "IconCard";
            _iconCard.transform.SetParent(templates, false);
            _iconCard.gameObject.SetActive(false);
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

        static void RemoveStaleModeCard(Transform templates)
        {
            var stale = templates.Find("ModeCard");
            if (stale == null)
                return;

            if (Application.isPlaying)
                Destroy(stale.gameObject);
            else
                DestroyImmediate(stale.gameObject);
        }

        void ConfigureIconRow()
        {
            if (_iconRow == null)
                return;

            EnsureModePanel();
            TintModePanel();
            ApplyModeRowLayout();
        }

        void EnsureModePanel()
        {
            if (_iconRow.parent != null && _iconRow.parent.name == "Panel")
                return;

            var section = _iconRow.parent;
            if (section == null)
                return;

            MakeModePanel(section);
            _iconRow.SetParent(section.Find("Panel"), false);
        }

        void TintModePanel()
        {
            if (_iconRow == null || _iconRow.parent == null)
                return;
            var image = _iconRow.parent.GetComponent<Image>();
            if (image != null)
                image.color = MemoryPathPalette.HomeModePanel;
        }

        static Image MakeModePanel(Transform parent)
        {
            var panel = UiDraw.Panel(parent, "Panel", MemoryPathPalette.HomeModePanel);
            UiDraw.SetCornerRadius(panel, S(24));
            UiDraw.Stroke(panel, MemoryPathPalette.HomeFrostBorder);
            UiDraw.DropShadow(panel, new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.1f), new Vector2(0f, -S(6)));
            var pad = Si(12);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, 0f, TextAnchor.MiddleCenter);
            layout.padding = new RectOffset(pad, pad, pad, pad);
            Size(panel.rectTransform, S(ModeCardHeight) + pad * 2);
            return panel;
        }

        void ApplyModeRowLayout()
        {
            var rowLayout = _iconRow.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout == null)
                return;

            UiDraw.Horizontal(rowLayout, S(10), TextAnchor.MiddleCenter, expandWidth: true);
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var fit = _iconRow.GetComponent<LayoutElement>() ?? _iconRow.gameObject.AddComponent<LayoutElement>();
            fit.flexibleWidth = 1f;
            fit.minHeight = fit.preferredHeight = S(ModeCardHeight);
        }

        void SpawnModeCard(JourneyModeIconInfo info)
        {
            var card = Instantiate(_iconCard, _iconRow, false);
            card.gameObject.SetActive(true);
            card.name = info.Id.ToString();
            card.PrepareForLayout(S(ModeCardWidth), S(ModeCardHeight));
            var fit = card.GetComponent<LayoutElement>();
            fit.flexibleWidth = 1f;
            fit.minWidth = 0f;
            card.Apply(ToLook(info));
            RoundModeCard(card);
            var mask = card.GetComponent<Mask>();
            if (mask != null)
                mask.enabled = !(info.Selected && info.Unlocked);
            var id = info.Id;
            if (_payload.OnSelectMode != null)
                card.SetClick(() => _payload.OnSelectMode(id));
            else
                card.SetClick(null);
        }

        static IconCardLook ToLook(JourneyModeIconInfo info)
        {
            var selected = info.Selected && info.Unlocked;
            var item = info.Icon != null ? info.Icon : UiDraw.ResourceSprite(ModeIcon(info));
            return new IconCardLook
            {
                BackgroundColor = info.Unlocked ? MemoryPathPalette.HomeFrost : MemoryPathPalette.HomeLockedCard,
                BorderColor = MemoryPathPalette.HomeFrostBorder,
                HighlightColor = MemoryPathPalette.HomeModeSelectedRing,
                Highlighted = selected,
                OutlineThickness = S(SelectedModeOutline),
                AccentColor = HeaderTint(info),
                Item = item,
                ItemColor = Color.white,
                Locked = !info.Unlocked,
                Lock = info.Unlocked ? null : UiDraw.ResourceSprite(NixinIcons.Lock),
                LockColor = Color.white,
                LockScrimColor = new Color(1f, 1f, 1f, 0.55f),
                Caption = string.IsNullOrEmpty(info.Title) ? info.Id.ToString() : info.Title,
                CaptionColor = info.Unlocked ? MemoryPathPalette.HomeInk : MemoryPathPalette.HomeMuted
            };
        }

        static Color HeaderTint(JourneyModeIconInfo info)
        {
            if (!info.Unlocked)
                return MemoryPathPalette.HomeLockedHeader;
            switch (info.Id)
            {
                case GameModeId.GraphArena:
                    return MemoryPathPalette.HomeGraph;
                case GameModeId.ScoutArena:
                    return info.Tint;
                default:
                    return MemoryPathPalette.HomeModeSelected;
            }
        }

        static string ModeIcon(JourneyModeIconInfo info)
        {
            switch (info.Id)
            {
                case GameModeId.GraphArena:
                    return "Home/share-2";
                case GameModeId.ScoutArena:
                    return "Home/lock";
                default:
                    return "Home/grid";
            }
        }

        void Build()
        {
            SetCloseOnBackdrop(false);
            UiDraw.Stretch(GetComponent<RectTransform>());

            var bg = new GameObject("Bg", typeof(RawImage));
            bg.transform.SetParent(transform, false);
            UiDraw.Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<RawImage>().texture = UiDraw.LoadOrCreateVerticalGradient(
                UiDraw.GradHomeBgResource,
                MemoryPathPalette.MenuTo,
                MemoryPathPalette.MenuMid,
                MemoryPathPalette.MenuFrom);

            var col = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            col.SetParent(transform, false);
            var colRect = col.GetComponent<RectTransform>();
            StretchColumn(colRect);
            var layout = col.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(16), TextAnchor.UpperCenter);
            layout.padding = new RectOffset(Si(24), Si(24), Si(12), Si(24));

            BuildHeader(col);
            BuildBrand(col);
            var body = BuildBody(col);
            BuildHero(body);
            BuildModeTitle(body);
            BuildModes(body);
            BuildLevelChip(col);
            BuildStats(col);
            EnsureHomeScroll();
        }

        Transform FindHome(string path)
        {
            return FindChrome("Column/" + BodyName + "/" + BodyContentName + "/" + path)
                ?? FindChrome("Column/" + BodyName + "/" + path)
                ?? FindChrome("Column/" + path);
        }

        Transform HomeContent()
        {
            return FindChrome("Column/" + BodyName + "/" + BodyContentName)
                ?? FindChrome("Column/" + BodyName)
                ?? FindChrome("Column");
        }

        void EnsureHomeBody()
        {
            var col = FindChrome("Column") as RectTransform;
            if (col == null)
                return;

            Transform content;
            if (col.Find(BodyName) == null)
            {
                StretchColumn(col);
                content = BuildBody(col);
            }
            else
            {
                content = HomeContent();
            }

            if (content != null && content != col)
            {
                MoveNamed(col, "Hero", content);
                MoveNamed(col, "GameMode", content);
                MoveNamed(col, "Modes", content);
            }

            EnsureHomeScroll();
        }

        static void MoveNamed(Transform from, string name, Transform to)
        {
            var child = from.Find(name);
            if (child == null || to == null)
                return;
            child.SetParent(to, false);
        }

        Transform BuildBody(Transform col)
        {
            var body = new GameObject(
                BodyName,
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter),
                typeof(LayoutElement)).transform;
            body.SetParent(col, false);
            var layout = body.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(16), TextAnchor.UpperCenter);
            body.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var fit = body.GetComponent<LayoutElement>();
            fit.flexibleHeight = 0f;
            fit.flexibleWidth = 1f;
            return body;
        }

        void EnsureHomeScroll()
        {
            var col = FindChrome("Column") as RectTransform;
            if (col == null || col.parent == null)
                return;

            var viewport = col.parent as RectTransform;
            if (viewport == null)
                return;

            if (viewport.GetComponent<ScrollRect>() != null)
                return;

            if (viewport.GetComponent<Image>() == null)
            {
                var hit = viewport.gameObject.AddComponent<Image>();
                hit.color = Color.clear;
                hit.raycastTarget = true;
            }

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = col;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 60f;
            scroll.inertia = true;
            scroll.verticalNormalizedPosition = 1f;
        }

        static void StretchColumn(RectTransform col)
        {
            if (col == null)
                return;
            col.anchorMin = new Vector2(0f, 1f);
            col.anchorMax = new Vector2(1f, 1f);
            col.pivot = new Vector2(0.5f, 1f);
            col.anchoredPosition = Vector2.zero;
            col.sizeDelta = new Vector2(0f, col.sizeDelta.y);
            var fitter = col.GetComponent<ContentSizeFitter>() ?? col.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.enabled = true;
        }

        void BuildModeTitle(Transform col)
        {
            var section = new GameObject("GameMode", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).transform;
            section.SetParent(col, false);
            MakeModeTitleLabel(section);
        }

        void MakeModeTitleLabel(Transform section)
        {
            Size(section, S(ModeTitleHeight));
            var layout = section.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, 0f, TextAnchor.MiddleCenter);
            section.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _modeTitle = UiDraw.Label(
                section,
                "Title",
                "Tile Arena",
                S(ModeTitleSize),
                UiWeight.Black,
                MemoryPathPalette.HomeInk,
                TextAnchor.MiddleCenter);
            _modeTitle.textWrappingMode = TextWrappingModes.NoWrap;
            _modeTitle.overflowMode = TextOverflowModes.Overflow;
            Size(_modeTitle, S(ModeTitleHeight));
        }

        void ApplyPlayButton()
        {
            if (_play == null)
                return;

            var playRect = _play.GetComponent<RectTransform>();
            var size = S(PlayButtonSize);
            playRect.sizeDelta = new Vector2(size, size);

            var hero = _play.transform.parent as RectTransform;
            if (hero != null && hero.name == "Hero")
                Size(hero, S(HeroHeight));

            playRect.anchoredPosition = Vector2.zero;

            var stack = _play.transform.Find("Stack") as RectTransform;
            if (stack != null)
                stack.sizeDelta = new Vector2(S(126), S(108));

            var icon = _play.transform.Find("Stack/Icon");
            if (icon != null)
                Fit(icon, S(PlayIconSize));

            var label = _play.transform.Find("Stack/Label")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.fontSize = S(PlayLabelSize);
        }

        void ApplyModeHeading()
        {
            var heading = FindHome("Modes/Heading");
            if (heading == null)
                return;
            var text = heading.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;
            text.font = UiDraw.FontOf(UiWeight.Black);
            text.fontStyle = FontStyles.Normal;
            text.color = MemoryPathPalette.HomeInk;
            text.fontSize = S(ModeHeadingSize);
            Size(text, S(20));
        }

        void ApplyHomeStackOrder()
        {
            var col = FindChrome("Column");
            if (col == null)
                return;

            Place(col, "Header", 0);
            Place(col, "Brand", 1);
            Place(col, BodyName, 2);
            Place(col, "LevelWrap", 3);
            Place(col, "Stats", 4);

            var content = HomeContent();
            if (content != null && content != col)
            {
                Place(content, "Hero", 0);
                Place(content, "GameMode", 1);
                Place(content, "Modes", 2);
            }
        }

        static void Place(Transform parent, string name, int index)
        {
            var child = parent.Find(name);
            if (child == null)
                return;
            child.SetSiblingIndex(Mathf.Clamp(index, 0, parent.childCount - 1));
        }

        void BuildHeader(Transform col)
        {
            var header = new GameObject("Header", typeof(HorizontalLayoutGroup)).transform;
            header.SetParent(col, false);
            Size(header, S(HeaderButtonSize));
            UiDraw.Horizontal(header.GetComponent<HorizontalLayoutGroup>(), 0f, TextAnchor.MiddleCenter);

            _volume = FrostIcon(header, "Volume", "Home/volume-2");
            _volumeIcon = _volume.transform.Find("Icon").GetComponent<Image>();

            var spacer = new GameObject("Flex", typeof(LayoutElement));
            spacer.transform.SetParent(header, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1f;

            _settings = FrostIcon(header, "Settings", "Home/settings");
        }

        void BuildBrand(Transform col)
        {
            var brand = new GameObject("Brand", typeof(VerticalLayoutGroup)).transform;
            brand.SetParent(col, false);
            Size(brand, S(105));
            UiDraw.Vertical(brand.GetComponent<VerticalLayoutGroup>(), S(6), TextAnchor.UpperCenter);

            _title = UiDraw.Label(brand, "Title", TitleCopy, S(28), UiWeight.Black, MemoryPathPalette.HomeInk);
            _title.textWrappingMode = TextWrappingModes.NoWrap;
            _title.overflowMode = TextOverflowModes.Overflow;
            _title.enableAutoSizing = true;
            _title.fontSizeMin = S(16);
            _title.fontSizeMax = S(28);
            Size(_title, S(80));
            _tag = UiDraw.Label(brand, "Tag", TagCopy, S(15), UiWeight.Regular, MemoryPathPalette.HomeMuted);
            Size(_tag, S(19));
        }

        void BuildHero(Transform col)
        {
            var hero = new GameObject("Hero", typeof(RectTransform)).transform;
            hero.SetParent(col, false);
            Size(hero, S(HeroHeight));

            var art = UiDraw.Icon(hero, "Path", UiDraw.ResourceSprite("Home/path-graphic"), S(260));
            var artRect = art.rectTransform;
            artRect.anchorMin = artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.sizeDelta = new Vector2(S(260), S(230));
            artRect.anchoredPosition = Vector2.zero;

            _play = MakePlay(hero);
            var playRect = _play.GetComponent<RectTransform>();
            playRect.anchorMin = playRect.anchorMax = new Vector2(0.5f, 0.5f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = Vector2.zero;
            playRect.sizeDelta = new Vector2(S(PlayButtonSize), S(PlayButtonSize));
        }

        Button MakePlay(Transform parent)
        {
            var ring = UiDraw.Panel(parent, "Play", MemoryPathPalette.HomePlayRing, UiDraw.Circle);
            var button = ring.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            UiDraw.DropShadow(ring, MemoryPathPalette.HomePlayShadow, new Vector2(0f, -S(14)));

            var fill = UiDraw.Panel(ring.transform, "Fill", Color.white, UiDraw.Circle);
            fill.raycastTarget = false;
            UiDraw.Stretch(fill.rectTransform);
            var inset = S(4);
            fill.rectTransform.offsetMin = new Vector2(inset, inset);
            fill.rectTransform.offsetMax = new Vector2(-inset, -inset);
            var mask = fill.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var gradGo = new GameObject("Grad", typeof(RawImage));
            gradGo.transform.SetParent(fill.transform, false);
            UiDraw.Stretch(gradGo.GetComponent<RectTransform>());
            var raw = gradGo.GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = UiDraw.LoadOrCreateVerticalGradient(
                UiDraw.GradHomePlayResource,
                MemoryPathPalette.HomePlayFrom,
                Color.Lerp(MemoryPathPalette.HomePlayFrom, MemoryPathPalette.HomePlayTo, 0.5f),
                MemoryPathPalette.HomePlayTo);

            var stack = new GameObject("Stack", typeof(VerticalLayoutGroup)).transform;
            stack.SetParent(ring.transform, false);
            var stackRect = stack.GetComponent<RectTransform>();
            stackRect.anchorMin = stackRect.anchorMax = new Vector2(0.5f, 0.5f);
            stackRect.pivot = new Vector2(0.5f, 0.5f);
            stackRect.sizeDelta = new Vector2(S(126), S(108));
            var layout = stack.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(4), TextAnchor.MiddleCenter);
            layout.childForceExpandWidth = false;

            LayoutIcon(stack, "Icon", "Home/play-circle", S(PlayIconSize));
            UiDraw.Label(stack, "Label", "PLAY", S(PlayLabelSize), UiWeight.ExtraBold, Color.white);
            return button;
        }

        void BuildLevelChip(Transform col)
        {
            var wrap = new GameObject("LevelWrap", typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter)).transform;
            wrap.SetParent(col, false);
            Size(wrap, S(40));
            var wrapLayout = wrap.GetComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(wrapLayout, 0f, TextAnchor.MiddleCenter);
            wrap.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var chip = UiDraw.Panel(wrap, "Levels", MemoryPathPalette.HomeFrost);
            UiDraw.SetCornerRadius(chip, S(1));
            UiDraw.Stroke(chip, MemoryPathPalette.HomeFrostBorder);
            UiDraw.DropShadow(chip, new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.1f), new Vector2(0f, -S(4)));
            _levels = chip.gameObject.AddComponent<Button>();
            _levels.targetGraphic = chip;
            var chipLayout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(chipLayout, S(8), TextAnchor.MiddleCenter);
            chipLayout.padding = new RectOffset(Si(16), Si(16), Si(8), Si(8));
            chip.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutIcon(chip.transform, "Pin", "Home/map-pin", S(14));
            var label = UiDraw.Label(chip.transform, "Label", LevelsCopy, S(13), UiWeight.Bold, MemoryPathPalette.HomeInk);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            LayoutIcon(chip.transform, "Chevron", "Home/chevron-right", S(12));
        }

        void BuildStats(Transform col)
        {
            var stats = UiDraw.Panel(col, "Stats", MemoryPathPalette.HomeFrost);
            UiDraw.SetCornerRadius(stats, S(24));
            UiDraw.Stroke(stats, MemoryPathPalette.HomeFrostBorder);
            UiDraw.DropShadow(stats, new Color(15f / 255f, 23f / 255f, 42f / 255f, 0.1f), new Vector2(0f, -S(6)));
            Size(stats.rectTransform, S(70));
            var row = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(row, 0f, TextAnchor.MiddleCenter, expandWidth: true);
            row.padding = new RectOffset(Si(20), Si(20), Si(14), Si(14));

            _level = Stat(stats.transform, "LEVEL", "1", MemoryPathPalette.HomeInk);
            Divider(stats.transform);
            _stars = Stat(stats.transform, "STARS", "0/0", MemoryPathPalette.HomeStar);
            Divider(stats.transform);
            _score = Stat(stats.transform, "SCORE", "0", MemoryPathPalette.HomeScore);
        }

        void BuildModes(Transform col)
        {
            var section = new GameObject("Modes", typeof(VerticalLayoutGroup)).transform;
            section.SetParent(col, false);
            var layout = section.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(12), TextAnchor.UpperLeft);

            var heading = UiDraw.Label(section, "Heading", ModesCopy, S(ModeHeadingSize), UiWeight.Black, MemoryPathPalette.HomeInk, TextAnchor.MiddleLeft);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            Size(heading, S(20));

            var panel = MakeModePanel(section);
            var row = new GameObject("Row", typeof(HorizontalLayoutGroup), typeof(LayoutElement)).transform;
            row.SetParent(panel.transform, false);
            _iconRow = row;
            ApplyModeRowLayout();
            EnsureModeCard();
        }

        static TextMeshProUGUI Stat(Transform parent, string caption, string value, Color accent)
        {
            var cell = new GameObject(caption, typeof(VerticalLayoutGroup)).transform;
            cell.SetParent(parent, false);
            var layout = cell.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(4), TextAnchor.MiddleCenter);
            UiDraw.Label(cell, "Cap", caption, S(12), UiWeight.SemiBold, MemoryPathPalette.HomeMuted);
            return UiDraw.Label(cell, "Val", value, S(18), UiWeight.ExtraBold, accent);
        }

        static void Divider(Transform parent)
        {
            var go = new GameObject("Div", typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = MemoryPathPalette.HomeDivider;
            image.raycastTarget = false;
            var fit = go.GetComponent<LayoutElement>();
            fit.minWidth = fit.preferredWidth = 2f;
            fit.minHeight = fit.preferredHeight = S(32);
            fit.flexibleWidth = 0f;
            fit.flexibleHeight = 0f;
        }

        void ApplyHeaderButtons()
        {
            var header = FindChrome("Column/Header");
            if (header != null)
                Size(header, S(HeaderButtonSize));

            ResizeFrostIcon(_volume, _volumeIcon);
            var settingsIcon = _settings != null ? _settings.transform.Find("Icon") : null;
            ResizeFrostIcon(_settings, settingsIcon != null ? settingsIcon.GetComponent<Image>() : null);
        }

        static void ResizeFrostIcon(Button button, Image icon)
        {
            if (button == null)
                return;

            Fit(button.transform, S(HeaderButtonSize));
            var image = button.GetComponent<Image>();
            if (image != null)
                UiDraw.SetCornerRadius(image, S(14));

            if (icon == null)
                return;
            var size = S(HeaderIconSize);
            icon.rectTransform.sizeDelta = new Vector2(size, size);
        }

        static void RoundModeCard(IconCard card)
        {
            if (card == null)
                return;
            var image = card.Background != null ? card.Background : card.GetComponent<Image>();
            if (image == null)
                return;
            image.sprite = UiDraw.Rounded;
            image.type = Image.Type.Sliced;
            UiDraw.SetCornerRadius(image, S(ModeCardCorner));
        }

        Button FrostIcon(Transform parent, string name, string resource)
        {
            var image = UiDraw.Panel(parent, name, MemoryPathPalette.HomeFrost);
            UiDraw.SetCornerRadius(image, S(14));
            UiDraw.Stroke(image, MemoryPathPalette.HomeFrostBorder);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Fit(image.rectTransform, S(HeaderButtonSize));

            var icon = UiDraw.Icon(image.transform, "Icon", UiDraw.ResourceSprite(resource), S(HeaderIconSize));
            var rect = icon.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(S(HeaderIconSize), S(HeaderIconSize));
            return button;
        }

        static Image LayoutIcon(Transform parent, string name, string resource, float size)
        {
            var image = UiDraw.Icon(parent, name, UiDraw.ResourceSprite(resource), size);
            Fit(image.rectTransform, size);
            return image;
        }

        static void Fit(Component target, float size)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            fit.minWidth = size;
            fit.preferredWidth = size;
            fit.minHeight = size;
            fit.preferredHeight = size;
            fit.flexibleWidth = 0f;
            fit.flexibleHeight = 0f;
        }

        static void Size(Component target, float height)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = height;
            fit.preferredHeight = height;
        }
    }
}
