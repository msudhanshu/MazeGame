using Game.Unity.View;
using Nixin.Grid.Core;
using UnityEngine;

namespace Game.Unity.Themes
{
    /// <summary>
    /// A single coloured tile: a solid fill with a thin dark border, driven through a
    /// property block so all tiles share one material.
    /// </summary>
    public sealed class DanceFloorTileView : MonoBehaviour, ITileView
    {
        static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int BorderId = Shader.PropertyToID("_Border");

        Renderer _renderer;
        MaterialPropertyBlock _block;
        Color _baseColor;
        float _flashUntil;
        TileVisualState _flashState;
        Vector3 _restScale = Vector3.one;
        bool _hasRestScale;

        public GridCoord Coord { get; private set; }
        public TileVisualState State { get; private set; } = TileVisualState.Idle;

        public void Initialise(GridCoord coord, Color baseColor, Renderer tileRenderer)
        {
            Coord = coord;
            _baseColor = baseColor;
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

            // Idle and unused pickups keep pulsing so they stay readable without a VFX pass.
            if (State == TileVisualState.Idle
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

            var glow = _baseColor;
            var intensity = 1.12f;
            var alpha = 1f;
            var border = 0.028f;
            var edge = DanceFloorPalette.Grout;
            var scale = 1f;

            switch (state)
            {
                case TileVisualState.Idle:
                    intensity = 1.08f + 0.08f * Mathf.Sin(Time.time * 1.4f + Coord.X + Coord.Y * 0.7f);
                    break;

                case TileVisualState.Candidate:
                    glow = Color.Lerp(_baseColor, Color.white, 0.18f);
                    intensity = 1.22f;
                    break;

                case TileVisualState.Walked:
                    glow = DanceFloorPalette.Walked;
                    intensity = 1.18f;
                    break;

                case TileVisualState.Revealed:
                case TileVisualState.Lighthouse:
                    glow = DanceFloorPalette.Walked;
                    intensity = 1.18f;
                    break;

                case TileVisualState.Pickup:
                    glow = DanceFloorPalette.Pickup;
                    intensity = 1.2f + 0.14f * Mathf.Sin(Time.time * 3.2f + Coord.X);
                    break;

                case TileVisualState.Wrong:
                    ApplyWrongLook(false, out glow, out intensity, out border, out edge, out scale);
                    break;

                case TileVisualState.WrongIntense:
                    ApplyWrongLook(true, out glow, out intensity, out border, out edge, out scale);
                    break;

                case TileVisualState.Start:
                    glow = DanceFloorPalette.Start;
                    intensity = 1.2f;
                    break;

                case TileVisualState.Goal:
                    glow = DanceFloorPalette.Goal;
                    intensity = 1.2f;
                    break;

                case TileVisualState.Blocked:
                    glow = DanceFloorPalette.Blocked;
                    intensity = 0.55f;
                    alpha = 1f;
                    break;
            }

            transform.localScale = _restScale * scale;

            _renderer.GetPropertyBlock(_block);
            _block.SetColor(GlowColorId, glow);
            _block.SetColor(EdgeColorId, edge);
            _block.SetFloat(IntensityId, intensity);
            _block.SetFloat(AlphaId, alpha);
            _block.SetFloat(BorderId, border);
            _renderer.SetPropertyBlock(_block);
        }

        static void ApplyWrongLook(
            bool intense,
            out Color glow,
            out float intensity,
            out float border,
            out Color edge,
            out float scale)
        {
            var beat = Mathf.Abs(Mathf.Sin(Time.time * (intense ? 18f : 14f)));
            var flash = Mathf.Exp(-(Time.time % 1f) * (intense ? 8f : 6f));
            var whiteMix = intense
                ? 0.35f + 0.45f * beat
                : 0.18f + 0.35f * beat;
            glow = Color.Lerp(DanceFloorPalette.Wrong, Color.white, whiteMix);
            intensity = (intense ? 2.15f : 1.75f) + (intense ? 0.7f : 0.45f) * beat + flash * 0.35f;
            border = intense ? 0.055f : 0.042f;
            edge = Color.Lerp(DanceFloorPalette.Grout, Color.white, intense ? 0.55f * beat : 0.3f * beat);
            scale = 1f + (intense ? 0.12f : 0.07f) * beat;
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
