using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Unity.Themes
{
    /// <summary>
    /// Builds the board as a coloured tile floor with thin borders and sets up the camera
    /// and a dark room around it.
    /// </summary>
    public sealed class DanceFloorTileViewFactory : ITileViewFactory
    {
        const string ShaderResourcePath = "Shaders/TileGlow";

        Material _sharedMaterial;

        public string ThemeId => "dance_floor";

        public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Tile " + coord;
            go.transform.SetParent(parent, false);
            go.transform.position = worldPosition;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            // Tiles are picked by projecting a ray onto the board plane, so the primitive's
            // collider is only in the way.
            ArenaEnvironment.DestroyNow(go.GetComponent<Collider>());

            var tileRenderer = go.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = SharedMaterial();
            tileRenderer.shadowCastingMode = ShadowCastingMode.Off;
            tileRenderer.receiveShadows = false;

            var view = go.AddComponent<DanceFloorTileView>();
            view.Initialise(coord, DanceFloorPalette.BaseColorFor(coord), tileRenderer);
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
            BuildBloom(parent);
        }

        Material SharedMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;

            // Loaded from Resources rather than Shader.Find so the shader is guaranteed to
            // survive into a player build.
            var shader = Resources.Load<Shader>(ShaderResourcePath)
                         ?? Shader.Find("Nixin Studio/TileGlow")
                         ?? Shader.Find("Universal Render Pipeline/Unlit");

            _sharedMaterial = new Material(shader) { name = "DanceFloorTile" };
            _sharedMaterial.SetFloat("_Border", 0.028f);
            _sharedMaterial.SetFloat("_Alpha", 1f);
            return _sharedMaterial;
        }

        static void BuildDarkRoom(BoardLayout layout, Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Room Floor";
            floor.transform.SetParent(parent, false);
            // Plane primitives are 10 units across, and this one sits just under the tiles.
            floor.transform.position = layout.Origin + new Vector3(0f, -0.02f, 0f);
            floor.transform.localScale = new Vector3(layout.Width * 0.4f, 1f, layout.Depth * 0.4f);
            ArenaEnvironment.DestroyNow(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial = ArenaMaterials.Unlit("RoomFloor", DanceFloorPalette.Grout);

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
            profile.name = "DanceFloorPost";

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.55f);
            bloom.threshold.Override(0.92f);
            bloom.scatter.Override(0.5f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.35f);
            vignette.smoothness.Override(0.6f);

            var go = new GameObject("DanceFloor Post");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;
        }
    }
}
