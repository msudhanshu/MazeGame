using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Unity.Input
{
    /// <summary>
    /// Blocks playfield gestures only when the pointer hits interactive UI (buttons,
    /// modal overlays). Decorative HUD chrome must not swallow board touches.
    /// </summary>
    public static class PlayfieldInputGate
    {
        static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>(8);

        public static bool BlocksPlayfield(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointerData = new PointerEventData(eventSystem) { position = screenPosition };
            RaycastHits.Clear();
            eventSystem.RaycastAll(pointerData, RaycastHits);
            for (var i = 0; i < RaycastHits.Count; i++)
            {
                var hit = RaycastHits[i].gameObject;
                if (hit == null)
                    continue;

                if (hit.GetComponentInParent<Selectable>() != null)
                    return true;

                var group = hit.GetComponentInParent<CanvasGroup>();
                if (group != null && group.blocksRaycasts && group.interactable)
                    return true;
            }

            return false;
        }
    }
}
