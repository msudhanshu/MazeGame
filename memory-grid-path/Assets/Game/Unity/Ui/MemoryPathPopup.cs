using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Nixin.Ui;

namespace Game.Unity.Ui
{
    public enum MemoryPathPopupChrome
    {
        Exit,
        Complete,
        Fail,
        Pause
    }

    public enum MemoryPathGlyph
    {
        None,
        Heart,
        Pause
    }

    public sealed class MemoryPathButton
    {
        public string Label;
        public Color Fill;
        public Color LabelColor = Color.white;
        public Color Border;
        public Color Shadow;
        public UiWeight Weight = UiWeight.Bold;
        public float Height;
        public Action OnClick;
    }

    public sealed class MemoryPathPopupPayload
    {
        public MemoryPathPopupChrome Chrome = MemoryPathPopupChrome.Exit;
        public Color Panel = MemoryPathPalette.PopupCream;
        public Color KickerColor = MemoryPathPalette.KickerExit;
        public Color TitleColor = MemoryPathPalette.PopupInk;
        public UiWeight TitleWeight = UiWeight.ExtraBold;
        public float TitleSize;
        public float KickerSize;
        public float MessageSize;
        public float PanelWidth = MemoryPathPopup.PanelWidth;
        public string Kicker;
        public string Title;
        public string Message;
        public MemoryPathGlyph Glyph;
        public int Stars;
        public string ScoreLabel = "Level Score:";
        public string ScoreLine;
        public bool CloseOnBackdrop = true;
        public bool StackButtons;
        public bool ShowCloseButton;
        public float AutoCloseSeconds;
        public MemoryPathButton[] Buttons;
    }

    /// <summary>Figma cream dialog used for exit, skip, complete, fail, and pause.</summary>
    public sealed class MemoryPathPopup : UiView<MemoryPathPopupPayload>
    {
        public const float FigmaWidth = 322f;
        public const float PanelWidth = 720f;

        [SerializeField] TextMeshProUGUI _kicker;
        [SerializeField] TextMeshProUGUI _title;
        [SerializeField] TextMeshProUGUI _message;
        [SerializeField] Image _glyphCircle;
        [SerializeField] Image _glyphStroke;
        [SerializeField] Image _glyphInner;
        [SerializeField] Image _heart;
        [SerializeField] Transform _pauseMark;
        [SerializeField] Transform _stars;
        [SerializeField] Image[] _starDots;
        [SerializeField] Image[] _starIcons;
        [SerializeField] Image _scoreChip;
        [SerializeField] TextMeshProUGUI _scoreCaption;
        [SerializeField] TextMeshProUGUI _score;
        [SerializeField] VerticalLayoutGroup _bodyLayout;
        [SerializeField] Transform _row;
        [SerializeField] Transform _stack;
        [SerializeField] ButtonSlot[] _rowSlots;
        [SerializeField] ButtonSlot[] _stackSlots;
        [SerializeField] Image _panel;
        [SerializeField] Transform _confettiLeft;
        [SerializeField] Transform _confettiRight;
        [SerializeField] HorizontalLayoutGroup _confettiLeftLayout;
        [SerializeField] HorizontalLayoutGroup _confettiRightLayout;
        [SerializeField] Button _closeButton;
        MemoryPathPopupPayload _payload;
        Coroutine _autoClose;
        Color _titleRest;

        public bool HasBuiltLayout => _panel != null;

        public static float S(float fig) => fig * (PanelWidth / FigmaWidth);

        static int Si(float fig) => Mathf.RoundToInt(S(fig));

        public static MemoryPathPopup CreateTemplate()
        {
            var go = new GameObject("MemoryPathPopup", typeof(RectTransform), typeof(MemoryPathPopup));
            var popup = go.GetComponent<MemoryPathPopup>();
            popup.Build();
            go.SetActive(false);
            return popup;
        }

