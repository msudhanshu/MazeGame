using Game.Unity.Themes.Experimental;
using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.TestLab
{
    /// <summary>
    /// Mosaic glass arena with a world-space coordinate label on every tile.
    /// </summary>
    public sealed class MosaicDebugTileViewFactory : ITileViewFactory
    {
        readonly MosaicArenaTileViewFactory _mosaic;

        public MosaicDebugTileViewFactory(ArenaVisualSettings settings, Texture2D mosaicTexture)
        {
            var resolved = settings;
            if (resolved == null || resolved.MosaicTexture == null)
                resolved = ArenaVisualSettings.CreateMosaicOverride(mosaicTexture);
            _mosaic = new MosaicArenaTileViewFactory(resolved);
        }

        public string ThemeId => "testlab_mosaic_debug";

        public ITileView CreateTile(GridCoord coord, GridSize size, Vector3 worldPosition, float tileSize, Transform parent)
        {
            var view = _mosaic.CreateTile(coord, size, worldPosition, tileSize, parent);
            if (view is MonoBehaviour behaviour)
                AddLabel(behaviour.transform, coord);
            return view;
        }

        public void ApplyEnvironment(Camera camera, BoardLayout layout, Transform parent)
        {
            _mosaic.ApplyEnvironment(camera, layout, parent);
        }

        static void AddLabel(Transform tile, GridCoord coord)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(tile, false);
            labelGo.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            labelGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelGo.transform.localScale = Vector3.one * 0.14f;

            var text = labelGo.AddComponent<TextMesh>();
            text.text = coord.X + "," + coord.Y;
            text.fontSize = 48;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;
            text.fontStyle = FontStyle.Bold;
        }
    }
}
