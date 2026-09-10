using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// One patchwork panel: the cell texture with no glow, pulse, or overlay.
    /// </summary>
    public sealed class PatchworkArenaTileView : MonoBehaviour, ITileView
    {
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        public GridCoord Coord { get; private set; }
        public TileVisualState State { get; private set; } = TileVisualState.Idle;

        public void Initialise(GridCoord coord, Texture2D texture, Renderer tileRenderer)
        {
            Coord = coord;
            if (tileRenderer == null)
                return;

            var block = new MaterialPropertyBlock();
            tileRenderer.GetPropertyBlock(block);
            if (texture != null)
            {
                block.SetTexture(MainTexId, texture);
                block.SetTexture(BaseMapId, texture);
            }

            tileRenderer.SetPropertyBlock(block);
        }

        public void SetState(TileVisualState state) => State = state;

        public void Flash(TileVisualState state, float seconds)
        {
        }

        public void Destroy()
        {
            if (this != null && gameObject != null)
                Object.Destroy(gameObject);
        }
    }
}
