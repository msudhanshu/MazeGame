using Game.Unity.View;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Ocean under and around scout boards so camera framing never hits a hard dark edge.
    /// Uses the imported FX/ProceduralWater material when it is present.
    /// </summary>
    public static class PatchworkOceanBackdrop
    {
        public const string OceanName = "Patchwork Ocean";
        public const string MaterialResourcePath = "OceanWater";
        public const string ProceduralShaderName = "FX/ProceduralWater";
        public const string FallbackShaderPath = "Shaders/OceanWater";
        public const float OceanDepth = 0.08f;
        public const float SpanMultiplier = 12f;

        public static readonly Color Background = new Color(0.01f, 0.06f, 0.10f);

        public static void Build(BoardLayout layout, Transform parent)
        {
            Build(layout.Origin, layout.Width, layout.Depth, parent);
        }

        public static void Build(Vector3 origin, float width, float depth, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = OceanName;
            go.transform.SetParent(parent, false);
            go.transform.position = origin + new Vector3(0f, -OceanDepth, 0f);
            var span = Mathf.Max(width, depth, 8f) * SpanMultiplier;
            go.transform.localScale = new Vector3(span / 10f, 1f, span / 10f);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }

            var oceanRenderer = go.GetComponent<Renderer>();
            oceanRenderer.shadowCastingMode = ShadowCastingMode.Off;
            oceanRenderer.receiveShadows = false;
            oceanRenderer.sharedMaterial = OceanMaterial();
        }

        static Material OceanMaterial()
        {
            var template = Resources.Load<Material>(MaterialResourcePath);
            if (template != null)
                return new Material(template) { name = "PatchworkOcean" };

            var shader = Shader.Find(ProceduralShaderName)
                         ?? Resources.Load<Shader>(FallbackShaderPath)
                         ?? Shader.Find("Nixin Studio/OceanWater")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(shader) { name = "PatchworkOcean" };
        }
    }
}