        public override void Bind(MemoryPathPopupPayload payload)
        {
            StopAutoClose();
            _payload = payload ?? new MemoryPathPopupPayload();
            if (_panel == null)
                Build();

            SetCloseOnBackdrop(_payload.CloseOnBackdrop);
            _panel.color = _payload.Panel;
            var panelWidth = _payload.PanelWidth > 0f ? _payload.PanelWidth : PanelWidth;
            _panel.rectTransform.sizeDelta = new Vector2(panelWidth, _panel.rectTransform.sizeDelta.y);

            var kickerSize = _payload.KickerSize > 0f ? S(_payload.KickerSize) : S(12);
            _kicker.text = (_payload.Kicker ?? "").ToUpperInvariant();
            _kicker.color = _payload.KickerColor;
            _kicker.fontSize = kickerSize;
            _kicker.characterSpacing = 100f / Mathf.Max(1f, kickerSize / S(12f));

            var titleSize = _payload.TitleSize > 0f ? S(_payload.TitleSize) : TitleSize(_payload.Chrome);
            _title.text = _payload.Title ?? "";
            _title.color = _payload.TitleColor;
            _title.font = UiDraw.FontOf(_payload.TitleWeight);
            _title.fontSize = titleSize;
            _title.alignment = Centered(_payload.Chrome)
                ? TextAlignmentOptions.Center
                : TextAlignmentOptions.MidlineLeft;

            _message.text = _payload.Message ?? "";
            _message.gameObject.SetActive(!string.IsNullOrEmpty(_payload.Message));
            _message.alignment = _title.alignment;
            _message.fontSize = _payload.MessageSize > 0f ? S(_payload.MessageSize) : S(14);

            BindGlyph(_payload.Glyph);
            BindStars(_payload.Stars);
            BindScore(_payload.ScoreLabel, _payload.ScoreLine);
            BindConfetti(_payload.Chrome);

            _bodyLayout.childAlignment = Centered(_payload.Chrome)
                ? TextAnchor.UpperCenter
                : TextAnchor.UpperLeft;
            _bodyLayout.spacing = BodyGap(_payload.Chrome);

            var buttons = _payload.Buttons ?? Array.Empty<MemoryPathButton>();
            var hasButtons = buttons.Length > 0;
            var stacked = hasButtons && (_payload.StackButtons || buttons.Length > 2);
            _row.gameObject.SetActive(hasButtons && !stacked);
            _stack.gameObject.SetActive(hasButtons && stacked);
            if (hasButtons)
            {
                if (stacked)
                    EnsureSlots(ref _stackSlots, _stack, buttons.Length);
                else
                    EnsureSlots(ref _rowSlots, _row, buttons.Length);
                BindButtons(stacked ? _stackSlots : _rowSlots, buttons);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel.rectTransform);
            BindCloseButton();
            SetBackHandler(CloseSelf);
        }

        void CloseSelf()
        {
            StopAutoClose();
            if (UiNavigator.Current != null)
                UiNavigator.Current.Close();
            else
                gameObject.SetActive(false);
        }

        void BindCloseButton()
        {
            EnsureCloseButton();
            if (_closeButton == null)
                return;

            var show = _payload != null && _payload.ShowCloseButton;
            _closeButton.gameObject.SetActive(show);
            _closeButton.onClick.RemoveAllListeners();
            if (show)
                _closeButton.onClick.AddListener(CloseSelf);
        }

        void EnsureCloseButton()
        {
            if (_closeButton != null)
                return;
            if (_panel == null)
                return;

            var existing = _panel.transform.Find("Close");
            if (existing != null)
            {
                _closeButton = existing.GetComponent<Button>();
                if (_closeButton != null)
                    return;
            }

            BuildCloseButton();
        }

        void BuildCloseButton()
        {
            var size = S(36);
            var image = UiDraw.Panel(_panel.transform, "Close", new Color(1f, 1f, 1f, 0.9f), UiDraw.Circle);
            var ignore = image.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-S(10), -S(10));
            rect.sizeDelta = new Vector2(size, size);

            var mark = UiDraw.Label(image.transform, "X", "×", S(22), UiWeight.Bold, MemoryPathPalette.PopupInk);
            UiDraw.Stretch(mark.rectTransform);
            mark.alignment = TextAlignmentOptions.Center;
            mark.raycastTarget = false;

