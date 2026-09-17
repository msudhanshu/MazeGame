using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    /// <summary>Spark from a world tile into the current HUD heart.</summary>
    public static class HudHeartSpark
    {
        public static IEnumerator Fly(
            RectTransform overlay,
            Camera camera,
            Vector3 worldFrom,
            RectTransform heart,
            Action onHit)
        {
            if (overlay == null || camera == null || heart == null)
            {
                onHit?.Invoke();
                yield break;
            }

            var go = new GameObject("HeartSpark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlay, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            image.sprite = UiDraw.Heart;
            image.preserveAspect = true;
            image.useSpriteMesh = true;
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(GridPathHud.S(18), GridPathHud.S(18));

            var start = WorldToOverlay(overlay, camera, worldFrom);
            var end = OverlayLocal(overlay, heart);
            rect.anchoredPosition = start;

            var elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                rect.anchoredPosition = Vector2.Lerp(start, end, t);
                rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.7f, t);
                yield return null;
            }

            onHit?.Invoke();
            if (go != null)
                UnityEngine.Object.Destroy(go);
        }

        static Vector2 WorldToOverlay(RectTransform overlay, Camera camera, Vector3 world)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(camera, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out var local);
            return local;
        }

        static Vector2 OverlayLocal(RectTransform overlay, RectTransform heart)
        {
            var corners = new Vector3[4];
            heart.GetWorldCorners(corners);
            var mid = (corners[0] + corners[2]) * 0.5f;
            var screen = RectTransformUtility.WorldToScreenPoint(null, mid);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out var local);
            return local;
        }
    }
}
