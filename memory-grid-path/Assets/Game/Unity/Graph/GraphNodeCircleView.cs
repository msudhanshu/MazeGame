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
        Vector3 _restScale = Vector3.one;
        bool _hasRestScale;
        float _flashUntil;
        GraphNodeVisualState _flashState;

        public GraphNodeId NodeId { get; private set; }
        public GraphNodeVisualState State { get; private set; } = GraphNodeVisualState.Idle;
        public bool Visible => gameObject.activeSelf;

        public void Initialise(GraphNodeId nodeId, Renderer renderer)
        {
            NodeId = nodeId;
            _renderer = renderer;
            CaptureRestScale();
            Apply(GraphNodeVisualState.Idle);
        }

        public void SetState(GraphNodeVisualState state)
        {
            State = state;
            if (Time.time >= _flashUntil)
                Apply(state);
        }

        public void Flash(GraphNodeVisualState state, float seconds)
        {
            _flashState = state;
            _flashUntil = Time.time + Mathf.Max(0.01f, seconds);
            Apply(state);
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

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

            if (State == GraphNodeVisualState.Candidate || State == GraphNodeVisualState.Wrong)
                Apply(State);
        }

        void Apply(GraphNodeVisualState state)
        {
            if (_renderer == null || _renderer.sharedMaterial == null)
                return;

            CaptureRestScale();

            var color = _base;
            var scale = 1f;
            switch (state)
            {
                case GraphNodeVisualState.Candidate:
                    var wave = Mathf.Abs(Mathf.Sin(Time.time * 5.5f));
                    color = Color.Lerp(DanceFloorPalette.Candidate, DanceFloorPalette.CandidateEdge, 0.45f + 0.5f * wave);
                    color.a = 0.72f + 0.28f * wave;
                    scale = 1.08f + 0.14f * wave;
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
                    color = WithAlpha(DanceFloorPalette.Revealed, 0.85f);
                    scale = 1.18f;
                    break;
                case GraphNodeVisualState.Wrong:
                    color = WithAlpha(DanceFloorPalette.Wrong, 0.95f);
                    scale = 1.22f;
                    break;
            }

            var material = Application.isPlaying ? _renderer.material : _renderer.sharedMaterial;
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * 0.2f);

            transform.localScale = _restScale * scale;
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

        static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
