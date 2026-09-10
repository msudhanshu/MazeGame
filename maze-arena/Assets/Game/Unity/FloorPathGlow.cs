using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Glowing centerline along every open corridor, sitting on the floor.</summary>
    public sealed class FloorPathGlow : MonoBehaviour
    {
        MeshFilter _filter;
        MeshRenderer _renderer;
        Mesh _mesh;
        Material _mat;
        bool _running;
        CorridorRail _built;

        public void SetRunning(bool running)
        {
            _running = running;
            if (_renderer != null)
                _renderer.enabled = running;
            if (!running)
                Clear();
        }

        public void Rebuild(CorridorRail rail, float originY)
        {
            Ensure();
            _built = rail;
            if (!_running || rail == null)
            {
                Clear();
                return;
            }

            var edges = rail.OpenEdges();
            if (edges.Count == 0)
            {
                Clear();
                return;
            }

            const float halfWidth = 0.055f;
            const float inset = 0.38f;
            var y = originY + 0.012f;
            var verts = new Vector3[edges.Count * 4];
            var tris = new int[edges.Count * 12];
            var colors = new Color[edges.Count * 4];
            var glow = new Color(0.2f, 0.95f, 1f, 0.42f);
            var used = 0;
            var tri = 0;
            for (var i = 0; i < edges.Count; i++)
            {
                var edge = edges[i];
                rail.CellCenter(edge.A, out var ax, out var az);
                rail.CellCenter(edge.B, out var bx, out var bz);
                var dx = bx - ax;
                var dz = bz - az;
                var len = Mathf.Sqrt(dx * dx + dz * dz);
                if (len <= inset * 2f + 0.05f)
                    continue;
                var inv = 1f / len;
                var ux = dx * inv;
                var uz = dz * inv;
                var px = -uz * halfWidth;
                var pz = ux * halfWidth;
                var x0 = ax + ux * inset;
                var z0 = az + uz * inset;
                var x1 = bx - ux * inset;
                var z1 = bz - uz * inset;
                var v = used;
                verts[v] = new Vector3(x0 - px, y, z0 - pz);
                verts[v + 1] = new Vector3(x0 + px, y, z0 + pz);
                verts[v + 2] = new Vector3(x1 + px, y, z1 + pz);
                verts[v + 3] = new Vector3(x1 - px, y, z1 - pz);
                colors[v] = glow;
                colors[v + 1] = glow;
                colors[v + 2] = glow;
                colors[v + 3] = glow;
                tris[tri] = v;
                tris[tri + 1] = v + 1;
                tris[tri + 2] = v + 2;
                tris[tri + 3] = v;
                tris[tri + 4] = v + 2;
                tris[tri + 5] = v + 3;
                tris[tri + 6] = v;
                tris[tri + 7] = v + 2;
                tris[tri + 8] = v + 1;
                tris[tri + 9] = v;
                tris[tri + 10] = v + 3;
                tris[tri + 11] = v + 2;
                used += 4;
                tri += 12;
            }

            if (used == 0)
            {
                Clear();
                return;
            }

            if (used != verts.Length)
            {
                var slimV = new Vector3[used];
                var slimC = new Color[used];
                var slimT = new int[tri];
                System.Array.Copy(verts, slimV, used);
                System.Array.Copy(colors, slimC, used);
                System.Array.Copy(tris, slimT, tri);
                verts = slimV;
                colors = slimC;
                tris = slimT;
            }

            _mesh.Clear();
            _mesh.vertices = verts;
            _mesh.colors = colors;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();
            _renderer.enabled = true;
        }

        void Ensure()
        {
            if (_filter != null)
                return;

            var meshGo = MeshHost();
            _filter = meshGo.GetComponent<MeshFilter>();
            if (_filter == null)
                _filter = meshGo.AddComponent<MeshFilter>();
            _renderer = meshGo.GetComponent<MeshRenderer>();
            if (_renderer == null)
                _renderer = meshGo.AddComponent<MeshRenderer>();
            if (_mesh == null)
                _mesh = new Mesh { name = "CorridorGlow" };
            _filter.sharedMesh = _mesh;
            _mat = ArenaMarkMaterial.CreateUnlit();
            if (_mat != null)
            {
                ArenaMarkMaterial.Tint(_mat, new Color(0.2f, 0.95f, 1f, 0.42f));
                _renderer.sharedMaterial = _mat;
            }

            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
        }

        GameObject MeshHost()
        {
            var existing = transform.Find("Mesh");
            if (existing != null)
                return existing.gameObject;

            var meshGo = new GameObject("Mesh");
            meshGo.transform.SetParent(transform, false);
            return meshGo;
        }

        void Clear()
        {
            if (_mesh != null)
                _mesh.Clear();
            if (_renderer != null)
                _renderer.enabled = false;
        }

        void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying)
                    Destroy(_mesh);
                else
                    DestroyImmediate(_mesh);
            }
        }
    }
}
