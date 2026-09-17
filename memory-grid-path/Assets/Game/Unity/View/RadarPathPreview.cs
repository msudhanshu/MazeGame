using System.Collections;
using System.Collections.Generic;
using Game.Core.Domain;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.View
{
    /// <summary>
    /// Night-radar glance: a full-width bar sweeps the board from bottom to top.
    /// The path trail appears only where the scan has already passed.
    /// </summary>
    public sealed class RadarPathPreview : MonoBehaviour
    {
        static readonly Color Veil = new Color(0.02f, 0.10f, 0.06f, 0.72f);
        static readonly Color PathGlow = new Color(0.35f, 0.95f, 0.45f, 0.42f);
        static readonly Color Scan = new Color(0.55f, 1f, 0.62f, 0.55f);
        static readonly Color Trail = new Color(0.35f, 0.95f, 0.45f, 0.85f);
        static readonly Color Arrow = new Color(0.55f, 1f, 0.62f, 0.95f);
        static readonly Color FlashBurstTint = new Color(1f, 1f, 1f, 0.82f);
        static readonly Color FlashTrail = new Color(0.96f, 0.98f, 1f, 0.92f);
        static readonly Color FlashArrow = new Color(1f, 1f, 1f, 0.95f);

        Transform _root;

        public static RadarPathPreview Ensure(Transform parent)
        {
            var existing = parent != null ? parent.Find("Radar Preview") : null;
            if (existing != null)
                return existing.GetComponent<RadarPathPreview>() ?? existing.gameObject.AddComponent<RadarPathPreview>();

            var go = new GameObject("Radar Preview");
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.AddComponent<RadarPathPreview>();
        }

        public static bool TryClipSegment(Vector3 from, Vector3 to, float scanZ, out Vector3 a, out Vector3 b)
        {
            var fromBehind = from.z <= scanZ + 0.0001f;
            var toBehind = to.z <= scanZ + 0.0001f;
            if (fromBehind && toBehind)
            {
                a = from;
                b = to;
                return Vector3.Distance(from, to) > 0.0001f;
            }

            if (fromBehind && !toBehind)
            {
                a = from;
                b = PointAtZ(from, to, scanZ);
                return Vector3.Distance(a, b) > 0.0001f;
            }

            if (!fromBehind && toBehind)
            {
                a = PointAtZ(from, to, scanZ);
                b = to;
                return Vector3.Distance(a, b) > 0.0001f;
            }

            a = from;
            b = to;
            return false;
        }

        public IEnumerator Play(GridBoardView board, IReadOnlyList<GridCoord> path, float seconds) =>
            Play(board, path, seconds, PathPreviewSeconds.Hold);

        public IEnumerator Play(
            GridBoardView board,
            IReadOnlyList<GridCoord> path,
            float seconds,
            float holdSeconds,
            IRadarScanControls controls = null)
        {
            Clear();
            if (board == null || !board.IsBuilt || path == null || path.Count < 2 || seconds <= 0.05f)
                yield break;

            var layout = board.Layout;
            var origin = layout.Origin + new Vector3(0f, GridPathOverlay.Lift + 0.08f, 0f);
            var world = new Vector3[path.Count];
            for (var i = 0; i < path.Count; i++)
                world[i] = board.WorldPosition(path[i]) + Vector3.up * GridPathOverlay.Lift;

            yield return Sweep(
                world,
                origin,
                layout.SurfaceWidth + 0.4f,
                layout.SurfaceDepth + 0.4f,
                layout.SurfaceWidth + 0.35f,
                layout.TileSize * 0.22f,
                layout.TileSize * 0.16f,
                layout.TileSize * 0.28f,
                seconds,
                holdSeconds,
                showCells: true,
                cellSize: layout.TileSize,
                controls: controls);
        }

        public IEnumerator Play(
            IReadOnlyList<Vector3> worldPath,
            Vector3 origin,
            float surfaceWidth,
            float surfaceDepth,
            float seconds) =>
            Play(worldPath, origin, surfaceWidth, surfaceDepth, seconds, PathPreviewSeconds.Hold);

        public IEnumerator Play(
            IReadOnlyList<Vector3> worldPath,
            Vector3 origin,
            float surfaceWidth,
            float surfaceDepth,
            float seconds,
            float holdSeconds,
            IRadarScanControls controls = null)
        {
            Clear();
            if (worldPath == null || worldPath.Count < 2 || seconds <= 0.05f)
                yield break;

            var bar = Mathf.Max(0.16f, surfaceDepth * 0.045f);
            var line = Mathf.Max(0.08f, surfaceWidth * 0.018f);
            yield return Sweep(
                worldPath,
                origin,
                surfaceWidth + 0.4f,
                surfaceDepth + 0.4f,
                surfaceWidth + 0.35f,
                bar,
                line,
                line * 1.7f,
                seconds,
                holdSeconds,
                showCells: false,
                cellSize: 0f,
                controls: controls);
        }

        public IEnumerator PlayFlash(
            IReadOnlyList<Vector3> worldPath,
            Vector3 origin,
            float width,
            float depth,
            float seconds,
            IRadarScanControls controls = null)
        {
            Clear();
            if (worldPath == null || worldPath.Count < 2 || seconds <= 0f)
                yield break;

            _root = new GameObject("Flash").transform;
            _root.SetParent(transform, false);

            var lineWidth = Mathf.Max(0.08f, width * 0.018f);
            var arrowSize = lineWidth * 1.7f;
            var burst = Quad(
                "Burst",
                origin + Vector3.up * 0.04f,
                width + 0.4f,
                depth + 0.4f,
                FlashBurstTint);
            burst.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var burstRenderer = burst.GetComponent<Renderer>();

            for (var i = 0; i < worldPath.Count - 1; i++)
            {
                var from = worldPath[i];
                var to = worldPath[i + 1];
                var line = MakeLine("FlashSeg " + i, lineWidth, FlashTrail);
                line.enabled = true;
                line.positionCount = 2;
                line.SetPosition(0, from + Vector3.up * 0.02f);
                line.SetPosition(1, to + Vector3.up * 0.02f);

                var arrow = MakeArrow("FlashArrow " + i, arrowSize, FlashArrow);
                arrow.SetActive(true);
                arrow.transform.position = (from + to) * 0.5f + Vector3.up * 0.03f;
                arrow.transform.rotation = GridPathOverlay.FlatFacing(to - from);
            }

            var elapsed = 0f;
            var burstSeconds = GraphPathPreviewSeconds.FlashBurst;
            var total = Mathf.Max(0.05f, seconds);
            controls?.SetProgress(0f, total);
            while (elapsed < seconds)
            {
                elapsed = TickElapsed(elapsed, Time.unscaledDeltaTime, controls != null && controls.HeldPaused);
                if (elapsed > seconds)
                    elapsed = seconds;
                controls?.SetProgress(elapsed, total);
                if (burstRenderer != null)
                {
                    var t = burstSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / burstSeconds);
                    var color = FlashBurstTint;
                    color.a = Mathf.Lerp(FlashBurstTint.a, 0f, t);
                    TintRenderer(burstRenderer, color);
                    if (t >= 1f)
                        burst.SetActive(false);
                }

                yield return null;
            }

            Clear();
        }

        public void Clear()
        {
            if (_root == null)
                return;
            if (Application.isPlaying)
                Destroy(_root.gameObject);
            else
                DestroyImmediate(_root.gameObject);
            _root = null;
        }

        IEnumerator Sweep(
            IReadOnlyList<Vector3> world,
            Vector3 origin,
            float veilWidth,
            float veilDepth,
            float scanWidth,
            float scanBarDepth,
            float lineWidth,
            float arrowSize,
            float seconds,
            float holdSeconds,
            bool showCells,
            float cellSize,
            IRadarScanControls controls)
        {
            _root = new GameObject("Radar").transform;
            _root.SetParent(transform, false);

            var veil = Quad("Veil", origin, veilWidth, veilDepth, Veil);
            veil.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var cells = System.Array.Empty<GameObject>();
            if (showCells)
            {
                cells = new GameObject[world.Count];
                var goal = world.Count - 1;
                for (var i = 0; i < world.Count; i++)
                {
                    var isHome = i == goal;
                    var glow = isHome
                        ? new Color(PathGlow.r, PathGlow.g, PathGlow.b, 0.16f)
                        : new Color(PathGlow.r, PathGlow.g, PathGlow.b, 0.3f);
                    var tile = Quad(
                        "Cell" + i,
                        world[i] + Vector3.up * 0.02f,
                        cellSize * (isHome ? 0.72f : 0.84f),
                        cellSize * (isHome ? 0.72f : 0.84f),
                        glow);
                    tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    tile.SetActive(false);
                    cells[i] = tile;
                }
            }

            var lines = new LineRenderer[world.Count - 1];
            var arrows = new GameObject[world.Count - 1];
            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = MakeLine("Seg " + i, lineWidth);
                lines[i].enabled = false;
                arrows[i] = MakeArrow("Arrow " + i, arrowSize);
                arrows[i].SetActive(false);
            }

            var fromZ = origin.z - veilDepth * 0.5f;
            var toZ = origin.z + veilDepth * 0.5f;
            var scan = Quad(
                "Scan",
                new Vector3(origin.x, origin.y + 0.03f, fromZ),
                scanWidth,
                scanBarDepth,
                Scan);
            scan.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var elapsed = 0f;
            var total = Mathf.Max(0.05f, seconds + Mathf.Max(0f, holdSeconds));
            ApplyScan(fromZ, world, cells, lines, arrows, scan);
            controls?.SetProgress(0f, total);
            while (elapsed < seconds)
            {
                elapsed = TickElapsed(elapsed, Time.unscaledDeltaTime, controls != null && controls.HeldPaused);
                if (elapsed > seconds)
                    elapsed = seconds;
                var z = Mathf.Lerp(fromZ, toZ, Mathf.Clamp01(elapsed / seconds));
                ApplyScan(z, world, cells, lines, arrows, scan);
                controls?.SetProgress(elapsed, total);
                yield return null;
            }

            ApplyScan(toZ, world, cells, lines, arrows, scan);
            if (holdSeconds < 0f)
                holdSeconds = 0f;
            var held = 0f;
            while (held < holdSeconds)
            {
                held = TickElapsed(held, Time.unscaledDeltaTime, controls != null && controls.HeldPaused);
                if (held > holdSeconds)
                    held = holdSeconds;
                controls?.SetProgress(seconds + held, total);
                yield return null;
            }

            controls?.SetProgress(total, total);
            Clear();
        }

        public static float TickElapsed(float elapsed, float delta, bool paused) =>
            paused ? elapsed : elapsed + Mathf.Max(0f, delta);

        void ApplyScan(
            float scanZ,
            IReadOnlyList<Vector3> world,
            GameObject[] cells,
            LineRenderer[] lines,
            GameObject[] arrows,
            GameObject scan)
        {
            if (scan != null)
            {
                var p = scan.transform.position;
                scan.transform.position = new Vector3(p.x, p.y, scanZ);
            }

            for (var i = 0; i < cells.Length; i++)
            {
                if (cells[i] != null)
                    cells[i].SetActive(world[i].z <= scanZ + 0.001f);
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var from = world[i];
                var to = world[i + 1];
                if (TryClipSegment(from, to, scanZ, out var a, out var b))
                {
                    lines[i].enabled = true;
                    lines[i].positionCount = 2;
                    lines[i].SetPosition(0, a + Vector3.up * 0.02f);
                    lines[i].SetPosition(1, b + Vector3.up * 0.02f);
                }
                else
                {
                    lines[i].enabled = false;
                }

                var fullyBehind = from.z <= scanZ + 0.001f && to.z <= scanZ + 0.001f;
                if (fullyBehind && arrows[i] != null)
                {
                    arrows[i].SetActive(true);
                    arrows[i].transform.position = (from + to) * 0.5f + Vector3.up * 0.03f;
                    arrows[i].transform.rotation = GridPathOverlay.FlatFacing(to - from);
                }
                else if (arrows[i] != null)
                {
                    arrows[i].SetActive(false);
                }
            }
        }

        LineRenderer MakeLine(string name, float width) => MakeLine(name, width, Trail);

        LineRenderer MakeLine(string name, float width, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = ArenaMaterials.Overlay(name, color);
            line.startWidth = width;
            line.endWidth = width * 0.85f;
            line.startColor = color;
            line.endColor = color;
            return line;
        }

        GameObject MakeArrow(string name, float size) => MakeArrow(name, size, Arrow);

        GameObject MakeArrow(string name, float size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.localScale = new Vector3(size * 1.2f, size * 0.85f, 1f);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = ArenaMaterials.Overlay(name, color, GridPathOverlay.SharedArrowTexture());
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        static void TintRenderer(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            var material = renderer.material;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        GameObject Quad(string name, Vector3 position, float width, float depth, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(_root, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(width, depth, 1f);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Destroy(collider);
                else
                    DestroyImmediate(collider);
            }

            go.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.Overlay(name, color);
            return go;
        }

        static Vector3 PointAtZ(Vector3 from, Vector3 to, float scanZ)
        {
            var span = to.z - from.z;
            if (Mathf.Abs(span) < 0.0001f)
                return from;
            var t = Mathf.Clamp01((scanZ - from.z) / span);
            return Vector3.Lerp(from, to, t);
        }

        void OnDestroy() => Clear();
    }
}