            _closeButton = image.gameObject.AddComponent<Button>();
            _closeButton.targetGraphic = image;
            _closeButton.gameObject.SetActive(false);
        }

        public override void OnOpened()
        {
            TryStartAutoClose();
        }

        void OnEnable()
        {
            TryStartAutoClose();
        }

        public override void OnClosed()
        {
            StopAutoClose();
        }

        void StopAutoClose()
        {
            if (_autoClose != null)
            {
                StopCoroutine(_autoClose);
                _autoClose = null;
            }

            if (_title != null)
            {
                _title.color = _payload != null ? _payload.TitleColor : _titleRest;
                _title.rectTransform.localScale = Vector3.one;
            }
        }

        void TryStartAutoClose()
        {
            if (_autoClose != null)
                return;
            if (_payload == null || _payload.AutoCloseSeconds <= 0f)
                return;
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;

            if (_title != null)
                _titleRest = _title.color;
            _autoClose = StartCoroutine(AutoCloseRoutine());
        }

        IEnumerator AutoCloseRoutine()
        {
            var seconds = _payload != null ? _payload.AutoCloseSeconds : 0f;
            var elapsed = 0f;
            while (elapsed < seconds && _title != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var wave = (Mathf.Sin(elapsed * 10f) + 1f) * 0.5f;
                _title.color = Color.Lerp(_titleRest, Color.white, wave);
                _title.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.06f, wave);
                yield return null;
            }

            if (_title != null)
            {
                _title.color = _titleRest;
                _title.rectTransform.localScale = Vector3.one;
            }

