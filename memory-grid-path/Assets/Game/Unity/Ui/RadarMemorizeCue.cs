using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Centered one-line prompt shown while the radar path scan is running.
    /// </summary>
    public sealed class RadarMemorizeCue : MonoBehaviour
    {
        public const string Caption = "Try to memorize the path.";
        public const float HeldAlpha = 1f;
        public const float FadeSeconds = 0.16f;

        static readonly Color PanelTint = new Color(0.04f, 0.06f, 0.10f, 0.9f);
        static readonly Color LabelTint = new Color(1f, 1f, 1f, 1f);

        CanvasGroup _group;
        TextMeshProUGUI _label;
        Coroutine _fade;

        public static RadarMemorizeCue Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            var existing = parent.GetComponentInChildren<RadarMemorizeCue>(true);
            if (existing != null)
                return existing;

            var go = new GameObject(
                "RadarMemorizeCue",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(RadarMemorizeCue));
            go.transform.SetParent(parent, false);
            var cue = go.GetComponent<RadarMemorizeCue>();
            cue.Build();
            cue.HideImmediate();
            return cue;
        }

        public static void HideOn(Transform parent)
        {
            if (parent == null)
                return;
            var existing = parent.GetComponentInChildren<RadarMemorizeCue>(true);
            existing?.HideImmediate();
        }

        public void Show()
        {
            EnsureBuilt();
            if (_label != null)
                _label.text = Caption;

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
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                HideImmediate();
                return;
            }

            FadeTo(0f, deactivate: true);
        }

        public void HideImmediate()
        {
            StopFade();
            if (_group != null)
                _group.alpha = 0f;
            gameObject.SetActive(false);
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
            if (_label == null || _group == null)
                Build();
        }

        void Build()
        {
            var root = transform as RectTransform;
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(720f, 88f);
            root.anchoredPosition = Vector2.zero;

            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null)
                _group = gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _group.alpha = 0f;

            var panel = ChildImage("Panel", PanelTint);
            Stretch(panel.rectTransform);
            UiDraw.SetCornerRadius(panel, 24f);
            var panelIgnore = panel.gameObject.GetComponent<LayoutElement>()
                ?? panel.gameObject.AddComponent<LayoutElement>();
            panelIgnore.ignoreLayout = true;

            var layout = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(44, 44, 22, 22);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _label = UiDraw.Label(transform, "Caption", Caption, 52f, UiWeight.ExtraBold, LabelTint);
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.characterSpacing = 0.4f;
        }

        Image ChildImage(string name, Color color)
        {
            var existing = transform.Find(name);
            var image = existing != null ? existing.GetComponent<Image>() : null;
            if (image == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                image = go.GetComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
