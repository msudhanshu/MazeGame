using System.Collections.Generic;
using Game.Core.Rules;
using Game.Unity.Themes;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.View
{
    /// <summary>
    /// Theme-independent crumbs on the board: the walked trail, a home on the goal,
    /// and a cross on every blocked tile. Mosaic glass cannot carry this on its own.
    /// </summary>
    public sealed class GridPathOverlay : MonoBehaviour
    {
        public const string ObjectName = "Path Overlay";
        public const string DotsName = "Dots";
        public const string BlockedName = "Blocked";
        public const string ArrowsName = "Arrows";
        public const string ChoicesName = "Choices";
        public const string HomeName = "Home";
        public const string TrailName = "Trail";
        public const float Lift = 0.045f;
        public const float ChoiceOriginClearance = 0.10f;
        public const float CelebrateTraceSeconds = 2.2f;
        public const float CelebrateZoomSeconds = 1.8f;
        public const float CelebratePopupBeatSeconds = 0.55f;
        public const float AlreadyOverviewPopupDelay = 0.2f;
        public const float WalkArrowSpeedFactor = 0.28f;
        public const float CelebrateArrowSpeedFactor = 0.62f;

        public static readonly Color TrailTint = new Color(1f, 0.82f, 0.28f, 0.42f);
        public static readonly Color FocusTrailTint = new Color(1f, 0.82f, 0.28f, 0.22f);
        public static readonly Color CelebrateTint = new Color(1f, 0.94f, 0.45f, 0.68f);
        public static readonly Color DotTint = new Color(0.45f, 0.88f, 0.96f, 0.5f);
        public static readonly Color ArrowTint = new Color(1f, 0.92f, 0.4f, 0.5f);
        public static readonly Color FocusArrowTint = new Color(1f, 0.92f, 0.4f, 0.24f);
        public static readonly Color WrongTurnTint = new Color(1f, 0.22f, 0.22f, 0.92f);
        public static readonly Color ChoiceTint = new Color(0.84f, 0.9f, 1f, 0.62f);
        public static readonly Color RadarTrailTint = new Color(0.35f, 0.95f, 0.45f, 0.85f);
        public static readonly Color RadarArrowTint = new Color(0.55f, 1f, 0.62f, 0.95f);

        public static float CompletionHoldSeconds => CelebrateTraceSeconds + CelebratePopupBeatSeconds;

        static Texture2D _arrowTexture;
        static Texture2D _homeTexture;
        static Texture2D _crossTexture;

        LineRenderer _trail;
        Transform _dotsRoot;
        Transform _blockedRoot;
        Transform _arrowsRoot;
        Transform _choicesRoot;
        Transform _home;
        Material _trailMaterial;
        Material _dotMaterial;
        Material _arrowMaterial;
        Material _choiceMaterial;
        Material _homeMaterial;
        Material _crossMaterial;
        Vector3[] _points;
        Vector3[] _dotPoints;
        float _length;
        float _visibleLength;
        float _phase;
        float _baseWidth;
        float _arrowSpeed;
        float _dotScale;
        bool _focusedStyle = true;
        bool _radarPreview;
        int _radarArrowSegments = -1;
        Vector3[] _wrongTurnPoints;
        float _wrongTurnLength;
        float _wrongTurnEndsAt = -1f;
        float _choiceWidth = 0.09f;
        float _fade = 1f;
        readonly List<Vector3> _visiblePoints = new List<Vector3>(16);

        public bool IsCelebrating { get; private set; }
        public bool IsRadarPreview => _radarPreview;
        public LineRenderer Trail => _trail;

        public static GridPathOverlay Ensure(Transform parent)
        {
            var existing = parent.Find(ObjectName);
            if (existing != null)
            {
                var overlay = existing.GetComponent<GridPathOverlay>()
                              ?? existing.gameObject.AddComponent<GridPathOverlay>();
                overlay.BuildParts();
                return overlay;
            }

            var go = new GameObject(ObjectName);
            go.transform.SetParent(parent, false);
            var created = go.AddComponent<GridPathOverlay>();
            created.BuildParts();
            return created;
        }

        public static Vector3 PointAlong(IReadOnlyList<Vector3> points, float distance, out Vector3 direction)
        {
            direction = Vector3.right;
            if (points == null || points.Count == 0)
                return Vector3.zero;
            if (points.Count == 1)
                return points[0];

            var remaining = distance;
            if (remaining < 0f)
                remaining = 0f;

            for (var i = 0; i < points.Count - 1; i++)
            {
                var from = points[i];
                var to = points[i + 1];
                var delta = to - from;
                var span = delta.magnitude;
                if (span < 0.0001f)
                    continue;

                direction = delta / span;
                if (remaining < span)
                    return from + direction * remaining;

                remaining -= span;
            }

            var last = points[points.Count - 1];
            var previous = points[points.Count - 2];
            var tail = last - previous;
            if (tail.sqrMagnitude > 0.0001f)
                direction = tail.normalized;
            return last;
        }

        public void ResetVisuals()
        {
            IsCelebrating = false;
            _focusedStyle = true;
            _radarPreview = false;
            _radarArrowSegments = -1;
            _points = null;
            _dotPoints = null;
            _wrongTurnPoints = null;
            _wrongTurnLength = 0f;
            _wrongTurnEndsAt = -1f;
            _length = 0f;
            _visibleLength = 0f;
            _phase = 0f;
            _fade = 1f;
            if (_trail != null)
            {
                _trail.positionCount = 0;
                _trail.enabled = false;
            }

            Wipe(_dotsRoot);
            Wipe(_blockedRoot);
            Wipe(_arrowsRoot);
            Wipe(_choicesRoot);
            if (_home != null)
                _home.gameObject.SetActive(false);
        }

        public void SetFocusedStyle(bool focused)
        {
            if (_focusedStyle == focused)
                return;
            _focusedStyle = focused;
            ApplyVisibleTrail();
            RebuildArrows();
        }

        public void ShowWrongTurn(Vector3 from, Vector3 to, float seconds)
        {
            BuildParts();
            _wrongTurnPoints = new[] { from, to };
            _wrongTurnLength = Vector3.Distance(from, to);
            _wrongTurnEndsAt = Time.time + Mathf.Max(0.01f, seconds);
            _phase = 0f;
            ApplyVisibleTrail();
            RebuildArrows();
        }

        public void ShowChoices(Vector3 current, IReadOnlyList<Vector3> options, float width)
        {
            BuildParts();
            Wipe(_choicesRoot);
            if (options == null)
                return;

            var lineWidth = Mathf.Max(0.03f, width);
            _choiceWidth = lineWidth;
            for (var i = 0; i < options.Count; i++)
            {
                var go = new GameObject("Choice " + i);
                go.transform.SetParent(_choicesRoot, false);
                var line = go.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.alignment = LineAlignment.View;
                line.textureMode = LineTextureMode.Stretch;
                line.numCapVertices = 3;
                line.numCornerVertices = 3;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = ChoiceMaterial();
                line.startWidth = lineWidth;
                line.endWidth = lineWidth;
                line.startColor = Color.white;
                line.endColor = Color.white;
                var delta = options[i] - current;
                delta.y = 0f;
                var span = delta.magnitude;
                var start = current;
                if (span > 0.0001f)
                {
                    var inset = Mathf.Min(ChoiceOriginClearance, span * 0.12f);
                    start = current + delta * (inset / span);
                }

                line.SetPosition(0, start);
                line.SetPosition(1, options[i]);
            }
        }

        public void ClearChoices() => Wipe(_choicesRoot);

        public void SetFade(float alpha)
        {
            _fade = Mathf.Clamp01(alpha);
            ApplyVisibleTrail();
            FadeRoot(_dotsRoot);
            FadeRoot(_arrowsRoot);
            FadeRoot(_choicesRoot);
            if (_home != null)
                FadeRenderer(_home.GetComponent<Renderer>());
        }

        public void ShowHomeAt(Vector3 world, float tileSize)
        {
            BuildParts();
            PlaceHome(world, tileSize);
        }

        public void Refresh(GridWalkRun run, BoardLayout layout, bool showPath = false, float trailWidthScale = 1f)
        {
            if (run == null)
            {
                ResetVisuals();
                return;
            }

            BuildParts();
            var celebrating = run.IsLevelCompleted;
            var scale = Mathf.Max(0.15f, trailWidthScale);
            _baseWidth = Mathf.Max(0.018f, layout.TileSize * 0.07f * scale);
            _arrowSpeed = layout.Pitch * (celebrating ? CelebrateArrowSpeedFactor : WalkArrowSpeedFactor);

            var trailCells = showPath || run.IsLevelCompleted ? run.Path.Cells : run.WalkedCells;
            ShowPolyline(WorldPoints(trailCells, layout), layout.TileSize * 0.14f * scale, celebrating);
            PlaceHome(layout.WorldPosition(run.Path.Goal), layout.TileSize);
            RebuildBlocked(run.BlockedCells, layout);
            ClearChoices();
        }

        public void RefreshWorld(IReadOnlyList<Vector3> points, float markerScale, bool celebrating) =>
            RefreshWorld(points, points, markerScale, celebrating);

        public void RefreshWorld(
            IReadOnlyList<Vector3> trail,
            IReadOnlyList<Vector3> markers,
            float markerScale,
            bool celebrating)
        {
            BuildParts();
            _baseWidth = Mathf.Max(0.035f, markerScale * 0.45f);
            _arrowSpeed = Mathf.Max(0.08f, markerScale * 4f) * (celebrating ? CelebrateArrowSpeedFactor : WalkArrowSpeedFactor);
            ShowPolyline(CopyPoints(trail), CopyPoints(markers), markerScale, celebrating);
            if (_home != null)
                _home.gameObject.SetActive(false);
            Wipe(_blockedRoot);
            ClearChoices();
        }

        public void BeginRadarReveal(IReadOnlyList<Vector3> points, float tileSize)
        {
            BuildParts();
            IsCelebrating = false;
            _focusedStyle = false;
            _radarPreview = true;
            _wrongTurnPoints = null;
            _wrongTurnLength = 0f;
            _wrongTurnEndsAt = -1f;
            _baseWidth = Mathf.Max(0.04f, tileSize * 0.1f);
            _arrowSpeed = 0f;
            _dotScale = tileSize * 0.14f;
            SetPoints(CopyPoints(points));
            _dotPoints = null;
            _visibleLength = 0f;
            _radarArrowSegments = -1;
            Wipe(_dotsRoot);
            Wipe(_blockedRoot);
            ClearChoices();
            if (_points != null && _points.Length > 0)
                PlaceHome(_points[_points.Length - 1], tileSize);
            ApplyVisibleTrail();
            RebuildRadarArrows(0);
        }

        public void SetRadarReveal(float t)
        {
            if (!_radarPreview)
                return;

            _visibleLength = _length * Mathf.Clamp01(t);
            ApplyVisibleTrail();
            var segments = CountFullyRevealedSegments();
            if (segments == _radarArrowSegments)
                return;

            _radarArrowSegments = segments;
            RebuildRadarArrows(segments);
        }

        public void EndRadarReveal()
        {
            _radarPreview = false;
            _radarArrowSegments = -1;
            ResetVisuals();
        }

        void Update()
        {
            PulseChoices();

            if (_wrongTurnPoints != null && Time.time >= _wrongTurnEndsAt)
            {
                _wrongTurnPoints = null;
                _wrongTurnLength = 0f;
                _wrongTurnEndsAt = -1f;
                ApplyVisibleTrail();
                RebuildArrows();
            }

            if ((_points == null || _points.Length < 2 || _length < 0.001f) && _wrongTurnPoints == null)
                return;

            if (_radarPreview)
                return;

            if (IsCelebrating && _visibleLength < _length)
            {
                _visibleLength = Mathf.Min(_length, _visibleLength + Time.deltaTime * TraceSpeed);
                ApplyVisibleTrail();
            }

            _phase += Time.deltaTime * _arrowSpeed;
            AnimateArrows();
            if (!IsCelebrating || _trail == null)
                return;

            var pulse = 1f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 2.2f));
            _trail.startWidth = BaseTrailWidth * pulse;
            _trail.endWidth = BaseTrailWidth * pulse;
            if (_trailMaterial != null)
                Tint(_trailMaterial, CelebrateTint);
        }

        float TraceSpeed => _length / Mathf.Max(0.35f, CelebrateTraceSeconds);
        float BaseTrailWidth => _baseWidth * (IsCelebrating ? 1.35f : _focusedStyle ? 0.62f : 1f);

        void BuildParts()
        {
            if (_trail == null)
            {
                var trailGo = transform.Find(TrailName);
                if (trailGo == null)
                {
                    trailGo = new GameObject(TrailName).transform;
                    trailGo.SetParent(transform, false);
                }

                _trail = trailGo.GetComponent<LineRenderer>();
                if (_trail == null)
                    _trail = trailGo.gameObject.AddComponent<LineRenderer>();

                _trailMaterial = ArenaMaterials.Overlay("PathTrail", TrailTint, Texture2D.whiteTexture);
                _trail.sharedMaterial = _trailMaterial;
                _trail.useWorldSpace = true;
                _trail.alignment = LineAlignment.View;
                _trail.textureMode = LineTextureMode.Stretch;
                _trail.numCapVertices = 3;
                _trail.numCornerVertices = 3;
                _trail.shadowCastingMode = ShadowCastingMode.Off;
                _trail.receiveShadows = false;
                _trail.enabled = false;
            }

            _dotsRoot = Child(DotsName);
            _blockedRoot = Child(BlockedName);
            _arrowsRoot = Child(ArrowsName);
            _choicesRoot = Child(ChoicesName);
            if (_home == null)
            {
                var existing = transform.Find(HomeName);
                _home = existing != null ? existing : MakeQuad(HomeName, transform, HomeTexture(), HomeMaterial(), 1f).transform;
                _home.gameObject.SetActive(false);
            }
        }

        void ShowPolyline(Vector3[] points, float markerScale, bool celebrating) =>
            ShowPolyline(points, points, markerScale, celebrating);

        void ShowPolyline(Vector3[] points, Vector3[] markers, float markerScale, bool celebrating)
        {
            _dotScale = markerScale;
            var wasCelebrating = IsCelebrating;
            IsCelebrating = celebrating;
            _radarPreview = false;
            _radarArrowSegments = -1;
            SetPoints(points);
            _dotPoints = markers;
            if (IsCelebrating && !wasCelebrating)
                // Keep the whole trail visible; completion should "highlight + flash",
                // not spend time re-drawing from the source.
                _visibleLength = _length;
            else if (!IsCelebrating)
                _visibleLength = _length;
            else
                _visibleLength = Mathf.Min(_visibleLength, _length);

            ApplyVisibleTrail();
            RebuildDots();
            RebuildArrows();
        }

        void SetPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0)
            {
                _points = null;
                _length = 0f;
                _visibleLength = 0f;
                return;
            }

            _points = points;
            _length = 0f;
            for (var i = 1; i < points.Length; i++)
                _length += Vector3.Distance(points[i - 1], points[i]);
        }

        void ApplyVisibleTrail()
        {
            if (_trail == null)
                return;

            var points = DisplayPoints;
            var displayLength = DisplayLength;
            if (points == null || points.Length < 2 || displayLength < 0.001f)
            {
                _trail.positionCount = 0;
                _trail.enabled = false;
                return;
            }

            var visible = _wrongTurnPoints != null
                ? displayLength
                : _radarPreview || IsCelebrating ? Mathf.Clamp(_visibleLength, 0f, _length) : _length;
            _visiblePoints.Clear();
            _visiblePoints.Add(points[0]);
            var remaining = visible;
            for (var i = 0; i < points.Length - 1; i++)
            {
                var from = points[i];
                var to = points[i + 1];
                var span = Vector3.Distance(from, to);
                if (span < 0.0001f)
                    continue;

                if (remaining >= span - 0.0001f)
                {
                    _visiblePoints.Add(to);
                    remaining -= span;
                    continue;
                }

                if (remaining > 0.0001f)
                    _visiblePoints.Add(from + (to - from) / span * remaining);
                break;
            }

            if (_visiblePoints.Count < 2)
            {
                _trail.positionCount = 0;
                _trail.enabled = false;
                return;
            }

            _trail.enabled = true;
            _trail.positionCount = _visiblePoints.Count;
            _trail.SetPositions(_visiblePoints.ToArray());
            _trail.startColor = Color.white;
            _trail.endColor = Color.white;
            var width = CurrentTrailWidth();
            _trail.startWidth = width;
            _trail.endWidth = width;
            if (_trailMaterial != null)
                Tint(_trailMaterial, CurrentTrailTint());
        }

        void RebuildDots()
        {
            Wipe(_dotsRoot);
            var dots = _dotPoints ?? _points;
            if (dots == null)
                return;

            var scale = Mathf.Max(0.06f, _dotScale);
            var count = IsCelebrating ? dots.Length : Mathf.Max(0, dots.Length - 1);
            for (var i = 0; i < count; i++)
            {
                var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.name = "Dot " + i;
                dot.transform.SetParent(_dotsRoot, false);
                dot.transform.position = dots[i];
                dot.transform.localScale = Vector3.one * scale;
                DestroyCollider(dot);
                var renderer = dot.GetComponent<Renderer>();
                renderer.sharedMaterial = DotMaterial();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        void RebuildArrows()
        {
            Wipe(_arrowsRoot);
            var points = DisplayPoints;
            var length = DisplayLength;
            if (points == null || points.Length < 2 || length < 0.001f)
                return;

            var count = _wrongTurnPoints != null
                ? 1
                : IsCelebrating
                ? Mathf.Clamp(_points.Length + 2, 4, 8)
                : Mathf.Clamp(_points.Length - 1, 1, 5);
            for (var i = 0; i < count; i++)
            {
                var arrow = MakeQuad("Arrow " + i, _arrowsRoot, ArrowTexture(), ArrowMaterial(), 1f);
                arrow.transform.localScale = new Vector3(_baseWidth * 3.4f, _baseWidth * 2.4f, 1f);
            }

            AnimateArrows();
        }

        void RebuildRadarArrows(int segments)
        {
            Wipe(_arrowsRoot);
            if (_points == null || segments <= 0)
                return;

            var count = Mathf.Min(Mathf.Min(segments, _points.Length - 1), 14);
            for (var i = 0; i < count; i++)
            {
                var from = _points[i];
                var to = _points[i + 1];
                var delta = to - from;
                if (delta.sqrMagnitude < 0.0001f)
                    continue;

                var arrow = MakeQuad("Arrow " + i, _arrowsRoot, ArrowTexture(), ArrowMaterial(), 1f);
                arrow.transform.position = (from + to) * 0.5f + Vector3.up * 0.008f;
                arrow.transform.rotation = FlatFacing(delta);
                arrow.transform.localScale = new Vector3(_baseWidth * 3.8f, _baseWidth * 2.6f, 1f);
                var renderer = arrow.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    Tint(renderer.sharedMaterial, RadarArrowTint);
            }
        }

        int CountFullyRevealedSegments()
        {
            if (_points == null || _points.Length < 2 || _visibleLength < 0.0001f)
                return 0;

            var remaining = _visibleLength;
            var count = 0;
            for (var i = 0; i < _points.Length - 1; i++)
            {
                var span = Vector3.Distance(_points[i], _points[i + 1]);
                if (span < 0.0001f)
                    continue;
                if (remaining + 0.0001f < span)
                    break;
                remaining -= span;
                count++;
            }

            return count;
        }

        void AnimateArrows()
        {
            var points = DisplayPoints;
            if (_arrowsRoot == null || points == null || DisplayLength < 0.001f)
                return;

            var pathLength = VisiblePathLength;
            if (pathLength < 0.001f)
                return;

            var count = _arrowsRoot.childCount;
            if (count == 0)
                return;

            var spacing = pathLength / count;
            for (var i = 0; i < count; i++)
            {
                var distance = Mathf.Repeat(_phase + i * spacing, pathLength);
                var position = PointAlong(points, distance, out var direction);
                var arrow = _arrowsRoot.GetChild(i);
                arrow.position = position + Vector3.up * 0.008f;
                arrow.rotation = FlatFacing(direction);
                var renderer = arrow.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                    Tint(renderer.sharedMaterial, CurrentArrowTint());
            }
        }

        float VisiblePathLength => _wrongTurnPoints != null
            ? _wrongTurnLength
            : _radarPreview || IsCelebrating ? Mathf.Max(0f, _visibleLength) : _length;

        Vector3[] DisplayPoints => _wrongTurnPoints ?? _points;

        float DisplayLength => _wrongTurnPoints != null ? _wrongTurnLength : _length;

        static Vector3[] WorldPoints(IReadOnlyList<GridCoord> cells, BoardLayout layout)
        {
            if (cells == null || cells.Count == 0)
                return null;

            var points = new Vector3[cells.Count];
            for (var i = 0; i < cells.Count; i++)
                points[i] = layout.WorldPosition(cells[i]) + Vector3.up * Lift;
            return points;
        }

        static Vector3[] CopyPoints(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return null;

            var copy = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
                copy[i] = points[i];
            return copy;
        }

        void PlaceHome(Vector3 world, float tileSize)
        {
            _home.gameObject.SetActive(true);
            _home.position = world + Vector3.up * (Lift + 0.012f);
            _home.rotation = Quaternion.Euler(90f, 0f, 0f);
            var size = Mathf.Max(0.28f, tileSize * 0.42f);
            _home.localScale = new Vector3(size, size, 1f);
        }

        void RebuildBlocked(IReadOnlyList<GridCoord> blocked, BoardLayout layout)
        {
            Wipe(_blockedRoot);
            if (blocked == null)
                return;

            var size = Mathf.Max(0.3f, layout.TileSize * 0.52f);
            for (var i = 0; i < blocked.Count; i++)
            {
                var marker = MakeQuad("Cross " + blocked[i], _blockedRoot, CrossTexture(), CrossMaterial(), size);
                marker.transform.position = layout.WorldPosition(blocked[i]) + Vector3.up * (Lift + 0.01f);
                marker.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        Transform Child(string name)
        {
            var existing = transform.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        GameObject MakeQuad(string name, Transform parent, Texture texture, Material material, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(size, size, 1f);
            DestroyCollider(go);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (texture != null && material.mainTexture == null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }

            return go;
        }

        void PulseChoices()
        {
            if (_choicesRoot == null || _choicesRoot.childCount == 0)
                return;

            var wave = Mathf.Abs(Mathf.Sin(Time.time * 5.5f));
            var width = _choiceWidth * (1f + 0.28f * wave);
            var tint = Color.Lerp(ChoiceTint, DanceFloorPalette.CandidateEdge, 0.4f + 0.55f * wave);
            tint.a = 0.55f + 0.4f * wave;
            if (_choiceMaterial != null)
                Tint(_choiceMaterial, tint);

            for (var i = 0; i < _choicesRoot.childCount; i++)
            {
                var line = _choicesRoot.GetChild(i).GetComponent<LineRenderer>();
                if (line == null)
                    continue;
                line.startWidth = width;
                line.endWidth = width * 0.85f;
            }
        }

        Material DotMaterial() =>
            _dotMaterial != null ? _dotMaterial : _dotMaterial = ArenaMaterials.Overlay("PathDot", DotTint);

        Material ArrowMaterial() =>
            _arrowMaterial != null ? _arrowMaterial : _arrowMaterial = ArenaMaterials.Overlay("PathArrow", ArrowTint, ArrowTexture());

        Material ChoiceMaterial() =>
            _choiceMaterial != null ? _choiceMaterial : _choiceMaterial = ArenaMaterials.Overlay("PathChoice", ChoiceTint, Texture2D.whiteTexture);

        Material HomeMaterial() =>
            _homeMaterial != null
                ? _homeMaterial
                : _homeMaterial = ArenaMaterials.Overlay("PathHome", DanceFloorPalette.Goal, HomeTexture());

        Material CrossMaterial() =>
            _crossMaterial != null
                ? _crossMaterial
                : _crossMaterial = ArenaMaterials.Overlay("PathCross", Color.white, CrossTexture());

        public static Quaternion FlatFacing(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return Quaternion.LookRotation(Vector3.up, Vector3.back);

            var along = direction.normalized;
            var upOnQuad = Vector3.Cross(Vector3.up, along);
            if (upOnQuad.sqrMagnitude < 0.0001f)
                return Quaternion.LookRotation(Vector3.up, Vector3.back);

            return Quaternion.LookRotation(Vector3.up, upOnQuad);
        }

        static void Tint(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        Color FadeTint(Color color)
        {
            color.a *= _fade;
            return color;
        }

        void FadeRoot(Transform root)
        {
            if (root == null)
                return;
            for (var i = 0; i < root.childCount; i++)
                FadeRenderer(root.GetChild(i).GetComponent<Renderer>());
        }

        void FadeRenderer(Renderer renderer)
        {
            if (renderer == null)
                return;
            var material = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            if (material == null)
                return;
            var color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.color;
            color.a = Mathf.Clamp01(_fade);
            Tint(material, color);
        }

        Color CurrentTrailTint()
        {
            if (_wrongTurnPoints != null)
            {
                var pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 10f));
                return FadeTint(Color.Lerp(
                    new Color(WrongTurnTint.r, WrongTurnTint.g, WrongTurnTint.b, 0.35f),
                    WrongTurnTint,
                    pulse));
            }

            if (IsCelebrating)
                return FadeTint(CelebrateTint);

            if (_radarPreview)
                return FadeTint(RadarTrailTint);

            return FadeTint(_focusedStyle ? FocusTrailTint : TrailTint);
        }

        Color CurrentArrowTint()
        {
            if (_wrongTurnPoints != null)
            {
                var pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 10f));
                return FadeTint(Color.Lerp(
                    new Color(WrongTurnTint.r, WrongTurnTint.g, WrongTurnTint.b, 0.4f),
                    WrongTurnTint,
                    pulse));
            }

            if (_radarPreview)
                return FadeTint(RadarArrowTint);

            return FadeTint(_focusedStyle ? FocusArrowTint : ArrowTint);
        }

        float CurrentTrailWidth() => _wrongTurnPoints != null ? _baseWidth * 0.95f : BaseTrailWidth;

        static void Wipe(Transform root)
        {
            if (root == null)
                return;

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                DestroyObject(child);
            }
        }

        static void DestroyCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
                DestroyObject(collider);
        }

        static void DestroyObject(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        public static Texture2D SharedArrowTexture() => ArrowTexture();

        static Texture2D ArrowTexture() => _arrowTexture != null ? _arrowTexture : _arrowTexture = BuildArrowTexture();

        static Texture2D HomeTexture() => _homeTexture != null ? _homeTexture : _homeTexture = BuildHomeTexture();

        static Texture2D CrossTexture() => _crossTexture != null ? _crossTexture : _crossTexture = BuildCrossTexture();

        static Texture2D BuildArrowTexture()
        {
            const int size = 48;
            var tex = NewTexture(size, size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    var t = Mathf.InverseLerp(0.12f, 0.88f, u);
                    var half = Mathf.Lerp(0.38f, 0.02f, t);
                    var inside = u >= 0.12f && u <= 0.88f && Mathf.Abs(v - 0.5f) <= half;
                    var outline = u >= 0.06f && u <= 0.94f && Mathf.Abs(v - 0.5f) <= half + 0.08f;
                    tex.SetPixel(x, y, inside ? Color.white : outline ? new Color(0.12f, 0.1f, 0.05f, 0.9f) : Color.clear);
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        static Texture2D BuildHomeTexture()
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size;
                    var v = (y + 0.5f) / size;
                    var body = u > 0.28f && u < 0.72f && v > 0.14f && v < 0.52f;
                    var roof = v >= 0.48f && v <= 0.88f && Mathf.Abs(u - 0.5f) <= (0.88f - v) * 0.85f;
                    var door = u > 0.44f && u < 0.56f && v > 0.14f && v < 0.34f;
                    var chimney = u > 0.62f && u < 0.72f && v > 0.62f && v < 0.86f;
                    var fill = (body || roof || chimney) && !door;
                    var outline = InHome(u, v, 0.04f) && !InHome(u, v, 0f);
                    if (fill)
                        tex.SetPixel(x, y, Color.white);
                    else if (outline)
                        tex.SetPixel(x, y, new Color(0.18f, 0.12f, 0.04f, 1f));
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        static bool InHome(float u, float v, float pad)
        {
            var body = u > 0.28f - pad && u < 0.72f + pad && v > 0.14f - pad && v < 0.52f + pad;
            var roof = v >= 0.48f - pad && v <= 0.88f + pad && Mathf.Abs(u - 0.5f) <= (0.88f - v) * 0.85f + pad;
            var chimney = u > 0.62f - pad && u < 0.72f + pad && v > 0.62f - pad && v < 0.86f + pad;
            return body || roof || chimney;
        }

        static Texture2D BuildCrossTexture()
        {
            const int size = 64;
            var tex = NewTexture(size, size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = (x + 0.5f) / size - 0.5f;
                    var v = (y + 0.5f) / size - 0.5f;
                    var a = Mathf.Abs(u - v);
                    var b = Mathf.Abs(u + v);
                    var inner = Mathf.Min(a, b) < 0.09f && Mathf.Abs(u) < 0.38f && Mathf.Abs(v) < 0.38f;
                    var outline = Mathf.Min(a, b) < 0.16f && Mathf.Abs(u) < 0.44f && Mathf.Abs(v) < 0.44f;
                    if (inner)
                        tex.SetPixel(x, y, new Color(0.12f, 0.1f, 0.12f, 1f));
                    else if (outline)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.95f));
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply(false, false);
            return tex;
        }

        static Texture2D NewTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "PathOverlayGlyph",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            return tex;
        }
    }
}
