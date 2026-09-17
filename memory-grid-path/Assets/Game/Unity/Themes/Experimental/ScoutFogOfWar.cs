using Game.Unity.View;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Photoreal cloud deck around a scout arena. The playable rect stays clear; clouds
    /// live only outside it so follow-camera framing never hits a hard edge.
    /// </summary>
    public static class ScoutFogOfWar
    {
        public const string PaddingName = "Scout Fog Padding";
        public const string OverlayName = "Scout Fog Overlay";
        public const string PaddingShaderPath = "Shaders/ScoutFogPadding";
        public const string AtmosphereResourcePath = "ScoutFogAtmosphere";
        public const float SpanMultiplier = 12f;
        public const float EdgeOverlap = 0f;
        public const float Feather = 0.55f;
        public const float PaddingLift = 0.028f;
        public const float CloudTiling = 0.07f;

        public static readonly Color Background = new Color(0.55f, 0.72f, 0.84f);
        public static readonly Color PaddingColor = Color.white;

        static Texture2D _atmosphere;

        public static void Build(BoardLayout layout, Transform parent)
        {
            Build(layout.Origin, layout.SurfaceWidth, layout.SurfaceDepth, parent);
        }

        public static void Build(Vector3 origin, float width, float depth, Transform parent)
        {
            Clear(parent);
            BuildPadding(origin, width, depth, parent);
        }

        public static void Clear(Transform parent)
        {
            ArenaEnvironment.DestroyNow(Child(parent, PaddingName));
            ArenaEnvironment.DestroyNow(Child(parent, OverlayName));
        }

        /// <summary>
        /// 0 in the clear window, 1 in the opaque cloud field. Matches the padding shader hole.
        /// </summary>
        public static float PaddingAlpha(Vector3 world, Vector3 origin, float width, float depth)
        {
            var p = new Vector2(world.x - origin.x, world.z - origin.z);
            var hole = new Vector2(
                Mathf.Max(0.05f, width * 0.5f),
                Mathf.Max(0.05f, depth * 0.5f));
            var d = new Vector2(Mathf.Abs(p.x) - hole.x, Mathf.Abs(p.y) - hole.y);
            var outside = new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f));
            var sd = outside.magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sd / Mathf.Max(0.01f, Feather)));
        }

        static void BuildPadding(Vector3 origin, float width, float depth, Transform parent)
        {
            var material = PaddingMaterial();
            if (material == null)
            {
                BuildPaddingFrame(origin, width, depth, parent);
                return;
            }

            var span = Mathf.Max(width, depth, 8f) * SpanMultiplier;
            var go = CreateQuad(PaddingName, origin + new Vector3(0f, PaddingLift, 0f), span, span, parent);
            material.SetVector("_ArenaOrigin", origin);
            material.SetVector("_ArenaHalf", new Vector4(width * 0.5f, depth * 0.5f, 0f, 0f));
            material.SetFloat("_EdgeOverlap", EdgeOverlap);
            material.SetFloat("_Feather", Feather);
            material.SetFloat("_CloudTiling", CloudTiling);
            Tint(material, PaddingColor);
            var clouds = CloudTexture();
            material.mainTexture = clouds;
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", clouds);
            Assign(go, material);
        }

        static void BuildPaddingFrame(Vector3 origin, float width, float depth, Transform parent)
        {
            var root = new GameObject(PaddingName);
            root.transform.SetParent(parent, false);
            root.transform.position = origin + new Vector3(0f, PaddingLift, 0f);

            var span = Mathf.Max(width, depth, 8f) * SpanMultiplier;
            var innerW = Mathf.Max(0.1f, width - EdgeOverlap * 2f);
            var innerD = Mathf.Max(0.1f, depth - EdgeOverlap * 2f);
            var halfSpan = span * 0.5f;
            var clouds = CloudTexture();

            // Four slabs around the playable rect so a missing hole-shader cannot cover the arena.
            Strip("Cloud East", new Vector3((innerW + span) * 0.25f, 0f, 0f), halfSpan - innerW * 0.5f, span);
            Strip("Cloud West", new Vector3(-(innerW + span) * 0.25f, 0f, 0f), halfSpan - innerW * 0.5f, span);
            Strip("Cloud North", new Vector3(0f, 0f, (innerD + span) * 0.25f), innerW, halfSpan - innerD * 0.5f);
            Strip("Cloud South", new Vector3(0f, 0f, -(innerD + span) * 0.25f), innerW, halfSpan - innerD * 0.5f);

            void Strip(string name, Vector3 local, float worldW, float worldD)
            {
                var go = CreateQuad(name, root.transform.position + local, worldW, worldD, root.transform);
                var material = ArenaMaterials.Overlay(name, PaddingColor, clouds);
                material.renderQueue = 2999;
                var scale = new Vector2(Mathf.Max(0.01f, worldW * CloudTiling), Mathf.Max(0.01f, worldD * CloudTiling));
                material.SetTextureScale("_MainTex", scale);
                Assign(go, material);
            }
        }

        static GameObject CreateQuad(string name, Vector3 position, float worldWidth, float worldDepth, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(worldWidth, worldDepth, 1f);
            ArenaEnvironment.DestroyNow(go.GetComponent<Collider>());
            return go;
        }

        static void Assign(GameObject go, Material material)
        {
            var fogRenderer = go.GetComponent<Renderer>();
            fogRenderer.shadowCastingMode = ShadowCastingMode.Off;
            fogRenderer.receiveShadows = false;
            fogRenderer.sharedMaterial = material;
        }

        static Material PaddingMaterial()
        {
            var shader = Resources.Load<Shader>(PaddingShaderPath)
                         ?? Shader.Find("Nixin Studio/ScoutFogPadding");
            // A failed compile still Load()s the asset; using it paints magenta over the arena.
            if (shader == null || !shader.isSupported || shader.name.Contains("InternalError"))
                return null;

            var material = new Material(shader) { name = "ScoutFogPadding" };
            if (!material.HasProperty("_EdgeOverlap"))
            {
                ArenaEnvironment.DestroyNow(material);
                return null;
            }

            material.renderQueue = 2999;
            return material;
        }

        static Texture2D CloudTexture()
        {
            var imported = Resources.Load<Texture2D>(AtmosphereResourcePath);
            if (imported != null)
            {
                imported.wrapMode = TextureWrapMode.Repeat;
                imported.filterMode = FilterMode.Bilinear;
                return imported;
            }

            if (_atmosphere != null)
                return _atmosphere;

            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "ScoutFogAtmosphere",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            var pixels = new Color[size * size];
            var sky = new Color(0.55f, 0.72f, 0.84f, 0f);
            var puff = new Color(0.98f, 0.98f, 0.96f, 1f);
            var shade = new Color(0.82f, 0.86f, 0.90f, 0.9f);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var v = y / (float)size;
                    var billow = 1f - Mathf.Abs(Noise(u * 4.2f, v * 4.2f) * 2f - 1f);
                    billow = Mathf.Pow(billow, 2.4f);
                    var lumps = Noise(u * 9.5f + 2.1f, v * 9.5f + 4.7f);
                    var density = Mathf.SmoothStep(0.28f, 0.78f, billow * 0.75f + lumps * 0.35f);
                    var rgb = Color.Lerp(sky, Color.Lerp(shade, puff, lumps), density);
                    rgb.a = density;
                    pixels[y * size + x] = rgb;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            _atmosphere = texture;
            return _atmosphere;
        }

        static float Noise(float x, float y)
        {
            var ix = Mathf.FloorToInt(x);
            var iy = Mathf.FloorToInt(y);
            var fx = x - ix;
            var fy = y - iy;
            var ux = fx * fx * (3f - 2f * fx);
            var uy = fy * fy * (3f - 2f * fy);
            var a = Hash(ix, iy);
            var b = Hash(ix + 1, iy);
            var c = Hash(ix, iy + 1);
            var d = Hash(ix + 1, iy + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, ux), Mathf.Lerp(c, d, ux), uy);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                var n = x * 374761393 + y * 668265263;
                n = (n << 13) ^ n;
                return ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 2147483647f;
            }
        }

        static void Tint(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
        }

        static GameObject Child(Transform parent, string name)
        {
            if (parent == null)
                return null;
            var child = parent.Find(name);
            return child != null ? child.gameObject : null;
        }
    }
}