            _autoClose = null;
            if (IsOpen)
                CloseSelf();
        }

        static bool Centered(MemoryPathPopupChrome chrome)
        {
            return chrome == MemoryPathPopupChrome.Complete
                || chrome == MemoryPathPopupChrome.Fail
                || chrome == MemoryPathPopupChrome.Pause;
        }

        static float TitleSize(MemoryPathPopupChrome chrome)
        {
            switch (chrome)
            {
                case MemoryPathPopupChrome.Complete:
                    return S(24);
                case MemoryPathPopupChrome.Fail:
                    return S(20);
                case MemoryPathPopupChrome.Pause:
                    return S(15);
                default:
                    return S(18);
            }
        }

        static float BodyGap(MemoryPathPopupChrome chrome)
        {
            switch (chrome)
            {
                case MemoryPathPopupChrome.Pause:
                    return S(8);
                case MemoryPathPopupChrome.Exit:
                    return S(6);
                default:
                    return S(12);
            }
        }

        void BindGlyph(MemoryPathGlyph glyph)
        {
            var show = glyph != MemoryPathGlyph.None;
            // _glyphCircle.gameObject.SetActive(show);
            // Temp manjeet
            _glyphCircle.gameObject.SetActive(false);
            if (!show)
                return;

            var fail = glyph == MemoryPathGlyph.Heart;
            _glyphCircle.color = fail ? MemoryPathPalette.GlyphFail : MemoryPathPalette.GlyphPause;
            _glyphStroke.color = fail ? MemoryPathPalette.GlyphFailBorder : MemoryPathPalette.GlyphPauseBorder;
            _glyphInner.color = _glyphCircle.color;
            var shadow = _glyphCircle.GetComponent<Shadow>();
            if (shadow != null)
            {
                shadow.effectColor = fail
                    ? new Color(0.937f, 0.267f, 0.267f, 0.2f)
                    : new Color(0.388f, 0.4f, 0.945f, 0.2f);
            }

            _heart.gameObject.SetActive(fail);
            _pauseMark.gameObject.SetActive(!fail);
            if (fail && _heart.sprite == null)
            {
                var sprite = UiDraw.ResourceSprite("MemoryPath/heart-crack");
                _heart.enabled = sprite != null;
                _heart.sprite = sprite;
            }
        }

        void BindStars(int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            _stars.gameObject.SetActive(stars > 0);
            if (stars <= 0)
                return;

            var side = _starIcons.Length > 0 ? _starIcons[0].sprite : null;
            if (side == null)
                side = UiDraw.ResourceSprite("MemoryPath/star");
            var center = UiDraw.ResourceSprite("MemoryPath/star-center") ?? side;
            for (var i = 0; i < _starIcons.Length; i++)
            {
                var on = i < stars;
                _starIcons[i].gameObject.SetActive(side != null && on);
                _starDots[i].gameObject.SetActive(side == null);
                if (side != null)
                    _starIcons[i].sprite = i == 1 ? center : side;
                else
                    _starDots[i].color = on ? MemoryPathPalette.LevelsButton : MemoryPathPalette.LockedTile;
            }
        }

        void BindScore(string caption, string value)
        {
            var hasScore = !string.IsNullOrEmpty(value);
            _scoreChip.gameObject.SetActive(hasScore);
            if (!hasScore)
                return;

            _scoreCaption.text = caption ?? "Level Score:";
            _score.text = value;
        }

        void BindButtons(ButtonSlot[] slots, MemoryPathButton[] buttons)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                var used = i < buttons.Length && buttons[i] != null && !string.IsNullOrEmpty(buttons[i].Label);
                slots[i].Root.SetActive(used);
                if (!used)
                    continue;

                var spec = buttons[i];
                if (spec.Height > 0f)
                    slots[i].Fit.minHeight = slots[i].Fit.preferredHeight = spec.Height;
                var height = slots[i].Fit.preferredHeight;
                slots[i].Label.text = spec.Label;
                slots[i].Label.color = spec.LabelColor;
                slots[i].Label.font = UiDraw.FontOf(spec.Weight);
                var figFont = spec.Weight == UiWeight.ExtraBold
                    ? 16f
                    : (height <= S(42.5f) ? 14f : 15f);
                slots[i].Label.fontSize = S(figFont);

                var bordered = spec.Border.a > 0.01f;
                slots[i].Fill.gameObject.SetActive(bordered);
                if (bordered)
                {
                    slots[i].Outer.color = spec.Border;
                    slots[i].Fill.color = spec.Fill;
                }
                else
                {
                    slots[i].Outer.color = spec.Fill;
                }

                slots[i].Shadow.enabled = spec.Shadow.a > 0.01f;
                if (slots[i].Shadow.enabled)
                    slots[i].Shadow.effectColor = spec.Shadow;

                slots[i].Button.onClick.RemoveAllListeners();
                var action = spec.OnClick;
                slots[i].Button.onClick.AddListener(() =>
                {
                    UiNavigator.Current?.Close();
                    action?.Invoke();
                });
            }
        }

        void BindConfetti(MemoryPathPopupChrome chrome)
        {
            ShowConfetti(_confettiLeft, chrome);
            ShowConfetti(_confettiRight, chrome);
        }

        static void ShowConfetti(Transform bar, MemoryPathPopupChrome chrome)
        {
            if (bar == null)
                return;
            var name = chrome.ToString();
            for (var i = 0; i < bar.childCount; i++)
            {
                var child = bar.GetChild(i);
                child.gameObject.SetActive(child.name == name);
            }
        }

        void Build()
        {
            var rect = GetComponent<RectTransform>();
            UiDraw.Stretch(rect);

            _panel = UiDraw.Panel(transform, "Panel", MemoryPathPalette.PopupCream);
            var panelRect = _panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, 400f);
            UiDraw.SetCornerRadius(_panel, S(20));
            UiDraw.DropShadow(_panel, new Color(0f, 0f, 0f, 0.25f), new Vector2(0f, -S(12)));

            var layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(Si(20), Si(20), Si(20), Si(20));
            layout.spacing = S(16);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = _panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _confettiLeft = MakeConfettiBar(_panel.transform, "ConfettiLeft", new Vector2(0f, 1f), new Vector2(S(16), -S(10)), out _confettiLeftLayout);
            _confettiRight = MakeConfettiBar(_panel.transform, "ConfettiRight", new Vector2(1f, 1f), new Vector2(-S(60), -S(10)), out _confettiRightLayout);
            var rightRect = _confettiRight.GetComponent<RectTransform>();
            rightRect.pivot = new Vector2(1f, 1f);
            BuildConfettiVariants();

            _kicker = UiDraw.Label(_panel.transform, "Kicker", "", S(12), UiWeight.Bold, MemoryPathPalette.KickerExit, TextAnchor.MiddleLeft);
            _kicker.characterSpacing = 100f / 12f;
            PreferHeight(_kicker, S(15));

            var body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            body.SetParent(_panel.transform, false);
            _bodyLayout = body.GetComponent<VerticalLayoutGroup>();
            _bodyLayout.spacing = S(6);
            _bodyLayout.childAlignment = TextAnchor.UpperLeft;
            _bodyLayout.childControlWidth = true;
            _bodyLayout.childControlHeight = true;
            _bodyLayout.childForceExpandWidth = true;
            _bodyLayout.childForceExpandHeight = false;

            BuildGlyph(body);
            _title = UiDraw.Label(body, "Title", "", S(18), UiWeight.ExtraBold, MemoryPathPalette.PopupInk, TextAnchor.MiddleLeft);
            _message = UiDraw.Label(body, "Message", "", S(14), UiWeight.Regular, MemoryPathPalette.Body, TextAnchor.MiddleLeft);
            _message.textWrappingMode = TextWrappingModes.Normal;
            BuildStars(body);
            BuildScore(body);

            _row = MakeButtonGroup(_panel.transform, "Row", true, 2, out _rowSlots);
            _stack = MakeButtonGroup(_panel.transform, "Stack", false, 4, out _stackSlots);
            BuildCloseButton();
        }

        static void EnsureSlots(ref ButtonSlot[] slots, Transform container, int requiredCount)
        {
            if (slots != null && slots.Length >= requiredCount)
                return;

            var existingCount = slots != null ? slots.Length : 0;
            var list = slots != null ? new List<ButtonSlot>(slots) : new List<ButtonSlot>();
            for (var i = existingCount; i < requiredCount; i++)
            {
                list.Add(MakeSlot(container, i));
            }
            slots = list.ToArray();
        }

        void BuildGlyph(Transform parent)
        {
            _glyphCircle = UiDraw.Panel(parent, "Glyph", MemoryPathPalette.GlyphFail, UiDraw.Circle);
            var size = S(64);
            var fit = _glyphCircle.gameObject.AddComponent<LayoutElement>();
            fit.minWidth = fit.preferredWidth = fit.minHeight = fit.preferredHeight = size;
            fit.flexibleWidth = 0;
            UiDraw.DropShadow(_glyphCircle, new Color(0.937f, 0.267f, 0.267f, 0.2f), new Vector2(0f, -S(4)));

            _glyphStroke = UiDraw.Panel(_glyphCircle.transform, "Stroke", MemoryPathPalette.GlyphFailBorder, UiDraw.Circle);
            UiDraw.Stretch(_glyphStroke.rectTransform);
            _glyphStroke.raycastTarget = false;
            var inner = UiDraw.Panel(_glyphCircle.transform, "Inner", MemoryPathPalette.GlyphFail, UiDraw.Circle);
            _glyphInner = inner;
            UiDraw.Stretch(inner.rectTransform);
            var inset = S(1);
            inner.rectTransform.offsetMin = new Vector2(inset, inset);
            inner.rectTransform.offsetMax = new Vector2(-inset, -inset);
            inner.raycastTarget = false;

            _heart = UiDraw.Icon(inner.transform, "Heart", UiDraw.ResourceSprite("MemoryPath/heart-crack"), S(32));
            Center(_heart.rectTransform);

            _pauseMark = new GameObject("Pause", typeof(HorizontalLayoutGroup)).transform;
            _pauseMark.SetParent(inner.transform, false);
            var pauseRect = _pauseMark.GetComponent<RectTransform>();
            Center(pauseRect);
            pauseRect.sizeDelta = new Vector2(S(22), S(22));
            var pauseLayout = _pauseMark.GetComponent<HorizontalLayoutGroup>();
            pauseLayout.spacing = S(5);
            pauseLayout.childAlignment = TextAnchor.MiddleCenter;
            pauseLayout.childForceExpandWidth = false;
            pauseLayout.childForceExpandHeight = false;
            MakePauseBar(_pauseMark);
            MakePauseBar(_pauseMark);

            _glyphCircle.gameObject.SetActive(false);
        }

        void MakePauseBar(Transform parent)
        {
            var bar = UiDraw.Panel(parent, "Bar", Color.black, UiDraw.Rounded);
            UiDraw.SetCornerRadius(bar, S(1.5f));
            var fit = bar.gameObject.AddComponent<LayoutElement>();
            fit.minWidth = fit.preferredWidth = S(6);
            fit.minHeight = fit.preferredHeight = S(18);
            bar.raycastTarget = false;
        }

        void BuildStars(Transform parent)
        {
            _stars = new GameObject("Stars", typeof(HorizontalLayoutGroup)).transform;
            _stars.SetParent(parent, false);
            var starLayout = _stars.GetComponent<HorizontalLayoutGroup>();
            starLayout.spacing = S(4);
            starLayout.childAlignment = TextAnchor.MiddleCenter;
            starLayout.childForceExpandWidth = false;
            starLayout.childForceExpandHeight = false;
            starLayout.childControlWidth = true;
            starLayout.childControlHeight = true;
            var starsFit = _stars.gameObject.AddComponent<LayoutElement>();
            starsFit.flexibleWidth = 0;
            starsFit.minHeight = starsFit.preferredHeight = S(32);

            _starDots = new Image[3];
            _starIcons = new Image[3];
            for (var i = 0; i < 3; i++)
            {
                var size = i == 1 ? S(32) : S(28);
                var starSprite = UiDraw.ResourceSprite(i == 1 ? "MemoryPath/star-center" : "MemoryPath/star")
                    ?? UiDraw.ResourceSprite("MemoryPath/star");
                _starIcons[i] = UiDraw.Icon(_stars, "StarIcon", starSprite, size);
                var iconFit = _starIcons[i].gameObject.AddComponent<LayoutElement>();
                iconFit.minWidth = iconFit.preferredWidth = iconFit.minHeight = iconFit.preferredHeight = size;
                _starDots[i] = UiDraw.Panel(_stars, "StarDot", MemoryPathPalette.LevelsButton, UiDraw.Circle);
                _starDots[i].rectTransform.sizeDelta = new Vector2(size, size);
                _starDots[i].raycastTarget = false;
                var dotFit = _starDots[i].gameObject.AddComponent<LayoutElement>();
                dotFit.minWidth = dotFit.preferredWidth = dotFit.minHeight = dotFit.preferredHeight = size;
                _starDots[i].gameObject.SetActive(false);
            }

            _stars.gameObject.SetActive(false);
        }

        void BuildScore(Transform parent)
        {
            _scoreChip = UiDraw.Panel(parent, "ScoreChip", MemoryPathPalette.ScoreChipBorder);
            UiDraw.SetCornerRadius(_scoreChip, S(12));
            var chipLayout = _scoreChip.gameObject.AddComponent<HorizontalLayoutGroup>();
            chipLayout.padding = new RectOffset(Si(16), Si(16), Si(8), Si(8));
            chipLayout.spacing = S(12);
            chipLayout.childAlignment = TextAnchor.MiddleCenter;
            chipLayout.childForceExpandWidth = false;
            chipLayout.childForceExpandHeight = false;
            chipLayout.childControlWidth = true;
            chipLayout.childControlHeight = true;
            var chipFit = _scoreChip.gameObject.AddComponent<LayoutElement>();
            chipFit.flexibleWidth = 0;
            var fill = UiDraw.Panel(_scoreChip.transform, "Fill", MemoryPathPalette.ScoreChip);
            fill.raycastTarget = false;
            UiDraw.Stretch(fill.rectTransform);
            var inset = S(1);
            fill.rectTransform.offsetMin = new Vector2(inset, inset);
            fill.rectTransform.offsetMax = new Vector2(-inset, -inset);
            UiDraw.SetCornerRadius(fill, S(11));
            fill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            _scoreCaption = UiDraw.Label(_scoreChip.transform, "Caption", "Level Score:", S(14), UiWeight.Regular, MemoryPathPalette.ScoreCaption, TextAnchor.MiddleLeft);
            _score = UiDraw.Label(_scoreChip.transform, "Score", "", S(16), UiWeight.ExtraBold, MemoryPathPalette.ScoreValue, TextAnchor.MiddleLeft);
            _scoreChip.gameObject.SetActive(false);
        }

        Transform MakeButtonGroup(Transform parent, string name, bool horizontal, int count, out ButtonSlot[] slots)
        {
            var go = new GameObject(name, horizontal ? typeof(HorizontalLayoutGroup) : typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);
            if (horizontal)
            {
                var row = go.GetComponent<HorizontalLayoutGroup>();
                row.spacing = S(12);
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childForceExpandWidth = true;
                row.childForceExpandHeight = true;
                row.childControlWidth = true;
                row.childControlHeight = true;
            }
            else
            {
                var stack = go.GetComponent<VerticalLayoutGroup>();
                stack.spacing = S(10);
                stack.childAlignment = TextAnchor.UpperCenter;
                stack.childForceExpandWidth = true;
                stack.childForceExpandHeight = false;
                stack.childControlWidth = true;
                stack.childControlHeight = true;
            }

            slots = new ButtonSlot[count];
            for (var i = 0; i < count; i++)
                slots[i] = MakeSlot(go.transform, i);
            return go.transform;
        }

        static ButtonSlot MakeSlot(Transform parent, int index)
        {
            var outer = UiDraw.Panel(parent, "Btn" + index, MemoryPathPalette.PlayButton);
            UiDraw.SetCornerRadius(outer, S(14));
            var button = outer.gameObject.AddComponent<Button>();
            button.targetGraphic = outer;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f);
            button.colors = colors;

            var fit = outer.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = fit.preferredHeight = S(43);

            var fill = UiDraw.Panel(outer.transform, "Fill", MemoryPathPalette.Stay);
            UiDraw.Stretch(fill.rectTransform);
            fill.raycastTarget = false;
            fill.gameObject.SetActive(false);

            var label = UiDraw.Label(outer.transform, "Label", "", S(15), UiWeight.Bold, Color.white);
            UiDraw.Stretch(label.rectTransform);

            var shadow = UiDraw.DropShadow(outer, Color.clear, new Vector2(0f, -S(4)));
            shadow.enabled = false;

            return new ButtonSlot
            {
                Root = outer.gameObject,
                Button = button,
                Outer = outer,
                Fill = fill,
                Label = label,
                Shadow = shadow,
                Fit = fit
            };
        }

        void BuildConfettiVariants()
        {
            var exitLeft = MakeConfettiVariant(_confettiLeft, MemoryPathPopupChrome.Exit, S(6));
            Bit(exitLeft, true, 7, MemoryPathPalette.ConfettiRed, 0);
            Bit(exitLeft, true, 7, MemoryPathPalette.ConfettiYellow, 0);
            Bit(exitLeft, true, 7, MemoryPathPalette.ConfettiGreen, 0);
            Bit(exitLeft, true, 7, MemoryPathPalette.ConfettiBlue, 0);
            Bit(exitLeft, true, 5, MemoryPathPalette.ConfettiRed, 0);
            var exitRight = MakeConfettiVariant(_confettiRight, MemoryPathPopupChrome.Exit, S(8));
            Bit(exitRight, false, 6, MemoryPathPalette.ConfettiYellow, 30);
            Bit(exitRight, false, 6, MemoryPathPalette.ConfettiBlue, 15);
            Bit(exitRight, false, 6, MemoryPathPalette.ConfettiRed, 45);

            var winLeft = MakeConfettiVariant(_confettiLeft, MemoryPathPopupChrome.Complete, S(5));
            Bit(winLeft, true, 8, MemoryPathPalette.ConfettiYellow, 0);
            Bit(winLeft, true, 6, MemoryPathPalette.ConfettiGreen, 0);
            Bit(winLeft, false, 7, MemoryPathPalette.ConfettiRed, 45);
            Bit(winLeft, true, 6, MemoryPathPalette.ConfettiBlue, 0);
            Bit(winLeft, false, 5, MemoryPathPalette.ConfettiYellow, 45);
            var winRight = MakeConfettiVariant(_confettiRight, MemoryPathPopupChrome.Complete, S(5));
            Bit(winRight, false, 6, MemoryPathPalette.ConfettiRed, 30);
            Bit(winRight, true, 7, MemoryPathPalette.ConfettiBlue, 0);
            Bit(winRight, false, 5, MemoryPathPalette.ConfettiGreen, 45);

            var failLeft = MakeConfettiVariant(_confettiLeft, MemoryPathPopupChrome.Fail, S(7));
            Bit(failLeft, true, 7, MemoryPathPalette.ConfettiOrange, 0);
            Bit(failLeft, false, 6, MemoryPathPalette.ConfettiGold, 45);
            Bit(failLeft, true, 5, MemoryPathPalette.ConfettiPurple, 0);
            Bit(failLeft, false, 7, MemoryPathPalette.ConfettiPink, 45);
            var failRight = MakeConfettiVariant(_confettiRight, MemoryPathPopupChrome.Fail, S(6));
            Bit(failRight, true, 6, MemoryPathPalette.ConfettiMint, 0);
            Bit(failRight, false, 5, MemoryPathPalette.ConfettiOrange, 45);
            Bit(failRight, true, 7, MemoryPathPalette.ConfettiSky, 0);

            var pauseLeft = MakeConfettiVariant(_confettiLeft, MemoryPathPopupChrome.Pause, S(6));
            Bit(pauseLeft, true, 7, MemoryPathPalette.ConfettiIndigo, 0);
            Bit(pauseLeft, false, 6, MemoryPathPalette.ConfettiMint, 30);
            Bit(pauseLeft, true, 5, MemoryPathPalette.ConfettiPink, 0);
            Bit(pauseLeft, false, 7, MemoryPathPalette.ConfettiGold, 45);
            var pauseRight = MakeConfettiVariant(_confettiRight, MemoryPathPopupChrome.Pause, S(5));
            Bit(pauseRight, true, 6, MemoryPathPalette.ConfettiOrange, 0);
            Bit(pauseRight, false, 5, MemoryPathPalette.ConfettiIndigo, 45);
            Bit(pauseRight, true, 7, MemoryPathPalette.ConfettiMint, 0);

            ShowConfetti(_confettiLeft, MemoryPathPopupChrome.Exit);
            ShowConfetti(_confettiRight, MemoryPathPopupChrome.Exit);
        }

        static Transform MakeConfettiVariant(Transform bar, MemoryPathPopupChrome chrome, float spacing)
        {
            var go = new GameObject(chrome.ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(bar, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return go.transform;
        }

        static Transform MakeConfettiBar(Transform parent, string name, Vector2 anchor, Vector2 pos, out HorizontalLayoutGroup layout)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            return go.transform;
        }

        static void Bit(Transform parent, bool circle, float figSize, Color color, float rotation)
        {
            var size = S(figSize);
            var slot = new GameObject("Bit", typeof(RectTransform), typeof(LayoutElement));
            slot.transform.SetParent(parent, false);
            var fit = slot.GetComponent<LayoutElement>();
            fit.minWidth = fit.preferredWidth = fit.minHeight = fit.preferredHeight = size;
            var piece = UiDraw.Panel(slot.transform, "G", color, circle ? UiDraw.Circle : UiDraw.Rounded);
            piece.raycastTarget = false;
            var rect = piece.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            if (!circle)
                UiDraw.SetCornerRadius(piece, S(2));
            if (Mathf.Abs(rotation) > 0.01f)
                rect.localEulerAngles = new Vector3(0f, 0f, rotation);
        }

        static void PreferHeight(Component target, float height)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = fit.preferredHeight = height;
        }

        static void Center(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        [Serializable]
        sealed class ButtonSlot
        {
            public GameObject Root;
            public Button Button;
            public Image Outer;
            public Image Fill;
            public TextMeshProUGUI Label;
            public Shadow Shadow;
            public LayoutElement Fit;
        }
    }
}
