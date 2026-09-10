using Nixin.Grid.Core;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    public enum FloorChipKind
    {
        Path,
        Reachable,
        Destination,
        Occupied
    }

    /// <summary>
    /// <see cref="WaypointStyle.FloorTile"/>: translucent chip on the floor.
    /// <see cref="WaypointStyle.SpaceBlob"/>: standing pin (disc + pole + head), yaw-only look.
    /// </summary>
    public sealed class FloorWaypointMarker : MonoBehaviour
    {
        public const float FloorLift = 0.03f;

        public GridCoord Cell;
        public WaypointStyle Style { get; private set; }

        Renderer[] _renderers;
        Material[] _mats;

        public static FloorWaypointMarker Create(Transform parent, WaypointStyle style)
        {
            var root = new GameObject("Waypoint");
            root.transform.SetParent(parent, false);
            var marker = root.AddComponent<FloorWaypointMarker>();
            marker.Build(style);
            marker.SetKind(FloorChipKind.Path);
            return marker;
        }

        public Vector3 PickPoint
        {
            get
            {
                var head = transform.Find("Head");
                if (head != null)
                    return head.position;
                var disc = transform.Find("Disc");
                if (disc != null)
                    return disc.position;
                return transform.position + Vector3.up * FloorLift;
            }
        }

        public void SetKind(FloorChipKind kind)
        {
            CacheRenderers();
            Color color;
            var show = true;
            var discScale = 0.55f;
            switch (kind)
            {
                case FloorChipKind.Reachable:
                    color = new Color(0.25f, 0.95f, 1f, 0.72f);
                    discScale = 0.72f;
                    break;
                case FloorChipKind.Destination:
                    color = new Color(1f, 0.85f, 0.2f, 0.8f);
                    discScale = 0.72f;
                    break;
                case FloorChipKind.Occupied:
                    color = new Color(0.2f, 0.7f, 0.85f, 0.12f);
                    discScale = 0.4f;
                    show = false;
                    break;
                default:
                    color = new Color(0.2f, 0.85f, 1f, 0.38f);
                    discScale = 0.55f;
                    break;
            }

            if (Style == WaypointStyle.FloorTile)
            {
                var disc = transform.Find("Disc");
                if (disc != null)
                    disc.localScale = new Vector3(discScale, 0.02f, discScale);
            }

            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = show;
                ArenaMarkMaterial.Tint(_mats[i], color);
            }
        }

        void Build(WaypointStyle style)
        {
            Style = style;
            if (style == WaypointStyle.SpaceBlob)
            {
                AddPart(PrimitiveType.Cylinder, "Disc", new Vector3(0f, 0.06f, 0f), new Vector3(1.05f, 0.04f, 1.05f));
                AddPart(PrimitiveType.Cylinder, "Pole", new Vector3(0f, 0.52f, 0f), new Vector3(0.16f, 0.46f, 0.16f));
                AddPart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.12f, 0f), new Vector3(0.42f, 0.42f, 0.42f));
                return;
            }

            AddPart(PrimitiveType.Cylinder, "Disc", new Vector3(0f, FloorLift, 0f), new Vector3(0.62f, 0.02f, 0.62f));
        }

        void AddPart(PrimitiveType type, string name, Vector3 localPos, Vector3 localScale)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Destroy(col);
                else
                    DestroyImmediate(col);
            }

            var renderer = go.GetComponent<Renderer>();
            if (renderer == null)
                return;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var src = ArenaMarkMaterial.CreateUnlit();
            if (src != null)
                renderer.material = src;
        }

        void CacheRenderers()
        {
            if (_renderers != null)
                return;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _mats = new Material[_renderers.Length];
            for (var i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;
                _mats[i] = _renderers[i].material;
            }
        }
    }
}
