using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Seamless patchwork floor: each cell shows one texture from a designer list. Tiles touch
    /// edge-to-edge (gap comes from <see cref="ArenaVisualSettings.TileGap"/> = 0).
    /// </summary>
    public sealed class PatchworkArenaTileViewFactory : ITileViewFactory
    {
        const string ShaderResourcePath = "Shaders/UnlitTexture";

        readonly ArenaVisualSettings _settings;
        Material _sharedMaterial;

        public PatchworkArenaTileViewFactory(ArenaVisualSettings settings)
        {
            _settings = settings;
        }

        public string ThemeId => "patchwork_arena_experimental";

        public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
        {
            var textures = _settings.PatchworkTextures;
            var index = PatchworkTexturePicker.PickIndex(coord, textures.Length, _settings.PatchworkSeed);
            var texture = textures[index];

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Patch " + coord;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }

            var tileRenderer = go.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = SharedMaterial();
            tileRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tileRenderer.receiveShadows = false;

            var view = go.AddComponent<PatchworkArenaTileView>();
            view.Initialise(coord, texture, tileRenderer);
            return view;
        }

        public void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent)
        {
            ArenaEnvironment.Clear(parent);

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = ScoutFogOfWar.Background;
            }

            BuildFogRoom(layout, parent);
        }

        Material SharedMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;

            var shader = Resources.Load<Shader>(ShaderResourcePath)
                         ?? Shader.Find("Nixin Studio/UnlitTexture")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default");

            _sharedMaterial = new Material(shader) { name = "PatchworkArenaTile" };
            if (_sharedMaterial.HasProperty("_BaseColor"))
                _sharedMaterial.SetColor("_BaseColor", Color.white);
            _sharedMaterial.color = Color.white;
            return _sharedMaterial;
        }

        static void BuildFogRoom(BoardLayout layout, Transform parent)
        {
            ScoutFogOfWar.Build(layout, parent);

            foreach (var light in Object.FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                    light.intensity = 0.35f;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.28f, 0.26f);
        }
    }
}
