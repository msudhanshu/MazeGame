using System.Collections;
using Game.Unity.View;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Video-style overlay for path scan: caption, progress, hold-anywhere to pause.
    /// </summary>
    public sealed class ScoutScanPlayer : MonoBehaviour, IRadarScanControls
    {
        public const string Caption = "Observe and Memorize the path and its landmarks.";
        public const float HeldAlpha = 1f;
        public const float FadeSeconds = 0.16f;

        static readonly Color CaptionPanelTint = new Color(0.04f, 0.06f, 0.10f, 0.9f);
        static readonly Color DockTint = new Color(0.04f, 0.05f, 0.09f, 0.92f);
        static readonly Color TrackTint = new Color(1f, 1f, 1f, 0.18f);
        static readonly Color FillTint = new Color(0.96f, 0.97f, 1f, 0.95f);
        static readonly Color ButtonTint = new Color(30f / 255f, 41f / 255f, 59f / 255f, 0.92f);

        CanvasGroup _group;
        TextMeshProUGUI _caption;
        TextMeshProUGUI _clock;
        Image _fill;
        RectTransform _pauseGlyph;
        RectTransform _playGlyph;
        Coroutine _fade;
        float _elapsed;
        float _total;

        public bool HeldPaused { get; private set; }

        public static ScoutScanPlayer Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            var existing = parent.GetComponentInChildren<ScoutScanPlayer>(true);
            if (existing != null)
                return existing;

            var go = new GameObject(
                "ScoutScanPlayer",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(ScoutScanPlayer));
            go.transform.SetParent(parent, false);
            var player = go.GetComponent<ScoutScanPlayer>();
            player.Build();
            player.HideImmediate();
            return player;
        }

        public static void HideOn(Transform parent, bool immediate = true)
        {
            if (parent == null)
                return;
            var existing = parent.GetComponentInChildren<ScoutScanPlayer>(true);
            if (existing == null)
                return;
            if (immediate)
                existing.HideImmediate();
            else
                existing.Hide();
        }

        public void Show()
        {
            EnsureBuilt();
            HeldPaused = false;
            SetProgress(0f, 0f);
            SetCaption(Caption);
            RefreshGlyphs();
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            if (!Application.isPlaying)
            {
                StopFade();
                if (_group != null)
                    _group.alpha = HeldAlpha;
                return;
            }

            FadeTo(HeldAlpha);
        }

        public void Hide()
        {
            HeldPaused = false;
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                HideImmediate();
                return;
            }

            FadeTo(0f, deactivate: true);
        }

        public void HideImmediate()
        {
            HeldPaused = false;
            StopFade();
            if (_group != null)
                _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        public RectTransform HoldControl
        {
            get
            {
                EnsureBuilt();
                return transform.Find("Hold") as RectTransform;
            }
        }

        public void SetCaption(string text)
        {
            EnsureBuilt();
            if (_caption != null)
                _caption.text = string.IsNullOrEmpty(text) ? Caption : text;
        }

        public void SetProgress(float elapsedSeconds, float totalSeconds)
        {
            EnsureBuilt();
            _elapsed = Mathf.Max(0f, elapsedSeconds);
            _total = Mathf.Max(0f, totalSeconds);
            var progress = _total <= 0.0001f ? 0f : Mathf.Clamp01(_elapsed / _total);
            if (_fill != null)
                _fill.fillAmount = progress;
            if (_clock != null)
                _clock.text = ScoutRotationMove.FormatClock(_elapsed)
                    + " / "
                    + ScoutRotationMove.FormatClock(_total);
        }

        public void SetHeld(bool held)
        {
            HeldPaused = held;
            RefreshGlyphs();
        }

        void Update()
        {
            if (!isActiveAndEnabled || _group != null && _group.alpha < 0.2f)
                return;
            SetHeld(PointerIsDown());
        }

        static bool PointerIsDown()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                return true;

            if (!EnhancedTouchSupport.enabled)
                EnhancedTouchSupport.Enable();

            return Touch.activeTouches.Count > 0;
        }

        void RefreshGlyphs()
        {
            if (_pauseGlyph != null)
                _pauseGlyph.gameObject.SetActive(!HeldPaused);
            if (_playGlyph != null)
                _playGlyph.gameObject.SetActive(HeldPaused);
        }

        void FadeTo(float target, bool deactivate = false)
        {
            StopFade();
            if (!gameObject.activeInHierarchy)
            {
                if (_group != null)
                    _group.alpha = target;
                if (deactivate)
                    gameObject.SetActive(false);
                return;
            }

            _fade = StartCoroutine(FadeRoutine(target, deactivate));
        }

        IEnumerator FadeRoutine(float target, bool deactivate)
        {
            var from = _group != null ? _group.alpha : 0f;
            var t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (_group != null)
                    _group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / FadeSeconds));
                yield return null;
            }

            if (_group != null)
                _group.alpha = target;
            _fade = null;
            if (deactivate)
                gameObject.SetActive(false);
        }

        void StopFade()
        {
            if (_fade == null)
                return;
            StopCoroutine(_fade);
            _fade = null;
        }

        void EnsureBuilt()
        {
            if (_caption == null || _group == null || _fill == null)
                Build();
        }

        void Build()
        {
            var root = transform as RectTransform;
            UiDraw.Stretch(root);

            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;
            _group.interactable = true;
            _group.alpha = 0f;

            var passthrough = gameObject.GetComponent<Image>();
            if (passthrough == null)
                passthrough = gameObject.AddComponent<Image>();
            passthrough.color = Color.clear;
            passthrough.raycastTarget = false;

            BuildCaption();
            BuildHoldButton();
            BuildDock();
            SetProgress(0f, 0f);
            RefreshGlyphs();
        }

        void BuildCaption()
        {
            var existing = transform.Find("Caption");
            var captionRoot = existing != null
                ? existing.gameObject
                : new GameObject("Caption", typeof(RectTransform), typeof(Image));
            captionRoot.transform.SetParent(transform, false);
            var rect = captionRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.68f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(900f, 180f);
            rect.anchoredPosition = Vector2.zero;

            var panel = captionRoot.GetComponent<Image>();
            panel.sprite = UiDraw.Rounded;
            panel.type = Image.Type.Sliced;
            panel.color = CaptionPanelTint;
            panel.raycastTarget = false;
            UiDraw.SetCornerRadius(panel, 24f);

            _caption = captionRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (_caption == null)
                _caption = UiDraw.Label(captionRoot.transform, "Label", Caption, 40f, UiWeight.ExtraBold, Color.white);
            _caption.text = Caption;
            _caption.alignment = TextAlignmentOptions.Center;
            _caption.textWrappingMode = TextWrappingModes.Normal;
            _caption.overflowMode = TextOverflowModes.Overflow;
            _caption.raycastTarget = false;
            var labelRect = _caption.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(36f, 22f);
            labelRect.offsetMax = new Vector2(-36f, -22f);
        }

        void BuildDock()
        {
            var existing = transform.Find("Dock");
            var dockGo = existing != null
                ? existing.gameObject
                : new GameObject("Dock", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            dockGo.transform.SetParent(transform, false);
            var dock = dockGo.GetComponent<RectTransform>();
            dock.anchorMin = new Vector2(0f, 0f);
            dock.anchorMax = new Vector2(1f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);
            dock.anchoredPosition = new Vector2(0f, 36f);
            dock.sizeDelta = new Vector2(-48f, 108f);

            var dockImage = dockGo.GetComponent<Image>();
            dockImage.sprite = UiDraw.Rounded;
            dockImage.type = Image.Type.Sliced;
            dockImage.color = DockTint;
            dockImage.raycastTarget = true;
            UiDraw.SetCornerRadius(dockImage, 28f);

            var layout = dockGo.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 18, 18);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            BuildProgress(dock);
        }

        void BuildHoldButton()
        {
            var existing = transform.Find("Hold");
            Image buttonImage;
            if (existing != null)
                buttonImage = existing.GetComponent<Image>();
            else
                buttonImage = UiDraw.Panel(transform, "Hold", ButtonTint, UiDraw.Circle);

            buttonImage.raycastTarget = false;
            var rect = buttonImage.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(120f, 120f);
            rect.anchoredPosition = Vector2.zero;

            _pauseGlyph = buttonImage.transform.Find("Pause") as RectTransform;
            if (_pauseGlyph == null)
            {
                _pauseGlyph = GlyphRoot(buttonImage.transform, "Pause");
                PauseBar(_pauseGlyph);
                PauseBar(_pauseGlyph);
            }

            _playGlyph = buttonImage.transform.Find("Play") as RectTransform;
            if (_playGlyph == null)
            {
                _playGlyph = GlyphRoot(buttonImage.transform, "Play");
                var play = UiDraw.Panel(_playGlyph, "Triangle", Color.white, UiDraw.Rounded);
                play.raycastTarget = false;
                var playRect = play.rectTransform;
                playRect.anchorMin = playRect.anchorMax = new Vector2(0.5f, 0.5f);
                playRect.pivot = new Vector2(0.5f, 0.5f);
                playRect.sizeDelta = new Vector2(28f, 28f);
                playRect.localRotation = Quaternion.Euler(0f, 0f, -90f);
                playRect.anchoredPosition = new Vector2(4f, 0f);
            }
        }

        void BuildProgress(Transform parent)
        {
            var columnGo = new GameObject("Progress", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            columnGo.transform.SetParent(parent, false);
            var columnFit = columnGo.GetComponent<LayoutElement>();
            columnFit.flexibleWidth = 1f;
            columnFit.minHeight = 72f;
            var column = columnGo.GetComponent<VerticalLayoutGroup>();
            column.spacing = 8f;
            column.childAlignment = TextAnchor.MiddleLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            _clock = UiDraw.Label(columnGo.transform, "Clock", "0:00 / 0:00", 28f, UiWeight.Bold, MemoryPathPalette.HudMuted);
            _clock.alignment = TextAlignmentOptions.MidlineLeft;
            _clock.textWrappingMode = TextWrappingModes.NoWrap;
            _clock.raycastTarget = false;

            var track = UiDraw.Panel(columnGo.transform, "Track", TrackTint, UiDraw.Rounded);
            track.raycastTarget = false;
            UiDraw.SetCornerRadius(track, 8f);
            var trackFit = track.gameObject.AddComponent<LayoutElement>();
            trackFit.minHeight = trackFit.preferredHeight = 14f;
            trackFit.flexibleWidth = 1f;

            _fill = UiDraw.Panel(track.transform, "Fill", FillTint, UiDraw.Rounded);
            _fill.raycastTarget = false;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.fillAmount = 0f;
            UiDraw.SetCornerRadius(_fill, 8f);
            UiDraw.Stretch(_fill.rectTransform);
        }

        static RectTransform GlyphRoot(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(22f, 22f);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        static void PauseBar(Transform parent)
        {
            var bar = UiDraw.Panel(parent, "Mark", Color.white, UiDraw.Rounded);
            bar.raycastTarget = false;
            UiDraw.SetCornerRadius(bar, 2f);
            var fit = bar.gameObject.AddComponent<LayoutElement>();
            fit.minWidth = fit.preferredWidth = 6f;
            fit.minHeight = fit.preferredHeight = 22f;
        }
    }
}
