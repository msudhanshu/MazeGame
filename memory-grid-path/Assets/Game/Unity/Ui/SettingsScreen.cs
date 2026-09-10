using System;
using Nixin.Boot;
using Nixin.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    public sealed class SettingsPayload
    {
        public bool SoundEffects;
        public bool Music;
        public bool Haptics;
        public bool PathDrag;
        public string ThemeId;
        public Action OnBack;
        public Action<bool> OnSoundEffects;
        public Action<bool> OnMusic;
        public Action<bool> OnHaptics;
        public Action<bool> OnPathDrag;
        public Action<string> OnTheme;
        public Action OnReplayTutorial;
        public Action OnReplayOpening;
    }

    public sealed class SettingsScreen : UiView<SettingsPayload>
    {
        public const string TitleCopy = "Settings";
        public const string SystemCopy = "SYSTEM SETTINGS";
        public const string HelpCopy = "HELP";
        public const string ReplayTutorialCopy = "Replay tutorial";
        public const string ReplayTutorialBlurb = "Play the first-steps lesson again";
        public const string OpeningReminderCopy = "How to play";
        public const string OpeningReminderBlurb = "Show the opening reminder again";
        public const string StudioCopy = "Nixin Studio";
        public const string StudioTagCopy = "INDIE GAMES";
        public const string ContactCopy = "CONTACT US";
        public const string EmailCopy = "sudhanshu.manjeet@gmail.com";
        public const string CopyrightCopy = "© 2026 Nixin Studio. All rights reserved.";
        public const string Mailto = "mailto:" + EmailCopy;
        public const float ToggleTrackWidth = 48f;
        public const float ToggleTrackHeight = 26f;

        [SerializeField] Button _back;
        [SerializeField] ToggleRow _sfx;
        [SerializeField] ToggleRow _music;
        [SerializeField] ToggleRow _haptics;
        [SerializeField] ToggleRow _pathDrag;
        [SerializeField] Button _replayTutorial;
        [SerializeField] Button _replayOpening;
        [SerializeField] Button _email;
        SettingsPayload _payload;

        static float S(float fig) => MemoryPathMenus.S(fig);
        static int Si(float fig) => MemoryPathMenus.Si(fig);

        public static SettingsScreen CreateTemplate()
        {
            var go = new GameObject("SettingsScreen", typeof(RectTransform), typeof(SettingsScreen));
            var view = go.GetComponent<SettingsScreen>();
            view.Build();
            go.SetActive(false);
            return view;
        }

        public override void Bind(SettingsPayload payload)
        {
            _payload = payload ?? new SettingsPayload();
            if (_sfx == null || _sfx.Button == null)
                Build();
            _sfx = EnsureToggle(_sfx, "Column/System/Sfx", "Sfx", "Sound Effects", "Cute chime noises when stepping");
            _music = EnsureToggle(_music, "Column/System/Music", "Music", "Relaxing Music", "Lofi ambient backdrop melody");
            _haptics = EnsureToggle(_haptics, "Column/System/Haptics", "Haptics", "Tactile Vibration", "Soft feedback when turning");
            _pathDrag = EnsureToggle(
                _pathDrag,
                "Column/System/PathDrag",
                "PathDrag",
                "Slide Along Path",
                "Drag across tiles to walk the remembered route");
            EnsureReplayTutorialRow();
            EnsureOpeningReminderRow();
            PlaceHelpInColumn();
            TightenToggles();

            LevelSelectScreen.WireBack(_back, () => _payload?.OnBack?.Invoke());
            if (ToggleRow.IsUsable(_sfx))
                _sfx.Set(_payload.SoundEffects);
            if (ToggleRow.IsUsable(_music))
                _music.Set(_payload.Music);
            if (ToggleRow.IsUsable(_haptics))
                _haptics.Set(_payload.Haptics);
            if (ToggleRow.IsUsable(_pathDrag))
                _pathDrag.Set(_payload.PathDrag);
            _sfx.Wire(ToggleSound);
            _music.Wire(ToggleMusic);
            _haptics.Wire(ToggleHaptics);
            if (_pathDrag != null)
                _pathDrag.Wire(TogglePathDrag);
            WireHelpActions();
            if (_email != null)
            {
                _email.onClick.RemoveAllListeners();
                _email.onClick.AddListener(OpenMail);
            }

            RefreshLayout();
        }

        void ToggleSound()
        {
            _payload.SoundEffects = !_payload.SoundEffects;
            _sfx.Set(_payload.SoundEffects);
            _payload.OnSoundEffects?.Invoke(_payload.SoundEffects);
        }

        void ToggleMusic()
        {
            _payload.Music = !_payload.Music;
            _music.Set(_payload.Music);
            _payload.OnMusic?.Invoke(_payload.Music);
        }

        void ToggleHaptics()
        {
            _payload.Haptics = !_payload.Haptics;
            _haptics.Set(_payload.Haptics);
            _payload.OnHaptics?.Invoke(_payload.Haptics);
        }

        void TogglePathDrag()
        {
            if (_pathDrag == null)
                return;
            _payload.PathDrag = !_payload.PathDrag;
            _pathDrag.Set(_payload.PathDrag);
            _payload.OnPathDrag?.Invoke(_payload.PathDrag);
        }

        static void OpenMail()
        {
            Application.OpenURL(Mailto);
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
                UiDraw.GradSettingsBgResource,
                MemoryPathPalette.SelectTo,
                MemoryPathPalette.SelectMid,
                MemoryPathPalette.SettingsFrom);

            var col = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            col.SetParent(transform, false);
            var colRect = col.GetComponent<RectTransform>();
            colRect.anchorMin = Vector2.zero;
            colRect.anchorMax = Vector2.one;
            colRect.pivot = new Vector2(0.5f, 1f);
            colRect.offsetMin = Vector2.zero;
            colRect.offsetMax = Vector2.zero;
            var layout = col.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(20), TextAnchor.UpperCenter);
            layout.padding = new RectOffset(Si(24), Si(24), Si(12), Si(32));
            layout.childForceExpandHeight = false;

            MemoryPathMenus.NavBar(col, TitleCopy, false, out _back, out _);
            BuildSystem(col);
            BuildHelp(col);

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(col, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;
            spacer.GetComponent<LayoutElement>().minHeight = S(8);

            BuildFooter(col);
        }

        void BuildSystem(Transform col)
        {
            var section = new GameObject("System", typeof(VerticalLayoutGroup)).transform;
            section.SetParent(col, false);
            var layout = section.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(12), TextAnchor.UpperLeft);
            layout.padding = new RectOffset(0, 0, Si(8), 0);

            var heading = UiDraw.Label(section, "Heading", SystemCopy, S(13), UiWeight.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            heading.textWrappingMode = TextWrappingModes.NoWrap;

            _sfx = ToggleRow.Create(section, "Sfx", "Sound Effects", "Cute chime noises when stepping");
            _music = ToggleRow.Create(section, "Music", "Relaxing Music", "Lofi ambient backdrop melody");
            _haptics = ToggleRow.Create(section, "Haptics", "Tactile Vibration", "Soft feedback when turning");
            _pathDrag = ToggleRow.Create(section, "PathDrag", "Slide Along Path", "Drag across tiles to walk the remembered route");
        }

        void BuildHelp(Transform col)
        {
            var section = new GameObject("Help", typeof(VerticalLayoutGroup)).transform;
            section.SetParent(col, false);
            var layout = section.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(12), TextAnchor.UpperLeft);
            layout.padding = new RectOffset(0, 0, Si(8), 0);

            var heading = UiDraw.Label(section, "Heading", HelpCopy, S(13), UiWeight.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            _replayTutorial = ActionRow(section, "ReplayTutorial", ReplayTutorialCopy, ReplayTutorialBlurb);
            _replayOpening = ActionRow(section, "ReplayOpening", OpeningReminderCopy, OpeningReminderBlurb);
            PlaceHelpInColumn(section);
        }

        ToggleRow EnsureToggle(ToggleRow row, string path, string name, string title, string blurb)
        {
            if (ToggleRow.IsUsable(row))
                return row;

            var existing = transform.Find(path);
            var hydrated = ToggleRow.From(existing);
            if (ToggleRow.IsUsable(hydrated))
                return hydrated;

            var slash = path.LastIndexOf('/');
            var parent = slash >= 0 ? transform.Find(path.Substring(0, slash)) : transform;
            if (parent == null)
                return row;

            return ToggleRow.Create(parent, name, title, blurb);
        }

        void EnsureReplayTutorialRow()
        {
            if (_replayTutorial != null)
                return;

            var help = EnsureHelpSection();
            if (help == null)
                return;

            _replayTutorial = help.Find("ReplayTutorial") != null
                ? help.Find("ReplayTutorial").GetComponent<Button>()
                : ActionRow(help, "ReplayTutorial", ReplayTutorialCopy, ReplayTutorialBlurb);
        }

        void EnsureOpeningReminderRow()
        {
            if (_replayOpening != null)
                return;

            var help = EnsureHelpSection();
            if (help == null)
                return;

            _replayOpening = help.Find("ReplayOpening") != null
                ? help.Find("ReplayOpening").GetComponent<Button>()
                : ActionRow(help, "ReplayOpening", OpeningReminderCopy, OpeningReminderBlurb);
        }

        Transform EnsureHelpSection()
        {
            var help = FindHelp();
            if (help != null)
                return help;
            var column = FindColumn();
            if (column == null)
                return null;
            BuildHelp(column);
            return FindHelp();
        }

        Transform FindColumn() =>
            FindChrome("Column") ?? transform.Find("Column");

        Transform FindHelp() =>
            FindChrome("Column/Help") ?? transform.Find("Column/Help");

        void PlaceHelpInColumn()
        {
            PlaceHelpInColumn(FindHelp());
        }

        static void PlaceHelpInColumn(Transform help)
        {
            if (help == null)
                return;
            var column = help.parent;
            if (column == null)
                return;
            var spacer = column.Find("Spacer");
            var system = column.Find("System");
            if (spacer != null)
                help.SetSiblingIndex(spacer.GetSiblingIndex());
            else if (system != null)
                help.SetSiblingIndex(system.GetSiblingIndex() + 1);
        }

        void WireHelpActions()
        {
            var help = FindHelp();
            if (help != null)
                help.gameObject.SetActive(true);

            WireAction(_replayTutorial, _payload != null ? _payload.OnReplayTutorial : null);
            WireAction(_replayOpening, _payload != null ? _payload.OnReplayOpening : null);
            if (_replayTutorial != null)
                _replayTutorial.gameObject.SetActive(true);
            if (_replayOpening != null)
                _replayOpening.gameObject.SetActive(true);
        }

        void TightenToggles()
        {
            Tighten(_sfx);
            Tighten(_music);
            Tighten(_haptics);
            Tighten(_pathDrag);
        }

        static void Tighten(ToggleRow row)
        {
            if (row == null || row.Button == null || row.Track == null)
                return;
            var group = row.Button.GetComponent<HorizontalLayoutGroup>();
            if (group != null)
                group.childForceExpandWidth = false;
            FitToggleTrack(row.Track);
        }

        static void FitToggleTrack(Image track)
        {
            if (track == null)
                return;
            UiDraw.Fit(track, S(ToggleTrackWidth), S(ToggleTrackHeight));
            var fit = track.GetComponent<LayoutElement>();
            if (fit == null)
                return;
            fit.flexibleWidth = 0f;
            fit.flexibleHeight = 0f;
            fit.minWidth = S(ToggleTrackWidth);
            fit.preferredWidth = S(ToggleTrackWidth);
            fit.minHeight = S(ToggleTrackHeight);
            fit.preferredHeight = S(ToggleTrackHeight);
        }

        static void WireAction(Button button, Action onClick)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        static Button ActionRow(Transform parent, string name, string title, string blurb)
        {
            var row = UiDraw.Panel(parent, name, Color.white);
            UiDraw.SetCornerRadius(row, S(16));
            UiDraw.Stroke(row, MemoryPathPalette.CardBorder);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(layout, S(12), TextAnchor.MiddleCenter, expandWidth: true);
            layout.padding = new RectOffset(Si(16), Si(16), Si(14), Si(14));

            var copy = new GameObject("Copy", typeof(VerticalLayoutGroup), typeof(LayoutElement)).transform;
            copy.SetParent(row.transform, false);
            var copyLayout = copy.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(copyLayout, S(2), TextAnchor.MiddleLeft);
            copyLayout.childForceExpandWidth = false;
            copy.GetComponent<LayoutElement>().flexibleWidth = 1f;
            var titleLabel = UiDraw.Label(copy, "T", title, S(15), UiWeight.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            var blurbLabel = UiDraw.Label(copy, "B", blurb, S(13), UiWeight.Regular, MemoryPathPalette.Body, TextAnchor.MiddleLeft);
            blurbLabel.textWrappingMode = TextWrappingModes.NoWrap;

            var play = UiDraw.Label(row.transform, "Play", "Play", S(13), UiWeight.Bold, MemoryPathPalette.Ocean, TextAnchor.MiddleRight);
            play.textWrappingMode = TextWrappingModes.NoWrap;

            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = row;
            return button;
        }

        void BuildFooter(Transform col)
        {
            var footer = new GameObject("Footer", typeof(VerticalLayoutGroup)).transform;
            footer.SetParent(col, false);
            var layout = footer.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(layout, S(14), TextAnchor.MiddleCenter);
            layout.padding = new RectOffset(0, 0, Si(20), 0);
            layout.childForceExpandWidth = false;

            var rule = new GameObject("Rule", typeof(Image), typeof(LayoutElement));
            rule.transform.SetParent(footer, false);
            var ruleImage = rule.GetComponent<Image>();
            ruleImage.color = MemoryPathPalette.CardBorder;
            ruleImage.raycastTarget = false;
            var ruleFit = rule.GetComponent<LayoutElement>();
            ruleFit.minWidth = ruleFit.preferredWidth = S(120);
            ruleFit.minHeight = ruleFit.preferredHeight = 2f;
            ruleFit.flexibleWidth = 0f;
            ruleFit.flexibleHeight = 0f;

            var brand = new GameObject("Brand", typeof(HorizontalLayoutGroup)).transform;
            brand.SetParent(footer, false);
            var brandLayout = brand.GetComponent<HorizontalLayoutGroup>();
            UiDraw.Horizontal(brandLayout, S(12), TextAnchor.MiddleLeft);
            brandLayout.childForceExpandWidth = false;

            var logo = UiDraw.Panel(brand, "Logo", MemoryPathPalette.LogoWell, UiDraw.Circle);
            UiDraw.Stroke(logo, MemoryPathPalette.CardBorder);
            UiDraw.DropShadow(logo, new Color(0f, 0f, 0f, 0.06f), new Vector2(0f, -S(4)));
            UiDraw.Fit(logo, S(44), S(44));
            var mark = UiDraw.Icon(logo.transform, "Mark", UiDraw.ResourceSprite(NixinBrand.Logo), S(40));
            MemoryPathMenus.Center(mark.rectTransform, S(40));

            var names = new GameObject("Names", typeof(VerticalLayoutGroup)).transform;
            names.SetParent(brand, false);
            var namesLayout = names.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(namesLayout, S(2), TextAnchor.MiddleLeft);
            namesLayout.childForceExpandWidth = false;
            var studio = UiDraw.Label(names, "Studio", StudioCopy, S(16), UiWeight.SemiBold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
            studio.textWrappingMode = TextWrappingModes.NoWrap;
            var tag = UiDraw.Label(names, "Tag", StudioTagCopy, S(11), UiWeight.SemiBold, MemoryPathPalette.Body, TextAnchor.MiddleLeft);
            tag.textWrappingMode = TextWrappingModes.NoWrap;

            var contact = new GameObject("Contact", typeof(VerticalLayoutGroup)).transform;
            contact.SetParent(footer, false);
            var contactLayout = contact.GetComponent<VerticalLayoutGroup>();
            UiDraw.Vertical(contactLayout, S(2), TextAnchor.MiddleCenter);
            contactLayout.childForceExpandWidth = false;
            var contactCap = UiDraw.Label(contact, "Cap", ContactCopy, S(11), UiWeight.Bold, MemoryPathPalette.Body);
            contactCap.textWrappingMode = TextWrappingModes.NoWrap;

            var email = UiDraw.Label(contact, "Email", EmailCopy, S(13), UiWeight.SemiBold, MemoryPathPalette.Ocean);
            email.textWrappingMode = TextWrappingModes.NoWrap;
            email.fontStyle = FontStyles.Underline;
            email.raycastTarget = true;
            _email = email.gameObject.AddComponent<Button>();
            _email.targetGraphic = email;

            var copy = UiDraw.Label(footer, "Copyright", CopyrightCopy, S(10), UiWeight.Regular, MemoryPathPalette.Body);
            copy.textWrappingMode = TextWrappingModes.NoWrap;
        }

        [Serializable]
        sealed class ToggleRow
        {
            public Button Button;
            public Image Knob;
            public Image Track;

            public static ToggleRow Create(Transform parent, string name, string title, string blurb)
            {
                var row = UiDraw.Panel(parent, name, Color.white);
                UiDraw.SetCornerRadius(row, S(16));
                UiDraw.Stroke(row, MemoryPathPalette.CardBorder);
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                UiDraw.Horizontal(layout, S(12), TextAnchor.MiddleCenter, expandWidth: false);
                layout.padding = new RectOffset(Si(16), Si(16), Si(14), Si(14));

                var copy = new GameObject("Copy", typeof(VerticalLayoutGroup), typeof(LayoutElement)).transform;
                copy.SetParent(row.transform, false);
                var copyLayout = copy.GetComponent<VerticalLayoutGroup>();
                UiDraw.Vertical(copyLayout, S(2), TextAnchor.MiddleLeft);
                copyLayout.childForceExpandWidth = false;
                copy.GetComponent<LayoutElement>().flexibleWidth = 1f;
                var titleLabel = UiDraw.Label(copy, "T", title, S(15), UiWeight.Bold, MemoryPathPalette.Ink, TextAnchor.MiddleLeft);
                titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
                var blurbLabel = UiDraw.Label(copy, "B", blurb, S(13), UiWeight.Regular, MemoryPathPalette.Body, TextAnchor.MiddleLeft);
                blurbLabel.textWrappingMode = TextWrappingModes.NoWrap;

                var track = UiDraw.Panel(row.transform, "Track", MemoryPathPalette.Ocean, UiDraw.Rounded);
                UiDraw.SetCornerRadius(track, S(13));
                FitToggleTrack(track);
                var knob = UiDraw.Panel(track.transform, "Knob", Color.white, UiDraw.Circle);
                var knobRect = knob.rectTransform;
                knobRect.anchorMin = knobRect.anchorMax = new Vector2(1f, 0.5f);
                knobRect.pivot = new Vector2(1f, 0.5f);
                knobRect.anchoredPosition = new Vector2(-S(2), 0f);
                knobRect.sizeDelta = new Vector2(S(22), S(22));

                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = row;
                return new ToggleRow { Button = button, Track = track, Knob = knob };
            }

            public void Wire(Action onClick)
            {
                if (Button == null)
                    return;
                Button.onClick.RemoveAllListeners();
                if (onClick != null)
                    Button.onClick.AddListener(() => onClick());
            }

            public void Set(bool on)
            {
                if (Track == null || Knob == null)
                    return;
                Track.color = on ? MemoryPathPalette.Ocean : MemoryPathPalette.LockedTile;
                var rect = Knob.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
                rect.pivot = new Vector2(on ? 1f : 0f, 0.5f);
                rect.anchoredPosition = new Vector2(on ? -S(2) : S(2), 0f);
            }

            public static bool IsUsable(ToggleRow row) =>
                row != null && row.Button != null && row.Track != null && row.Knob != null;

            public static ToggleRow From(Transform row)
            {
                if (row == null)
                    return null;
                var button = row.GetComponent<Button>();
                var trackTransform = row.Find("Track");
                var track = trackTransform != null ? trackTransform.GetComponent<Image>() : null;
                var knobTransform = trackTransform != null ? trackTransform.Find("Knob") : null;
                var knob = knobTransform != null ? knobTransform.GetComponent<Image>() : null;
                if (button == null || track == null || knob == null)
                    return null;
                return new ToggleRow { Button = button, Track = track, Knob = knob };
            }
        }
    }
}
