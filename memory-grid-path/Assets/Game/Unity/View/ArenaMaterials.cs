using UnityEngine;

namespace Game.Unity.View
{
    /// <summary>
    /// Materials that ship in Resources so they survive a player build.
    /// Shader.Find("URP/Lit") is stripped on mobile and renders magenta.
    /// </summary>
    public static class ArenaMaterials
    {
        const string UnlitResourcePath = "Shaders/UnlitColor";
        const string OverlayResourcePath = "Shaders/UnlitOverlay";

        const string UnlitAlphaResourcePath = "Shaders/UnlitColorAlpha";

        public static Material Unlit(string name, Color color)
        {
            var shader = Resources.Load<Shader>(UnlitResourcePath)
                         ?? Shader.Find("Nixin Studio/UnlitColor")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name };
            Tint(material, color);
            return material;
        }

        public static Material UnlitAlpha(string name, Color color)
        {
            var shader = Resources.Load<Shader>(UnlitAlphaResourcePath)
                         ?? Shader.Find("Nixin Studio/UnlitColorAlpha")
                         ?? Resources.Load<Shader>(OverlayResourcePath)
                         ?? Shader.Find("Sprites/Default");
            var material = new Material(shader) { name = name };
            Tint(material, color);
            return material;
        }

        public static Material Overlay(string name, Color color, Texture texture = null)
        {
            var shader = Resources.Load<Shader>(OverlayResourcePath)
                         ?? Shader.Find("Nixin Studio/UnlitOverlay")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader) { name = name };
            Tint(material, color);
            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", texture);
            }

            return material;
        }

        static void Tint(Material material, Color color)
        {
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
