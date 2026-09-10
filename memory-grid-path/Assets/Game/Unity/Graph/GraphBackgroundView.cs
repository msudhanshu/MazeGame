using Game.Unity.Data;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Graph
{
    public static class GraphBackgroundView
    {
        public const string ObjectName = "Graph Background";

        public static GameObject Build(GraphLevelDefinition level, GraphBoardLayout layout, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = ObjectName;
            go.transform.SetParent(parent, false);
            go.transform.position = layout.Origin + new Vector3(0f, -0.03f, 0f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(layout.WorldWidth, layout.WorldDepth, 1f);
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }

            var renderer = go.GetComponent<Renderer>();
            if (level.Background != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Unlit/Texture")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    var material = new Material(shader) { name = "GraphBackground" };
                    material.color = Color.white;
                    material.mainTexture = level.Background;
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", level.Background);
                    if (material.HasProperty("_BaseColor"))
                        material.SetColor("_BaseColor", Color.white);
                    renderer.sharedMaterial = material;
                }
            }
            else
            {
                renderer.sharedMaterial = ArenaMaterials.Unlit("GraphBackground", new Color(0.12f, 0.18f, 0.14f));
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }
    }
}
