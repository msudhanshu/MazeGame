using System;
using System.Collections;
using System.Collections.Generic;
using Game.Unity.Graph;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
        /// Level-start motion: tiles assemble on the grid; graph arenas bounce in brightness and scale.
    /// </summary>
    public sealed class ArenaIntroPlayer : MonoBehaviour
    {
        Coroutine _running;

        public static ArenaIntroPlayer Ensure(Transform host)
        {
            var player = host.GetComponent<ArenaIntroPlayer>();
            if (player == null)
                player = host.gameObject.AddComponent<ArenaIntroPlayer>();
            return player;
        }

        public bool IsPlaying => _running != null;

        public void PlayGrid(GridBoardView board, WalkerView walker, GridCoord origin, Action onDone)
        {
            Stop();
            _running = StartCoroutine(GridRoutine(board, walker, origin, onDone));
        }

        public void PlayGraph(GraphBoardView board, WalkerView walker, Action onDone)
        {
            var origin = board != null && board.Layout != null
                ? board.Layout.Origin
                : board != null ? board.transform.position : Vector3.zero;
            PlayBounce(board != null ? board.transform : null, walker, origin, onDone);
        }

        public void PlayBounce(Transform board, WalkerView walker, Vector3 origin, Action onDone, bool clearPropertyBlocks = true)
        {
            Stop();
            _running = StartCoroutine(BounceRoutine(board, walker, origin, onDone, clearPropertyBlocks));
        }

        public void Stop()
        {
            if (_running == null)
                return;
            StopCoroutine(_running);
            _running = null;
        }

        IEnumerator GridRoutine(GridBoardView board, WalkerView walker, GridCoord origin, Action onDone)
        {
            var tiles = CollectTiles(board);
            tiles.Sort((a, b) =>
            {
                var wave = ArenaIntroMotion.WaveIndex(a.Coord, origin).CompareTo(ArenaIntroMotion.WaveIndex(b.Coord, origin));
                if (wave != 0)
                    return wave;
                if (a.Coord.Y != b.Coord.Y)
                    return a.Coord.Y.CompareTo(b.Coord.Y);
                return a.Coord.X.CompareTo(b.Coord.X);
            });

            var stagger = ArenaIntroMotion.StaggerForCount(tiles.Count);
            var drop = board != null && board.IsBuilt ? board.Layout.TileSize * 0.65f : 0.4f;
            if (board != null && board.Overlay != null)
                board.Overlay.gameObject.SetActive(false);

            Vector3 walkerScale = Vector3.one;
            if (walker != null)
            {
                walkerScale = walker.transform.localScale;
                walker.transform.localScale = Vector3.zero;
            }

            for (var i = 0; i < tiles.Count; i++)
            {
                tiles[i].Transform.localScale = Vector3.zero;
                tiles[i].Transform.localPosition = tiles[i].RestPosition + Vector3.up * drop;
            }

            var elapsed = 0f;
            var duration = ArenaIntroMotion.GridTotalSeconds(tiles.Count);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < tiles.Count; i++)
                {
                    var t = (elapsed - i * stagger) / ArenaIntroMotion.GridTileSeconds;
                    ApplyTile(tiles[i], drop, t);
                }

                yield return null;
            }

            for (var i = 0; i < tiles.Count; i++)
                ApplyTile(tiles[i], drop, 1f);

            if (board != null && board.Overlay != null)
                board.Overlay.gameObject.SetActive(true);

            if (walker != null)
            {
                var pop = 0f;
                while (pop < 0.18f)
                {
                    pop += Time.unscaledDeltaTime;
                    var u = Mathf.Clamp01(pop / 0.18f);
                    walker.transform.localScale = walkerScale * ArenaIntroMotion.AssembleScale(u);
                    yield return null;
                }

                walker.transform.localScale = walkerScale;
            }

            _running = null;
            onDone?.Invoke();
        }

        IEnumerator BounceRoutine(Transform board, WalkerView walker, Vector3 origin, Action onDone, bool clearPropertyBlocks)
        {
            if (board == null)
            {
                _running = null;
                onDone?.Invoke();
                yield break;
            }

            var boardRest = board.localScale;
            var boardPos = board.position;
            var walkerRest = walker != null ? walker.transform.localScale : Vector3.one;
            var walkerPos = walker != null ? walker.transform.position : Vector3.zero;
            var renderers = CaptureRenderers(board);
            var lines = CaptureLines(board);

            var elapsed = 0f;
            while (elapsed < ArenaIntroMotion.GraphSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = elapsed / ArenaIntroMotion.GraphSeconds;
                var brightness = ArenaIntroMotion.GraphBrightness(t);
                var scale = ArenaIntroMotion.GraphScale(t);
                board.localScale = boardRest * scale;
                board.position = ArenaIntroMotion.ScaleAround(boardPos, origin, scale);
                if (walker != null)
                {
                    walker.transform.localScale = walkerRest * scale;
                    walker.transform.position = ArenaIntroMotion.ScaleAround(walkerPos, origin, scale);
                }

                ApplyBrightness(renderers, lines, brightness);
                yield return null;
            }

            board.localScale = boardRest;
            board.position = boardPos;
            if (walker != null)
            {
                walker.transform.localScale = walkerRest;
                walker.transform.position = walkerPos;
            }

            ApplyBrightness(renderers, lines, 1f);
            if (clearPropertyBlocks)
                ClearBlocks(renderers);

            _running = null;
            onDone?.Invoke();
        }

        static void ApplyTile(TilePose tile, float drop, float t)
        {
            t = Mathf.Clamp01(t);
            tile.Transform.localScale = tile.RestScale * ArenaIntroMotion.AssembleScale(t);
            tile.Transform.localPosition = tile.RestPosition + Vector3.up * (drop * ArenaIntroMotion.AssembleDrop(t));
        }

        static List<TilePose> CollectTiles(GridBoardView board)
        {
            var tiles = new List<TilePose>();
            if (board == null || board.Tiles == null)
                return tiles;
            foreach (var pair in board.Tiles)
            {
                var behaviour = pair.Value as MonoBehaviour;
                if (behaviour == null)
                    continue;
                var tf = behaviour.transform;
                tiles.Add(new TilePose
                {
                    Coord = pair.Key,
                    Transform = tf,
                    RestPosition = tf.localPosition,
                    RestScale = tf.localScale
                });
            }

            return tiles;
        }

        static List<RendererPose> CaptureRenderers(Transform root)
        {
            var list = new List<RendererPose>();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer is LineRenderer)
                    continue;
                list.Add(new RendererPose { Renderer = renderer, Rest = RestColor(renderer) });
            }

            return list;
        }

        static List<LinePose> CaptureLines(Transform root)
        {
            var list = new List<LinePose>();
            var lines = root.GetComponentsInChildren<LineRenderer>(true);
            for (var i = 0; i < lines.Length; i++)
                list.Add(new LinePose { Line = lines[i], Rest = lines[i].startColor });
            return list;
        }

        static void ApplyBrightness(List<RendererPose> renderers, List<LinePose> lines, float brightness)
        {
            var block = new MaterialPropertyBlock();
            for (var i = 0; i < renderers.Count; i++)
            {
                var pose = renderers[i];
                if (pose.Renderer == null)
                    continue;
                var color = Tint(pose.Rest, brightness);
                pose.Renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                block.SetColor("_Color", color);
                block.SetColor("_GlowColor", color);
                pose.Renderer.SetPropertyBlock(block);
            }

            for (var i = 0; i < lines.Count; i++)
            {
                var pose = lines[i];
                if (pose.Line == null)
                    continue;
                var color = Tint(pose.Rest, brightness);
                pose.Line.startColor = color;
                pose.Line.endColor = color;
            }
        }

        static void ClearBlocks(List<RendererPose> renderers)
        {
            for (var i = 0; i < renderers.Count; i++)
            {
                if (renderers[i].Renderer != null)
                    renderers[i].Renderer.SetPropertyBlock(null);
            }
        }

        static Color RestColor(Renderer renderer)
        {
            var color = Color.white;
            var material = renderer.sharedMaterial;
            if (material != null)
            {
                if (material.HasProperty("_BaseColor"))
                    color = material.GetColor("_BaseColor");
                else if (material.HasProperty("_Color"))
                    color = material.GetColor("_Color");
                else if (material.HasProperty("_GlowColor"))
                    color = material.GetColor("_GlowColor");
            }

            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (block.HasColor("_BaseColor"))
                color = block.GetColor("_BaseColor");
            else if (block.HasColor("_Color"))
                color = block.GetColor("_Color");
            else if (block.HasColor("_GlowColor"))
                color = block.GetColor("_GlowColor");
            return color;
        }

        static Color Tint(Color rest, float brightness)
        {
            if (brightness <= 1f)
                return new Color(rest.r * brightness, rest.g * brightness, rest.b * brightness, rest.a);

            var extra = brightness - 1f;
            return new Color(
                Mathf.Lerp(rest.r, 1f, extra),
                Mathf.Lerp(rest.g, 1f, extra),
                Mathf.Lerp(rest.b, 1f, extra),
                rest.a);
        }

        struct TilePose
        {
            public GridCoord Coord;
            public Transform Transform;
            public Vector3 RestPosition;
            public Vector3 RestScale;
        }

        struct RendererPose
        {
            public Renderer Renderer;
            public Color Rest;
        }

        struct LinePose
        {
            public LineRenderer Line;
            public Color Rest;
        }
    }
}
