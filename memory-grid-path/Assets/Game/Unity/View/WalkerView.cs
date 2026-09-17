using System.Collections;
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
        public const float MissApproachSeconds = 0.3f;
        public const int SpawnPulses = 3;
        public const float SpawnOnSeconds = 0.2f;
        public const float SpawnOffSeconds = 0.14f;
        const float TravelHeight = 0.12f;
        static readonly float[] DespawnPeaks = { 1f, 0.5f, 0.22f };
        static readonly float[] SpawnPeaks = { 0.22f, 0.5f, 1f };
        static float PulseOnRatio => SpawnOnSeconds / (SpawnOnSeconds + SpawnOffSeconds);

        public static float CrashOutSeconds => DespawnSeconds;
        public static float CrashInSeconds => SpawnSeconds;
        public static float DespawnSeconds => SpawnPulses * (SpawnOnSeconds + SpawnOffSeconds);
        public static float SpawnSeconds =>
            (SpawnPulses - 1) * (SpawnOnSeconds + SpawnOffSeconds) + SpawnOnSeconds;

        Vector3 _from;
        Vector3 _to;
        Vector3[] _path;
        float _hopStartedAt = -1f;
        float _pauseBeganAt = -1f;
        bool _motionPaused;
        float _travelSeconds = CorrectTravelSeconds;
        Transform _body;
        Renderer[] _parts;
        Color[] _partColors;
        float _alpha = 1f;
        float _targetYaw;
        bool _faceTravel;
        bool _awaitingFacing;
        float _yawDegreesPerSecond = ScoutRotationMove.PlayYawDegreesPerSecond;

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

        public bool IsHopping => _hopStartedAt >= 0f || _awaitingFacing;
        public bool IsTurning => _awaitingFacing;
        public bool MotionPaused
        {
            get => _motionPaused;
            set
            {
                if (_motionPaused == value)
                    return;
                if (value)
                {
                    _motionPaused = true;
                    _pauseBeganAt = Time.time;
                    return;
                }

                if (_hopStartedAt >= 0f && _pauseBeganAt >= 0f)
                    _hopStartedAt += Time.time - _pauseBeganAt;
                _pauseBeganAt = -1f;
                _motionPaused = false;
            }
        }

        public float TravelNormalized
        {
            get
            {
                if (_awaitingFacing)
                    return 0f;
                if (_hopStartedAt < 0f)
                    return 1f;
                var now = _motionPaused && _pauseBeganAt >= 0f ? _pauseBeganAt : Time.time;
                return Mathf.Clamp01((now - _hopStartedAt) / Mathf.Max(0.05f, _travelSeconds));
            }
        }

        public float YawDegrees { get; private set; }
        public float YawDegreesPerSecond
        {
            get => _yawDegreesPerSecond;
            set => _yawDegreesPerSecond = Mathf.Max(1f, value);
        }

        public bool FaceTravel
        {
            get => _faceTravel;
            set
            {
                _faceTravel = value;
                if (!_faceTravel)
                {
                    _awaitingFacing = false;
                    ApplyYaw(0f);
                }
            }
        }

        public void FaceToward(Vector3 delta, bool instant)
        {
            if (!ScoutRotationMove.TryYawFromDelta(delta, out var yaw))
                return;
            if (instant)
                ApplyYaw(yaw);
            else
                _targetYaw = yaw;
        }

        public void TickFacing(float deltaTime)
        {
            if (!_faceTravel)
                return;

            if (!_awaitingFacing && _hopStartedAt >= 0f)
            {
                var tangent = CurrentTravelTangent();
                if (ScoutRotationMove.TryYawFromDelta(tangent, out var travelYaw))
                    _targetYaw = travelYaw;
            }

            var maxDelta = _yawDegreesPerSecond * Mathf.Max(0.0001f, deltaTime);
            YawDegrees = Mathf.MoveTowardsAngle(YawDegrees, _targetYaw, maxDelta);
            transform.rotation = Quaternion.Euler(0f, YawDegrees, 0f);

            if (_awaitingFacing
                && Mathf.Abs(Mathf.DeltaAngle(YawDegrees, _targetYaw)) <= ScoutRotationMove.FacingAlignDegrees)
                StartHop();
        }

        public void SnapTo(Vector3 position) => SnapTo(position, restoreAlpha: true);

        public void SnapTo(Vector3 position, bool restoreAlpha)
        {
            _hopStartedAt = -1f;
            _pauseBeganAt = -1f;
            _motionPaused = false;
            _awaitingFacing = false;
            _path = null;
            _to = position;
            transform.position = position;
            if (_body != null)
                _body.localScale = Vector3.one;
            if (restoreAlpha)
                SetBodyAlpha(1f);
            if (!_faceTravel)
                ApplyYaw(0f);
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

        public void HopTo(Vector3 position) =>
            HopTo(position, SecondsForDistance(HorizontalDistance(transform.position, position), TravelSecondsPerUnit()));

        public void HopTo(Vector3 position, float seconds)
        {
            BeginTravel(transform.position, position, path: null, seconds);
        }

        public void HopAlong(IReadOnlyList<Vector3> path) =>
            HopAlong(path, SecondsForPath(path, TravelSecondsPerUnit()));

        public void HopAlong(IReadOnlyList<Vector3> path, float seconds)
        {
            if (path == null || path.Count < 2)
            {
                HopTo(path != null && path.Count == 1 ? path[0] : transform.position, seconds);
                return;
            }

            var copy = new Vector3[path.Count];
            for (var i = 0; i < path.Count; i++)
                copy[i] = path[i];
            BeginTravel(copy[0], copy[copy.Length - 1], copy, seconds);
        }

        public static float PolylineLength(IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count < 2)
                return 0f;

            var total = 0f;
            for (var i = 1; i < path.Count; i++)
                total += HorizontalDistance(path[i - 1], path[i]);
            return total;
        }

        public static float SecondsForPath(IReadOnlyList<Vector3> path, float secondsPerUnit) =>
            SecondsForDistance(PolylineLength(path), secondsPerUnit);

        public static float SecondsForDistance(Vector3 from, Vector3 to, float secondsPerUnit) =>
            SecondsForDistance(HorizontalDistance(from, to), secondsPerUnit);

        public static float SecondsForDistance(float distance, float secondsPerUnit) =>
            Mathf.Max(0.05f, Mathf.Max(0f, distance) * Mathf.Max(0.01f, secondsPerUnit));

        float TravelSecondsPerUnit() =>
            _faceTravel ? ScoutRotationMove.PlayTravelSeconds : CorrectTravelSeconds;

        void BeginTravel(Vector3 from, Vector3 to, Vector3[] path, float seconds)
        {
            _from = from;
            _to = to;
            _path = path;
            _travelSeconds = Mathf.Max(0.05f, seconds);
            var delta = path != null && path.Length >= 2 ? path[1] - path[0] : to - from;
            if (_faceTravel
                && ScoutRotationMove.TryYawFromDelta(delta, out var yaw)
                && Mathf.Abs(Mathf.DeltaAngle(YawDegrees, yaw)) > ScoutRotationMove.FacingAlignDegrees)
            {
                _targetYaw = yaw;
                _awaitingFacing = true;
                _hopStartedAt = -1f;
                return;
            }

            if (_faceTravel)
                FaceToward(delta, instant: false);
            StartHop();
        }

        void StartHop()
        {
            _awaitingFacing = false;
            _hopStartedAt = Time.time;
        }

        public IEnumerator Dematerialize(float seconds)
        {
            var duration = Mathf.Max(0.2f, seconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                TickDespawn(elapsed / duration);
                yield return null;
            }

            SetBodyAlpha(0f);
        }

        public IEnumerator Rematerialize(float seconds)
        {
            var duration = Mathf.Max(0.2f, seconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                TickSpawn(elapsed / duration);
                yield return null;
            }

            SetBodyAlpha(1f);
        }

        public void TickDespawn(float t) => SetBodyAlpha(DespawnAlphaAt(t));

        public void TickSpawn(float t) => SetBodyAlpha(SpawnAlphaAt(t));

        public static float DespawnAlphaAt(float t)
        {
            t = Mathf.Clamp01(t);
            var pulse = t * SpawnPulses;
            var index = Mathf.Min(Mathf.FloorToInt(pulse), SpawnPulses - 1);
            var local = pulse - index;
            if (local >= PulseOnRatio)
                return 0f;
            return DespawnPeaks[index];
        }

        public static float SpawnAlphaAt(float t)
        {
            t = Mathf.Clamp01(t);
            if (t >= 1f)
                return 1f;
            var pulse = t * SpawnPulses;
            var index = Mathf.Min(Mathf.FloorToInt(pulse), SpawnPulses - 1);
            var local = pulse - index;
            var last = index == SpawnPulses - 1;
            if (!last && local >= PulseOnRatio)
                return 0f;
            return SpawnPeaks[index];
        }

        public void SetBodyAlpha(float alpha)
        {
            _alpha = Mathf.Clamp01(alpha);
            if (_parts == null)
                return;
            for (var i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null)
                    continue;
                _parts[i].enabled = _alpha > 0.001f;
                var color = _partColors[i];
                color.a = _alpha;
                var material = Application.isPlaying ? _parts[i].material : _parts[i].sharedMaterial;
                if (material == null)
                    continue;
                material.color = color;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
            }
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
            _parts = _body.GetComponentsInChildren<Renderer>();
            _partColors = new Color[_parts.Length];
            for (var i = 0; i < _parts.Length; i++)
                _partColors[i] = _parts[i].sharedMaterial.color;
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
            part.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.UnlitAlpha("WalkerPart", color);
        }

        void Update()
        {
            if (_motionPaused)
                return;

            TickFacing(Time.deltaTime);
            if (_hopStartedAt < 0f)
                return;

            var t = TravelNormalized;
            var hopHeight = _faceTravel ? 0f : TravelHeight * transform.localScale.y;
            transform.position = _path != null
                ? PointOnTravel(_path, t, hopHeight)
                : PointOnTravel(_from, _to, t, hopHeight);

            if (_body != null)
            {
                if (hopHeight > 0.0001f)
                {
                    var squash = t < 0.5f
                        ? Mathf.Lerp(0.72f, 1.18f, t * 2f)
                        : Mathf.Lerp(1.18f, 1f, (t - 0.5f) * 2f);
                    _body.localScale = new Vector3(2f - squash, squash, 2f - squash);
                }
                else
                    _body.localScale = Vector3.one;
            }

            if (t >= 1f)
            {
                transform.position = _to;
                _path = null;
                _hopStartedAt = -1f;
                if (_body != null)
                    _body.localScale = Vector3.one;
            }
        }

        static Vector3 PointAlongPolyline(IReadOnlyList<Vector3> path, float normalizedTime)
        {
            var t = Mathf.Clamp01(normalizedTime);
            var total = PolylineLength(path);
            if (total < 0.0001f)
                return path[path.Count - 1];

            var remaining = t * total;
            for (var i = 1; i < path.Count; i++)
            {
                var segment = HorizontalDistance(path[i - 1], path[i]);
                if (remaining <= segment || i == path.Count - 1)
                    return Vector3.Lerp(path[i - 1], path[i], segment < 0.0001f ? 1f : remaining / segment);

                remaining -= segment;
            }

            return path[path.Count - 1];
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        Vector3 CurrentTravelTangent()
        {
            if (_path != null && _path.Length >= 2)
            {
                var t = TravelNormalized;
                var a = PointAlongPolyline(_path, Mathf.Max(0f, t - 0.02f));
                var b = PointAlongPolyline(_path, Mathf.Min(1f, t + 0.02f));
                var along = b - a;
                along.y = 0f;
                if (along.sqrMagnitude > 0.0001f)
                    return along;
            }

            var delta = _to - _from;
            delta.y = 0f;
            return delta;
        }

        void ApplyYaw(float yawDegrees)
        {
            YawDegrees = yawDegrees;
            _targetYaw = yawDegrees;
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }
    }
}
