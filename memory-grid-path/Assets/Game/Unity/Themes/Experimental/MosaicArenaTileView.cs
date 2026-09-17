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
        TileVisualState _flashState;
        Vector3 _restScale = Vector3.one;
        bool _hasRestScale;

        public GridCoord Coord { get; private set; }
        public TileVisualState State { get; private set; } = TileVisualState.Idle;

        public void Initialise(GridCoord coord, Renderer tileRenderer)
        {
            Coord = coord;
            _renderer = tileRenderer;
            _block = new MaterialPropertyBlock();
            CaptureRestScale();
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
            _flashState = state;
            _flashUntil = Time.time + Mathf.Max(0.01f, seconds);
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
                {
                    Apply(_flashState);
                    return;
                }

                _flashUntil = 0f;
                Apply(State);
                return;
            }

            if (State == TileVisualState.Candidate
                || State == TileVisualState.Pickup
                || State == TileVisualState.Wrong
                || State == TileVisualState.WrongIntense)
                Apply(State);
        }

        void Apply(TileVisualState state)
        {
            if (_renderer == null)
                return;

            CaptureRestScale();

            var look = MosaicGlassLook.For(state);
            var scale = 1f;
            if (state == TileVisualState.Candidate)
            {
                var beat = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 5.5f));
                look = new MosaicGlassLook(
                    DanceFloorPalette.CandidateEdge,
                    0.38f + 0.22f * beat,
                    0.18f + 0.16f * beat,
                    0.28f + 0.16f * beat);
            }
            else if (state == TileVisualState.Pickup)
            {
                look = new MosaicGlassLook(
                    look.Tint,
                    look.TintStrength,
                    look.Frost,
                    look.Alpha + 0.12f * Mathf.Sin(Time.time * 4.2f + Coord.X));
            }
            else if (state == TileVisualState.Wrong || state == TileVisualState.WrongIntense)
            {
                var intense = state == TileVisualState.WrongIntense;
                var beat = Mathf.Abs(Mathf.Sin(Time.time * (intense ? 18f : 14f)));
                var whiteMix = intense ? 0.4f + 0.45f * beat : 0.2f + 0.35f * beat;
                look = new MosaicGlassLook(
                    Color.Lerp(look.Tint, Color.white, whiteMix),
                    1f,
                    look.Frost,
                    intense ? 0.9f + 0.1f * beat : 0.82f + 0.16f * beat);
                scale = 1f + (intense ? 0.12f : 0.07f) * beat;
            }

            transform.localScale = _restScale * scale;

            var seam = Color.Lerp(ClearSeam, look.Tint, look.TintStrength * 0.55f);

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(TintId, look.Tint);
            _block.SetFloat(TintStrengthId, look.TintStrength);
            _block.SetFloat(FrostId, look.Frost);
            _block.SetFloat(GlassAlphaId, look.Alpha);
            _block.SetColor(SeamColorId, seam);
            _renderer.SetPropertyBlock(_block);
        }

        void CaptureRestScale()
        {
            if (_hasRestScale)
                return;
            _restScale = transform.localScale;
            if (_restScale.sqrMagnitude < 0.0001f)
                _restScale = Vector3.one;
            _hasRestScale = true;
        }
    }
}
