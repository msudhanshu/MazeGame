using System;
using System.Collections;
using Game.Core.Rules;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Figma game-hud-v2: compact neon bar with pause, level, vertical lives, score, steps.
    /// </summary>
    public sealed class GridPathHud : MonoBehaviour
    {
        public const float FigmaWidth = 420f;
        public const float BarWidth = 940f;
        public const float PlayfieldGapFig = 16f;

        [SerializeField] Canvas _canvas;
        [SerializeField] TextMeshProUGUI _levelValue;
        [SerializeField] TextMeshProUGUI _scoreValue;
        [SerializeField] TextMeshProUGUI _stepsValue;
        [SerializeField] TextMeshProUGUI _hint;
        [SerializeField] TextMeshProUGUI _hintSub;
        [SerializeField] Button _exit;
        [SerializeField] Transform _healthWell;
        [SerializeField] Transform _barsRoot;
        int _builtRuns = -1;
        int _builtLives = -1;
        Image[][] _segments;
        int _paintedRun = int.MinValue;
        int _paintedLives = int.MinValue;
        Coroutine _healthPulse;
        Coroutine _hintFlash;
        Color _hintColor;
        Color _hintSubColor;

        public static float S(float fig) => fig * (BarWidth / FigmaWidth);

        static int Si(float fig) => Mathf.RoundToInt(S(fig));

        public static GridPathHud Create(Transform parent)
        {
            var go = new GameObject("HUD");
            if (parent != null)
                go.transform.SetParent(parent, false);
            var hud = go.AddComponent<GridPathHud>();
            hud.Build();
            return hud;
        }

        public static GridPathHud Resolve(GridPathHud hint, Transform parent)
        {
            var hud = hint != null ? hint : MemoryPathUi.FindScene<GridPathHud>();
            if (hud == null)
            {
                var prefab = Resources.Load<GridPathHud>(MemoryPathUi.HudResourcePath);
                if (prefab != null)
                    hud = UnityEngine.Object.Instantiate(prefab, parent);
            }

            if (hud == null)
                return Create(parent);

            hud.EnsureBuilt();
            return hud;
        }

        public void EnsureBuilt()
        {
            if (_canvas == null)
                Build();
            EnsureSafeArea();
        }

        public bool IsShown => _canvas != null && _canvas.gameObject.activeSelf;

        public RectTransform OverlayRoot
        {
            get
            {
                EnsureBuilt();
                return _canvas != null ? _canvas.transform as RectTransform : null;
            }
        }

        /// <summary>
        /// Fraction of camera height occupied by the top HUD bar plus a small gap,
        /// so the board can sit entirely below the chrome.
        /// </summary>
        public float TopChromeViewportHeight(Camera camera)
        {
            EnsureBuilt();
            if (_canvas == null || !_canvas.gameObject.activeSelf)
                return 0f;

            var bar = (_canvas.transform.Find("SafeArea/Bar") ?? _canvas.transform.Find("Bar")) as RectTransform;
            if (bar == null)
                return 0f;

            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            bar.GetWorldCorners(corners);
            var barBottom = corners[0].y;
            for (var i = 1; i < 4; i++)
                barBottom = Mathf.Min(barBottom, corners[i].y);

            float top;
            float height;
            if (camera != null)
            {
                var rect = camera.pixelRect;
                top = rect.yMax;
                height = rect.height;
            }
            else
            {
                top = Screen.height;
                height = Screen.height;
            }

            var gap = _canvas.scaleFactor * S(PlayfieldGapFig);
            return OccupiedTopViewport(top, height, barBottom, gap);
        }

        public static float OccupiedTopViewport(float viewTop, float viewHeight, float barBottomY, float gapPixels)
        {
            if (viewHeight < 1f)
                return 0f;
            return Mathf.Clamp01((viewTop - barBottomY + Mathf.Max(0f, gapPixels)) / viewHeight);
        }

        public RectTransform HealthWell
        {
            get
            {
                EnsureBuilt();
                return _healthWell as RectTransform;
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureBuilt();
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(visible);
                Nixin.Ui.SystemChrome.SetLightIcons(visible);
                if (visible)
                {
                    var safe = _canvas.transform.Find("SafeArea");
                    var fitter = safe != null ? safe.GetComponent<Nixin.Ui.SafeAreaFitter>() : null;
                    fitter?.Apply(force: true);
                }
            }
            if (!visible)
            {
                StopHintFlash();
                if (_healthPulse != null)
                {
                    StopCoroutine(_healthPulse);
                    _healthPulse = null;
                }

                if (_healthWell != null)
                    _healthWell.localScale = Vector3.one;
            }
        }

        public void SetStats(int level, int score, int step, int totalSteps) =>
            SetStats(level.ToString(), score, step, totalSteps);

        public void SetStats(string levelLabel, int score, int step, int totalSteps)
        {
            EnsureBuilt();
            _levelValue.text = levelLabel ?? "";
            _scoreValue.text = score.ToString();
            _stepsValue.text = step + "/" + totalSteps;
        }

        public void SetHealth(int currentRun, int livesLeft, int livesPerRun, int runsPerSession)
        {
            if (livesPerRun < 1 || runsPerSession < 1)
            {
                if (_healthWell != null)
                    _healthWell.gameObject.SetActive(false);
                return;
            }

            _healthWell.gameObject.SetActive(true);
            EnsureBars(runsPerSession, livesPerRun);

            var run = Mathf.Clamp(currentRun, 1, runsPerSession);
            var lives = Mathf.Clamp(livesLeft, 0, livesPerRun);
            var lostLife = ShouldPulseHealth(_paintedRun, _paintedLives, run, lives);
            for (var bar = 0; bar < runsPerSession; bar++)
            {
                for (var segment = 0; segment < livesPerRun; segment++)
                {
                    var remaining = SessionHealthBars.Segment(bar, segment, run, lives, livesPerRun, runsPerSession)
                        == HealthSegmentKind.Remaining;
                    var image = _segments[bar][segment];
                    image.color = remaining ? MemoryPathPalette.HealthFill : MemoryPathPalette.HealthEmpty;
                    var glow = image.GetComponent<Outline>();
                    if (glow != null)
                        glow.enabled = remaining;
                }
            }

            _paintedRun = run;
            _paintedLives = lives;
            if (lostLife)
                PulseHealth();
        }

        public static bool ShouldPulseHealth(int previousRun, int previousLives, int run, int lives) =>
            previousRun == run && lives < previousLives;

        public void SetMessage(string title, string subtitle = null)
        {
            EnsureBuilt();
            StopHintFlash();
            _hint.text = title ?? "";
            _hintSub.text = string.IsNullOrEmpty(subtitle)
                ? "Tap a neighbouring tile, or swipe toward it."
                : subtitle;
            _hint.color = _hintColor;
            _hintSub.color = _hintSubColor;
            _hint.rectTransform.localScale = Vector3.one;
        }

        public void FlashHint()
        {
            EnsureBuilt();
            StopHintFlash();
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;
            _hintFlash = StartCoroutine(FlashHintRoutine());
        }

        public void PulseHealth()
        {
            EnsureBuilt();
            if (_healthWell == null)
                return;
            if (_healthPulse != null)
                StopCoroutine(_healthPulse);
            if (!isActiveAndEnabled || !Application.isPlaying)
                return;
            _healthPulse = StartCoroutine(PulseHealthRoutine());
        }

        void StopHintFlash()
        {
            if (_hintFlash == null)
                return;
            StopCoroutine(_hintFlash);
            _hintFlash = null;
            if (_hint != null)
            {
                _hint.color = _hintColor;
                _hint.rectTransform.localScale = Vector3.one;
            }

            if (_hintSub != null)
                _hintSub.color = _hintSubColor;
        }

        public void BindExit(Action onExit)
        {
            EnsureBuilt();
            _exit.onClick.RemoveAllListeners();
            if (onExit != null)
                _exit.onClick.AddListener(() => onExit());
        }

        void EnsureBars(int runs, int lives)
        {
            if (_builtRuns == runs && _builtLives == lives && _segments != null)
                return;

            for (var i = _barsRoot.childCount - 1; i >= 0; i--)
            {
                var child = _barsRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }

            _builtRuns = runs;
            _builtLives = lives;
            _segments = new Image[runs][];
            var colW = S(14);
            var colH = S(41);
            var divider = S(1);
            var segH = lives <= 1 ? colH : (colH - divider * (lives - 1)) / lives;

            var wellFit = _healthWell.GetComponent<LayoutElement>();
            var wellW = S(16) + runs * colW + Mathf.Max(0, runs - 1) * S(6);
            wellFit.minWidth = wellFit.preferredWidth = wellW;

            for (var bar = 0; bar < runs; bar++)
            {
                var tray = UiDraw.Panel(_barsRoot, "Bar" + bar, MemoryPathPalette.HealthEmpty);
                UiDraw.SetCornerRadius(tray, S(3));
                tray.raycastTarget = false;
                tray.gameObject.AddComponent<RectMask2D>();
                var trayLayout = tray.gameObject.AddComponent<VerticalLayoutGroup>();
                trayLayout.padding = new RectOffset(0, 0, 0, 0);
                trayLayout.spacing = 0;
                trayLayout.childAlignment = TextAnchor.UpperCenter;
                trayLayout.childControlWidth = true;
                trayLayout.childControlHeight = true;
                trayLayout.childForceExpandWidth = true;
                trayLayout.childForceExpandHeight = false;
                Fit(tray, colW, colH);

                _segments[bar] = new Image[lives];
                for (var fromTop = lives - 1; fromTop >= 0; fromTop--)
                {
                    var chip = UiDraw.Panel(tray.transform, "Seg" + fromTop, MemoryPathPalette.HealthEmpty);
                    chip.raycastTarget = false;
                    UiDraw.SetCornerRadius(chip, 0f);
                    var glow = chip.gameObject.AddComponent<Outline>();
                    glow.effectColor = MemoryPathPalette.HealthGlow;
                    glow.effectDistance = new Vector2(0f, 0f);
                    glow.enabled = false;
                    Fit(chip, colW, segH);
                    _segments[bar][fromTop] = chip;

                    if (fromTop > 0)
                    {
                        var line = UiDraw.Panel(tray.transform, "Div", MemoryPathPalette.HealthDivider);
                        line.raycastTarget = false;
                        Fit(line, colW, divider);
                    }
                }
            }
        }

        void Build()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            UiDraw.ConfigureCanvas(_canvas, canvasGo.GetComponent<CanvasScaler>());

            PlaceGlow(canvasGo.transform, "GlowCyan", "MemoryPath/hud-glow-cyan", new Vector2(S(-120), -S(48)));
            PlaceGlow(canvasGo.transform, "GlowMagenta", "MemoryPath/hud-glow-magenta", new Vector2(S(40), -S(42)));

            var safe = MakeSafeArea(canvasGo.transform);

            var bar = Chrome(safe, "Bar", MemoryPathPalette.HudPanel, MemoryPathPalette.HudBorder, S(22), 1.5f);
            var barRect = bar.rectTransform;
            barRect.anchorMin = new Vector2(0.5f, 1f);
            barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -S(16));
            barRect.sizeDelta = new Vector2(BarWidth, S(84));
            UiDraw.DropShadow(bar, new Color(0f, 0f, 0f, 0.7f), new Vector2(0f, -S(12)));

            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            row.SetParent(bar.transform, false);
            UiDraw.Stretch(row.GetComponent<RectTransform>());
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(Si(16), Si(16), Si(10), Si(10));
            rowLayout.spacing = 0;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            _exit = MakePause(row);
            Flex(row);
            _levelValue = MakeStat(row, "Level", "LEVEL", "MemoryPath/hud-shield", MemoryPathPalette.HudLevel);
            Flex(row);
            BuildHealth(row);
            Flex(row);
            _scoreValue = MakeStat(row, "Score", "SCORE", "MemoryPath/hud-star", MemoryPathPalette.HudScore);
            Flex(row);
            _stepsValue = MakeSteps(row);

            var hintTop = -(S(16) + S(84) + S(18));
            _hint = UiDraw.Label(safe, "Hint", "", S(16), UiWeight.ExtraBold, MemoryPathPalette.HudText);
            var hintRect = _hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, hintTop);
            hintRect.sizeDelta = new Vector2(BarWidth, S(22));

            _hintSub = UiDraw.Label(safe, "HintSub", "", S(12), UiWeight.Regular, MemoryPathPalette.HudMuted);
            var subRect = _hintSub.rectTransform;
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, hintTop - S(22));
            subRect.sizeDelta = new Vector2(BarWidth, S(18));
            _hintColor = _hint.color;
            _hintSubColor = _hintSub.color;
        }

        void BuildHealth(Transform parent)
        {
            var well = Chrome(parent, "Health", MemoryPathPalette.HudHealthWell, MemoryPathPalette.HudBorder, S(14), 1f, raycast: false);
            _healthWell = well.transform;
            Fit(well, S(110), S(44));

            var bars = new GameObject("Bars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bars.transform.SetParent(well.transform, false);
            UiDraw.Stretch(bars.GetComponent<RectTransform>());
            var inset = S(8);
            bars.GetComponent<RectTransform>().offsetMin = new Vector2(inset, S(1.5f));
            bars.GetComponent<RectTransform>().offsetMax = new Vector2(-inset, -S(1.5f));
            _barsRoot = bars.transform;
            var layout = bars.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = S(6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        static Transform MakeSafeArea(Transform canvas)
        {
            var existing = canvas.Find("SafeArea");
            if (existing != null)
            {
                if (existing.GetComponent<Nixin.Ui.SafeAreaFitter>() == null)
                    existing.gameObject.AddComponent<Nixin.Ui.SafeAreaFitter>();
                return existing;
            }

            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(Nixin.Ui.SafeAreaFitter));
            go.transform.SetParent(canvas, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go.transform;
        }

        void EnsureSafeArea()
        {
            if (_canvas == null)
                return;

            var scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                UiDraw.ConfigureCanvas(_canvas, scaler);

            var safe = MakeSafeArea(_canvas.transform);
            var safeFitter = safe.GetComponent<Nixin.Ui.SafeAreaFitter>();
            if (safeFitter != null)
                safeFitter.Apply(force: true);

            var bar = _canvas.transform.Find("Bar") ?? safe.Find("Bar");
            if (bar != null)
            {
                ReparentInto(bar, safe);
                var barRect = bar as RectTransform;
                if (barRect != null)
                {
                    barRect.anchorMin = new Vector2(0.5f, 1f);
                    barRect.anchorMax = new Vector2(0.5f, 1f);
                    barRect.pivot = new Vector2(0.5f, 1f);
                    barRect.anchoredPosition = new Vector2(0f, -S(16));
                    barRect.sizeDelta = new Vector2(BarWidth, S(84));
                }
            }

            var hintTop = -(S(16) + S(84) + S(18));
            var hint = _canvas.transform.Find("Hint") ?? safe.Find("Hint");
            if (hint != null)
            {
                ReparentInto(hint, safe);
                var hintRect = hint as RectTransform;
                if (hintRect != null)
                {
                    hintRect.anchorMin = new Vector2(0.5f, 1f);
                    hintRect.anchorMax = new Vector2(0.5f, 1f);
                    hintRect.pivot = new Vector2(0.5f, 1f);
                    hintRect.anchoredPosition = new Vector2(0f, hintTop);
                    hintRect.sizeDelta = new Vector2(BarWidth, S(22));
                }
            }

            var hintSub = _canvas.transform.Find("HintSub") ?? safe.Find("HintSub");
            if (hintSub != null)
            {
                ReparentInto(hintSub, safe);
                var subRect = hintSub as RectTransform;
                if (subRect != null)
                {
                    subRect.anchorMin = new Vector2(0.5f, 1f);
                    subRect.anchorMax = new Vector2(0.5f, 1f);
                    subRect.pivot = new Vector2(0.5f, 1f);
                    subRect.anchoredPosition = new Vector2(0f, hintTop - S(22));
                    subRect.sizeDelta = new Vector2(BarWidth, S(18));
                }
            }
        }

        static void ReparentInto(Transform child, Transform parent)
        {
            if (child == null || parent == null || child.parent == parent)
                return;
            child.SetParent(parent, false);
        }

        static Button MakePause(Transform parent)
        {
            var image = Chrome(parent, "Pause", MemoryPathPalette.HudPause, MemoryPathPalette.SettingsButton, S(12), 1.5f, raycast: true);
            Fit(image, S(44), S(44));
            UiDraw.DropShadow(image, MemoryPathPalette.HudPauseGlow, new Vector2(0f, 0f));

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(MemoryPathPalette.HudPause, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(MemoryPathPalette.HudPause, Color.black, 0.18f);
            button.colors = colors;

            var marks = new GameObject("Bars", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            marks.SetParent(image.transform, false);
            var marksRect = marks.GetComponent<RectTransform>();
            marksRect.anchorMin = marksRect.anchorMax = new Vector2(0.5f, 0.5f);
            marksRect.pivot = new Vector2(0.5f, 0.5f);
            marksRect.sizeDelta = new Vector2(S(12), S(14));
            var marksLayout = marks.GetComponent<HorizontalLayoutGroup>();
            marksLayout.spacing = S(4);
            marksLayout.childAlignment = TextAnchor.MiddleCenter;
            marksLayout.childControlWidth = true;
            marksLayout.childControlHeight = true;
            marksLayout.childForceExpandWidth = false;
            marksLayout.childForceExpandHeight = false;
            marks.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            PauseBar(marks);
            PauseBar(marks);
            return button;
        }

        static void PauseBar(Transform parent)
        {
            var bar = UiDraw.Panel(parent, "Mark", MemoryPathPalette.HudText, UiDraw.Rounded);
            UiDraw.SetCornerRadius(bar, S(2));
            bar.raycastTarget = false;
            Fit(bar, S(4), S(14));
        }

        static TextMeshProUGUI MakeStat(Transform parent, string name, string caption, string icon, Color valueColor)
        {
            var cell = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup)).transform;
            cell.SetParent(parent, false);
            var layout = cell.GetComponent<VerticalLayoutGroup>();
            layout.spacing = S(2);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            Fit(cell, 0f, S(44));

            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup)).transform;
            header.SetParent(cell, false);
            var headerLayout = header.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = S(4);
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            HudIcon(header, "Icon", icon, S(12));
            var label = UiDraw.Label(header, "Cap", caption, S(10), UiWeight.Bold, MemoryPathPalette.HudMuted);
            label.characterSpacing = 100f / 10f;

            return UiDraw.Label(cell, "Val", "0", S(18), UiWeight.ExtraBold, valueColor);
        }

        static TextMeshProUGUI MakeSteps(Transform parent)
        {
            var chip = Chrome(parent, "Steps", MemoryPathPalette.HudSteps, MemoryPathPalette.HudStepsBorder, S(12), 1f, raycast: false);
            var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Si(12), Si(12), Si(10), Si(10));
            layout.spacing = S(8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            Fit(chip, 0f, S(44));
            var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            HudIcon(chip.transform, "Icon", "MemoryPath/hud-footprints", S(16));
            return UiDraw.Label(chip.transform, "Val", "0/0", S(14), UiWeight.Bold, MemoryPathPalette.HudText);
        }

        static void PlaceGlow(Transform parent, string name, string resource, Vector2 anchored)
        {
            var sprite = UiDraw.ResourceSprite(resource);
            var image = UiDraw.Icon(parent, name, sprite, S(240));
            image.color = Color.white;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(S(240), S(240));
            if (sprite == null)
                image.enabled = false;
        }

        static Image HudIcon(Transform parent, string name, string resource, float size)
        {
            var sprite = UiDraw.ResourceSprite(resource);
            var icon = UiDraw.Icon(parent, name, sprite, size);
            Fit(icon, size, size);
            if (sprite == null)
                icon.enabled = false;
            return icon;
        }

        static Image Chrome(Transform parent, string name, Color fill, Color border, float radius, float borderPx, bool raycast = false)
        {
            var outer = UiDraw.Panel(parent, name, border);
            outer.raycastTarget = raycast;
            UiDraw.SetCornerRadius(outer, radius);
            var inner = UiDraw.Panel(outer.transform, "Fill", fill);
            inner.raycastTarget = false;
            inner.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiDraw.Stretch(inner.rectTransform);
            var inset = S(borderPx);
            inner.rectTransform.offsetMin = new Vector2(inset, inset);
            inner.rectTransform.offsetMax = new Vector2(-inset, -inset);
            UiDraw.SetCornerRadius(inner, Mathf.Max(1f, radius - inset));
            return outer;
        }

        static void Flex(Transform parent)
        {
            var go = new GameObject("Flex", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().flexibleWidth = 1;
        }

        static LayoutElement Fit(Component target, float width, float height)
        {
            var fit = target.gameObject.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                fit.minWidth = fit.preferredWidth = width;
                fit.flexibleWidth = 0;
            }

            if (height > 0f)
            {
                fit.minHeight = fit.preferredHeight = height;
                fit.flexibleHeight = 0;
            }

            return fit;
        }

        static void EnsureEventSystem()
        {
            MemoryPathUi.EnsureEventSystem();
        }

        IEnumerator PulseHealthRoutine()
        {
            var well = _healthWell as RectTransform;
            var chrome = _healthWell.GetComponent<Image>();
            var rest = chrome != null ? chrome.color : Color.white;
            var punch = MemoryPathPalette.KickerFail;
            var elapsed = 0f;
            const float duration = 0.7f;
            while (elapsed < duration && well != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var wave = Mathf.PingPong(elapsed * 8f, 1f);
                well.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.16f, wave);
                if (chrome != null)
                    chrome.color = Color.Lerp(rest, punch, wave);
                yield return null;
            }

            if (well != null)
                well.localScale = Vector3.one;
            if (chrome != null)
                chrome.color = rest;
            _healthPulse = null;
        }

        IEnumerator FlashHintRoutine()
        {
            var elapsed = 0f;
            const float duration = 2.4f;
            while (elapsed < duration && _hint != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var wave = (Mathf.Sin(elapsed * 10f) + 1f) * 0.5f;
                _hint.color = Color.Lerp(_hintColor, Color.white, wave);
                _hint.rectTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.08f, wave);
                if (_hintSub != null)
                    _hintSub.color = Color.Lerp(_hintSubColor, Color.white, wave);
                yield return null;
            }

            if (_hint != null)
            {
                _hint.color = _hintColor;
                _hint.rectTransform.localScale = Vector3.one;
            }

            if (_hintSub != null)
                _hintSub.color = _hintSubColor;
            _hintFlash = null;
        }
    }
}
