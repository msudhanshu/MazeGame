using Game.Unity.Themes;
using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Mosaic arena: one photograph under the board, glass tiles sitting flush on top.
    /// </summary>
    public sealed class MosaicArenaTileViewFactory : ITileViewFactory
    {
        const string ShaderResourcePath = "Shaders/MosaicGlass";

        readonly ArenaVisualSettings _settings;
        Material _sharedMaterial;

        public MosaicArenaTileViewFactory(ArenaVisualSettings settings)
        {
            _settings = settings;
        }

        public string ThemeId => "mosaic_arena_experimental";

        public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Mosaic " + coord;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            Object.Destroy(go.GetComponent<Collider>());

            var tileRenderer = go.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = SharedMaterial();
            tileRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tileRenderer.receiveShadows = false;

            var view = go.AddComponent<MosaicArenaTileView>();
            view.Initialise(coord, tileRenderer);
            return view;
        }

        public void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent)
        {
            ArenaEnvironment.Clear(parent);

            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = DanceFloorPalette.Background;

                var cameraData = camera.GetUniversalAdditionalCameraData();
                if (cameraData != null)
                    cameraData.renderPostProcessing = true;
            }

            BuildDarkRoom(layout, parent);
            MosaicArenaBackdrop.Build(layout, parent, _settings);
            BuildBloom(parent);
        }

        Material SharedMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;

            var shader = Resources.Load<Shader>(ShaderResourcePath)
                         ?? Shader.Find("Nixin Studio/MosaicGlass")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");

            _sharedMaterial = new Material(shader) { name = "MosaicGlassTile" };
            _sharedMaterial.SetFloat("_SeamWidth", 0.028f);
            _sharedMaterial.SetFloat("_Rim", 2.4f);
            return _sharedMaterial;
        }

        static void BuildDarkRoom(BoardLayout layout, Transform parent)
        {
            ArenaEnvironment.PlaceDistantRoomFloor(layout, parent, DanceFloorPalette.Grout);

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    light.intensity = 0.08f;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.07f);
        }

        static void BuildBloom(Transform parent)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "MosaicArenaPost";

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.1f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.65f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.55f);

            var go = new GameObject("MosaicArena Post");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;
        }
    }
}
