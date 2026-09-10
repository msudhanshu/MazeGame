using Nixin.Maze;
using UnityEngine;

namespace Game.Unity
{
    public static class MazeThemeFactory
    {
        public static MazeStyleSet Create(string name, MazeStyleAssignment assignment, params Color[] wallColors)
        {
            var set = ScriptableObject.CreateInstance<MazeStyleSet>();
            set.name = name;
            set.Assignment = assignment;
            set.Walls = new MazeWallStyle[wallColors.Length];
            for (var i = 0; i < wallColors.Length; i++)
            {
                var style = ScriptableObject.CreateInstance<MazeWallStyle>();
                style.name = name + " Wall " + i;
                style.Material = CreateLit(wallColors[i], "NixinMaze/WallLit");
                set.Walls[i] = style;
            }

            var floor = ScriptableObject.CreateInstance<MazeWallStyle>();
            floor.name = name + " Floor";
            floor.Material = CreateLit(new Color(0.35f, 0.32f, 0.28f), "NixinMaze/FloorLit");
            set.Floor = floor;

            var ceiling = ScriptableObject.CreateInstance<MazeWallStyle>();
            ceiling.name = name + " Ceiling";
            ceiling.Material = CreateLit(new Color(0.55f, 0.55f, 0.52f), "NixinMaze/CeilingLit");
            set.Ceiling = ceiling;
            return set;
        }

        public static MazeStyleSet[] All()
        {
            return new[]
            {
                Create("Brick", MazeStyleAssignment.Uniform, new Color(0.62f, 0.28f, 0.22f)),
                Create("Wood", MazeStyleAssignment.Uniform, new Color(0.55f, 0.36f, 0.18f)),
                Create("Metal", MazeStyleAssignment.Uniform, new Color(0.45f, 0.48f, 0.52f)),
                Create("Grass", MazeStyleAssignment.Uniform, new Color(0.28f, 0.48f, 0.22f)),
                Create("Creeper", MazeStyleAssignment.Uniform, new Color(0.18f, 0.32f, 0.16f)),
                Create("Mixed", MazeStyleAssignment.SeededRandom,
                    new Color(0.62f, 0.28f, 0.22f),
                    new Color(0.55f, 0.36f, 0.18f),
                    new Color(0.45f, 0.48f, 0.52f),
                    new Color(0.28f, 0.48f, 0.22f))
            };
        }

        static Material CreateLit(Color color, string templateResource)
        {
            var template = Resources.Load<Material>(templateResource);
            Material material;
            if (template != null)
            {
                material = new Material(template);
            }
            else
            {
                var shader = FindLitShader();
                if (shader == null)
                    return null;
                material = new Material(shader);
            }

            material.enableInstancing = true;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", color.grayscale > 0.45f && color.r < 0.5f ? 0.65f : 0.05f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.35f);
            return material;
        }

        static Shader FindLitShader()
        {
            var named = Shader.Find("Universal Render Pipeline/Lit");
            if (named != null)
                return named;

            var wall = Resources.Load<GameObject>("NixinMaze/Wall");
            if (wall != null)
            {
                var renderer = wall.transform.Find("Mesh") != null
                    ? wall.transform.Find("Mesh").GetComponent<Renderer>()
                    : wall.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null)
                    return renderer.sharedMaterial.shader;
            }

            return Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        }
    }
}
