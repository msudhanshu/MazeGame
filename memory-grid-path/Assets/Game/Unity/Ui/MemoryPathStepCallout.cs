using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Compact callout card for step feedback. Hugs its text and places itself away from
    /// the walker so it never covers the current avatar.
    /// </summary>
    public sealed class MemoryPathStepCallout : MonoBehaviour
    {
        public const float ShowSeconds = 2.1f;
        public const float FadeSeconds = 0.18f;

        RectTransform _root;
        RectTransform _host;
        CanvasGroup _group;
        Image _panel;
        Image _tail;
        TextMeshProUGUI _label;
        ContentSizeFitter _fitter;
        Coroutine _hide;
        Camera _camera;

        public static MemoryPathStepCallout Ensure(Transform parent)
        {
            if (parent == null)
                return null;

            var existing = parent.GetComponentInChildren<MemoryPathStepCallout>(true);
            if (existing != null)
                return existing;

            var go = new GameObject("MemoryPathStepCallout", typeof(RectTransform), typeof(MemoryPathStepCallout));
            go.transform.SetParent(parent, false);
            var callout = go.GetComponent<MemoryPathStepCallout>();
            callout.Build();
            callout.HideImmediate();
            return callout;
        }

        public void ShowAwayFromAvatar(Camera camera, Vector3 avatarWorld, string caption, bool recover)
        {
            EnsureBuilt();
            if (_root == null || _host == null)
                return;

            _camera = camera;
            caption = caption ?? "";
            if (_label != null)
                _label.text = caption;

            if (_panel != null)
                _panel.color = recover
                    ? new Color(0.12f, 0.42f, 0.34f, 0.96f)
                    : new Color(0.55f, 0.12f, 0.16f, 0.96f);
            if (_tail != null)
                _tail.color = _panel != null ? _panel.color : Color.white;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            PlaceAwayFromAvatar(avatarWorld);
            gameObject.SetActive(true);

            if (_hide != null)
                StopCoroutine(_hide);
            _hide = StartCoroutine(ShowThenHide());
        }

        public void HideImmediate()
        {
            if (_hide != null)
            {
                StopCoroutine(_hide);
                _hide = null;
            }

            if (_group != null)
                _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        IEnumerator ShowThenHide()
        {
            if (_group != null)
                _group.alpha = 0f;

            var t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (_group != null)
                    _group.alpha = Mathf.Clamp01(t / FadeSeconds);
                if (_root != null)
                {
                    var s = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01(t / FadeSeconds));
                    _root.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            if (_group != null)
                _group.alpha = 1f;
            if (_root != null)
                _root.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(ShowSeconds);

            t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                if (_group != null)
                    _group.alpha = 1f - Mathf.Clamp01(t / FadeSeconds);
                yield return null;
            }

            HideImmediate();
        }

        void PlaceAwayFromAvatar(Vector3 avatarWorld)
        {
            var hostRect = _host.rect;
            var preferBottom = true;
            var preferLeft = false;

            if (_camera != null)
            {
                var screen = _camera.WorldToScreenPoint(avatarWorld);
                var viewport = new Vector2(screen.x / Screen.width, screen.y / Screen.height);
                preferBottom = viewport.y > 0.42f;
                preferLeft = viewport.x > 0.55f;
            }

            var marginX = Mathf.Max(24f, hostRect.width * 0.04f);
            var marginY = Mathf.Max(28f, hostRect.height * 0.05f);
            var halfW = _root.sizeDelta.x * 0.5f;
            var halfH = _root.sizeDelta.y * 0.5f;

            float x;
            float y;
            if (preferBottom)
            {
                y = -hostRect.height * 0.5f + marginY + halfH + 18f;
                x = preferLeft
                    ? -hostRect.width * 0.5f + marginX + halfW
                    : hostRect.width * 0.5f - marginX - halfW;
                // Keep it near bottom center when there is room, still offset from avatar.
                if (Mathf.Abs(x) > hostRect.width * 0.28f)
                    x *= 0.55f;
            }
            else
            {
                y = hostRect.height * 0.5f - marginY - halfH - 64f;
                x = preferLeft
                    ? -hostRect.width * 0.5f + marginX + halfW
                    : hostRect.width * 0.5f - marginX - halfW;
            }

            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, preferBottom ? 0f : 1f);
            _root.anchoredPosition = new Vector2(x, y);

            if (_tail != null)
            {
                var tail = _tail.rectTransform;
                tail.anchorMin = tail.anchorMax = preferBottom
                    ? new Vector2(0.5f, 0f)
                    : new Vector2(0.5f, 1f);
                tail.pivot = new Vector2(0.5f, 0.5f);
                tail.anchoredPosition = preferBottom ? new Vector2(0f, -2f) : new Vector2(0f, 2f);
                tail.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
        }

        void EnsureBuilt()
        {
            if (_label == null)
                Build();
        }

        void Build()
        {
            _host = transform.parent as RectTransform;
            _root = transform as RectTransform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0f);
            _root.sizeDelta = new Vector2(220f, 56f);

            _group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _panel = ChildImage("Panel", new Color(0.55f, 0.12f, 0.16f, 0.96f));
            Stretch(_panel.rectTransform);
            UiDraw.SetCornerRadius(_panel, 14f);
            var panelIgnore = _panel.gameObject.GetComponent<LayoutElement>()
                ?? _panel.gameObject.AddComponent<LayoutElement>();
            panelIgnore.ignoreLayout = true;

            var layout = gameObject.GetComponent<HorizontalLayoutGroup>()
                ?? gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 12);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _fitter = gameObject.GetComponent<ContentSizeFitter>()
                ?? gameObject.AddComponent<ContentSizeFitter>();
            _fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _label = UiDraw.Label(transform, "Caption", "", 28f, UiWeight.Bold, Color.white);
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.overflowMode = TextOverflowModes.Overflow;
            var labelFit = _label.gameObject.GetComponent<LayoutElement>()
                ?? _label.gameObject.AddComponent<LayoutElement>();
            labelFit.preferredWidth = 280f;

            _tail = ChildImage("Tail", _panel.color);
            var tailRect = _tail.rectTransform;
            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.pivot = new Vector2(0.5f, 0.5f);
            tailRect.sizeDelta = new Vector2(16f, 16f);
            tailRect.anchoredPosition = new Vector2(0f, -2f);
            tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var ignore = _tail.gameObject.GetComponent<LayoutElement>()
                ?? _tail.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
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
