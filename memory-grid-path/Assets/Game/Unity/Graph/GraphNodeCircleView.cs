using Game.Unity.Themes;
using Game.Unity.View;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public sealed class GraphNodeCircleView : MonoBehaviour, IGraphNodeView
    {
        Renderer _renderer;
        Color _base = new Color(0.25f, 0.3f, 0.38f, 0.45f);

        public GraphNodeId NodeId { get; private set; }
        public GraphNodeVisualState State { get; private set; } = GraphNodeVisualState.Idle;
        public bool Visible => gameObject.activeSelf;

        public void Initialise(GraphNodeId nodeId, Renderer renderer)
        {
            NodeId = nodeId;
            _renderer = renderer;
            Apply(GraphNodeVisualState.Idle);
        }

        public void SetState(GraphNodeVisualState state)
        {
            State = state;
            Apply(state);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void Destroy()
        {
            if (this != null && gameObject != null)
                Object.Destroy(gameObject);
        }

        void Apply(GraphNodeVisualState state)
        {
            if (_renderer == null || _renderer.sharedMaterial == null)
                return;

            var color = _base;
            switch (state)
            {
                case GraphNodeVisualState.Candidate:
                    color = WithAlpha(DanceFloorPalette.Candidate, 0.7f);
                    break;
                case GraphNodeVisualState.Walked:
                    color = GridPathOverlay.DotTint;
                    break;
                case GraphNodeVisualState.Start:
                    color = WithAlpha(DanceFloorPalette.Start, 0.7f);
                    break;
                case GraphNodeVisualState.Goal:
                    color = WithAlpha(DanceFloorPalette.Goal, 0.75f);
                    break;
                case GraphNodeVisualState.Revealed:
                    color = WithAlpha(DanceFloorPalette.Revealed, 0.55f);
                    break;
                case GraphNodeVisualState.Wrong:
                    color = WithAlpha(DanceFloorPalette.Wrong, 0.95f);
                    break;
            }

            _renderer.sharedMaterial.SetColor("_BaseColor", color);
            _renderer.sharedMaterial.SetColor("_Color", color);
            if (_renderer.sharedMaterial.HasProperty("_EmissionColor"))
                _renderer.sharedMaterial.SetColor("_EmissionColor", color * 0.2f);
        }

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
