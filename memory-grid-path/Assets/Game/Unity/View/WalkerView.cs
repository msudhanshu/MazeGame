using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// The little figure that hops from tile to tile. Built from primitives so the game has
    /// no art dependencies yet.
    /// </summary>
    public sealed class WalkerView : MonoBehaviour
    {
        public const float CorrectTravelSeconds = 0.22f;
        public const float MistakeTravelSeconds = 0.42f;
        const float TravelHeight = 0.12f;

        Vector3 _from;
        Vector3 _to;
        Vector3[] _path;
        float _hopStartedAt = -1f;
        float _travelSeconds = CorrectTravelSeconds;
        Transform _body;

        public static WalkerView Create(Transform parent, Vector3 position, Color tint, float scale = 1f)
        {
            var root = new GameObject("Walker");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

            var walker = root.AddComponent<WalkerView>();
            walker.BuildBody(tint);
            walker._to = position;
            return walker;
        }

        public bool IsHopping => _hopStartedAt >= 0f;

        public void SnapTo(Vector3 position)
        {
            _hopStartedAt = -1f;
            _path = null;
            _to = position;
            transform.position = position;
        }

        public static Vector3 PointOnTravel(Vector3 from, Vector3 to, float normalizedTime, float hopHeight = TravelHeight)
        {
            var t = Mathf.Clamp01(normalizedTime);
            if (t <= 0f)
                return from;
            if (t >= 1f)
                return to;

            var ground = Vector3.Lerp(from, to, t);
            return ground + new Vector3(0f, Mathf.Sin(t * Mathf.PI) * hopHeight, 0f);
        }

        public static Vector3 PointOnTravel(IReadOnlyList<Vector3> path, float normalizedTime, float hopHeight = TravelHeight)
        {
            if (path == null || path.Count == 0)
                return Vector3.zero;
            if (path.Count == 1)
                return path[0];

            var t = Mathf.Clamp01(normalizedTime);
            if (t <= 0f)
                return path[0];
            if (t >= 1f)
                return path[path.Count - 1];
            if (path.Count == 2)
                return PointOnTravel(path[0], path[1], t, hopHeight);

            var ground = PointAlongPolyline(path, t);
            return ground + new Vector3(0f, Mathf.Sin(t * Mathf.PI) * hopHeight, 0f);
        }

        public void HopTo(Vector3 position) => HopTo(position, CorrectTravelSeconds);

        public void HopTo(Vector3 position, float seconds)
        {
            _from = transform.position;
            _to = position;
            _path = null;
            _travelSeconds = Mathf.Max(0.05f, seconds);
            _hopStartedAt = Time.time;
        }

        public void HopAlong(IReadOnlyList<Vector3> path) => HopAlong(path, CorrectTravelSeconds);

        public void HopAlong(IReadOnlyList<Vector3> path, float seconds)
        {
            if (path == null || path.Count < 2)
            {
                HopTo(path != null && path.Count == 1 ? path[0] : transform.position, seconds);
                return;
            }

            _path = new Vector3[path.Count];
            for (var i = 0; i < path.Count; i++)
                _path[i] = path[i];
            _from = _path[0];
            _to = _path[_path.Length - 1];
            _travelSeconds = Mathf.Max(0.05f, seconds);
            _hopStartedAt = Time.time;
        }

        void BuildBody(Color tint)
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            _body.localPosition = new Vector3(0f, 0.05f, 0f);

            AddPart(PrimitiveType.Capsule, new Vector3(0f, 0.34f, 0f), new Vector3(0.3f, 0.26f, 0.3f), tint);
            AddPart(PrimitiveType.Sphere, new Vector3(0f, 0.66f, 0f), Vector3.one * 0.3f, Color.Lerp(tint, Color.white, 0.45f));
            AddPart(PrimitiveType.Sphere, new Vector3(-0.09f, 0.70f, 0.13f), Vector3.one * 0.08f, Color.black);
            AddPart(PrimitiveType.Sphere, new Vector3(0.09f, 0.70f, 0.13f), Vector3.one * 0.08f, Color.black);
        }

        void AddPart(PrimitiveType type, Vector3 localPosition, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(_body, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }
            part.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.Unlit("WalkerPart", color);
        }

        void Update()
        {
            if (_hopStartedAt < 0f)
                return;

            var t = Mathf.Clamp01((Time.time - _hopStartedAt) / _travelSeconds);
            var hopHeight = TravelHeight * transform.localScale.y;
            transform.position = _path != null
                ? PointOnTravel(_path, t, hopHeight)
                : PointOnTravel(_from, _to, t, hopHeight);

            if (t >= 1f)
            {
                transform.position = _to;
                _path = null;
                _hopStartedAt = -1f;
            }
        }

        static Vector3 PointAlongPolyline(IReadOnlyList<Vector3> path, float normalizedTime)
        {
            var t = Mathf.Clamp01(normalizedTime);
            var total = 0f;
            for (var i = 1; i < path.Count; i++)
                total += Vector3.Distance(path[i - 1], path[i]);

            if (total < 0.0001f)
                return path[path.Count - 1];

            var remaining = t * total;
            for (var i = 1; i < path.Count; i++)
            {
                var segment = Vector3.Distance(path[i - 1], path[i]);
                if (remaining <= segment || i == path.Count - 1)
                    return Vector3.Lerp(path[i - 1], path[i], segment < 0.0001f ? 1f : remaining / segment);

                remaining -= segment;
            }

            return path[path.Count - 1];
        }
    }
}
