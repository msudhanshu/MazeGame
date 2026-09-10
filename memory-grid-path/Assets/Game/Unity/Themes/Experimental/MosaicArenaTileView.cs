using Game.Unity.Themes;
using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// One glass tessera. The photograph lives on the backdrop; this pane frosts, stains,
    /// or clears and always draws its own seam.
    /// </summary>
    public sealed class MosaicArenaTileView : MonoBehaviour, ITileView
    {
        static readonly int TintId = Shader.PropertyToID("_Tint");
        static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
        static readonly int GlassAlphaId = Shader.PropertyToID("_GlassAlpha");
        static readonly int FrostId = Shader.PropertyToID("_Frost");
        static readonly int SeamColorId = Shader.PropertyToID("_SeamColor");

        static readonly Color ClearSeam = new Color(0.78f, 0.92f, 1f, 1f);

        Renderer _renderer;
        MaterialPropertyBlock _block;
        float _flashUntil;

        public GridCoord Coord { get; private set; }
        public TileVisualState State { get; private set; } = TileVisualState.Idle;

        public void Initialise(GridCoord coord, Renderer tileRenderer)
        {
            Coord = coord;
            _renderer = tileRenderer;
            _block = new MaterialPropertyBlock();
            Apply(TileVisualState.Idle);
        }

        public void SetState(TileVisualState state)
        {
            State = state;
            if (Time.time >= _flashUntil)
                Apply(state);
        }

        public void Flash(TileVisualState state, float seconds)
        {
            _flashUntil = Time.time + seconds;
            Apply(state);
        }

        public void Destroy()
        {
            if (this != null && gameObject != null)
                Object.Destroy(gameObject);
        }

        void Update()
        {
            if (_flashUntil > 0f)
            {
                if (Time.time < _flashUntil)
                    return;

                _flashUntil = 0f;
                Apply(State);
                return;
            }

            if (State == TileVisualState.Pickup || State == TileVisualState.Wrong)
                Apply(State);
        }

        void Apply(TileVisualState state)
        {
            if (_renderer == null)
                return;

            var look = MosaicGlassLook.For(state);
            if (state == TileVisualState.Pickup)
                look = new MosaicGlassLook(
                    look.Tint,
                    look.TintStrength,
                    look.Frost,
                    look.Alpha + 0.12f * Mathf.Sin(Time.time * 4.2f + Coord.X));
            else if (state == TileVisualState.Wrong)
                look = new MosaicGlassLook(
                    look.Tint,
                    look.TintStrength,
                    look.Frost,
                    0.82f + 0.16f * Mathf.Abs(Mathf.Sin(Time.time * 12f)));

            var seam = Color.Lerp(ClearSeam, look.Tint, look.TintStrength * 0.55f);

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(TintId, look.Tint);
            _block.SetFloat(TintStrengthId, look.TintStrength);
            _block.SetFloat(FrostId, look.Frost);
            _block.SetFloat(GlassAlphaId, look.Alpha);
            _block.SetColor(SeamColorId, seam);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
