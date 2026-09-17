using Game.Unity.Input;
using Game.Unity.View;
using UnityEngine;

namespace Game.Unity.Graph
{
    /// <summary>
    /// Keeps a clamped graph camera framing and applies player pan/zoom input.
    /// </summary>
    public sealed class GraphViewportController
    {
        readonly GraphViewportInput _input = new GraphViewportInput();

        BoardViewport.Framing _framing;
        BoardViewport.Limits _limits;
        bool _active;

        public bool IsActive => _active;
        public bool BlocksNodePick => _active && _input.BlocksNodePick;
        public bool DidZoom => _active && _input.DidZoom;
        public bool DidPan => _active && _input.DidPan;
        public BoardViewport.Framing Framing => _framing;
        public BoardViewport.Limits Limits => _limits;

        public void Bind(GraphBoardLayout layout, float aspect, float topViewportInset = 0f)
        {
            if (layout == null)
            {
                _active = false;
                return;
            }

            _limits = BoardViewport.ComputeLimits(
                layout.Origin,
                layout.WorldWidth,
                layout.WorldDepth,
                aspect,
                topViewportInset: topViewportInset);
            _framing = BoardViewport.DefaultFraming(_limits);
            _active = true;
        }

        public void Clear() => _active = false;

        public void Reset(GraphBoardLayout layout, float aspect, float topViewportInset = 0f) =>
            Bind(layout, aspect, topViewportInset);

        public void RefreshLimits(GraphBoardLayout layout, float aspect, float topViewportInset)
        {
            if (!_active || layout == null)
                return;

            _limits = BoardViewport.ComputeLimits(
                layout.Origin,
                layout.WorldWidth,
                layout.WorldDepth,
                aspect,
                topViewportInset: topViewportInset);
            _framing = BoardViewport.Clamp(_framing, _limits, aspect);
        }

        public void UpdateInput(Camera camera, GraphBoardLayout layout, bool allowInput)
        {
            if (!_active || camera == null || layout == null)
                return;

            _input.TryUpdate(camera, layout, ref _framing, _limits, allowInput);
        }

        public void Apply(Camera camera)
        {
            if (!_active || camera == null)
                return;

            BoardCamera.FrameFollow(camera, _framing.Focus, _framing.OrthographicSize);
        }
    }
}
