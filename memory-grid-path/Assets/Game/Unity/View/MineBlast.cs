using System.Collections;
using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>Burst of shards from a mine tile.</summary>
    public static class MineBlast
    {
        public static IEnumerator Play(Vector3 world, float tileSize)
        {
            var root = new GameObject("MineBlast");
            root.transform.position = world + Vector3.up * GridPathOverlay.Lift;
            var shards = new Transform[10];
            var dirs = new Vector3[10];
            for (var i = 0; i < shards.Length; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.transform.SetParent(root.transform, false);
                go.transform.localScale = Vector3.one * Mathf.Max(0.08f, tileSize * 0.18f);
                go.transform.localRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                    Object.Destroy(collider);
                var color = Color.Lerp(new Color(1f, 0.35f, 0.08f, 1f), Color.white, i % 2 == 0 ? 0.15f : 0.45f);
                go.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.Unlit("MineShard", color);
                shards[i] = go.transform;
                var angle = i / (float)shards.Length * Mathf.PI * 2f;
                dirs[i] = new Vector3(Mathf.Cos(angle), 0.35f, Mathf.Sin(angle));
            }

            var elapsed = 0f;
            const float duration = 0.38f;
            var shock = tileSize * 0.9f;
            while (elapsed < duration && root != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                for (var i = 0; i < shards.Length; i++)
                {
                    if (shards[i] == null)
                        continue;
                    shards[i].position = world + dirs[i] * shock * t + Vector3.up * (GridPathOverlay.Lift + t * 0.2f);
                    var fade = 1f - t;
                    shards[i].localScale = Vector3.one * Mathf.Max(0.04f, tileSize * 0.18f * (1f - t * 0.6f));
                    var renderer = shards[i].GetComponent<Renderer>();
                    if (renderer != null && renderer.material != null)
                    {
                        var c = renderer.material.color;
                        c.a = fade;
                        renderer.material.color = c;
                    }
                }

                yield return null;
            }

            if (root != null)
                Object.Destroy(root);
        }
    }
}
