using Game.Unity.Themes;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public sealed class GraphEdgeLine : MonoBehaviour
    {
        const float IdleWidth = 0.08f;
        static readonly Color IdleTint = new Color(0.55f, 0.62f, 0.72f, 0.85f);

        LineRenderer _line;
        Material _material;
        float _baseWidth = IdleWidth;

        public GraphNodeId NodeA { get; private set; }
        public GraphNodeId NodeB { get; private set; }
        public Vector3[] WorldPoints { get; private set; }
        public GraphEdgeVisualState Visual { get; private set; } = GraphEdgeVisualState.Idle;

        public bool Visible => gameObject.activeSelf;

        public void Initialise(GraphNodeId a, GraphNodeId b, Vector3[] worldPoints, LineRenderer line = null)
        {
            NodeA = a;
            NodeB = b;
            WorldPoints = worldPoints ?? System.Array.Empty<Vector3>();
            _line = line != null ? line : GetComponent<LineRenderer>();
            if (_line != null)
            {
                _material = _line.material;
                _baseWidth = _line.startWidth > 0.001f ? _line.startWidth : IdleWidth;
            }

            SetVisual(GraphEdgeVisualState.Idle);
        }

        public bool Connects(GraphNodeId a, GraphNodeId b) =>
            (NodeA == a && NodeB == b) || (NodeA == b && NodeB == a);

        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        public void SetVisual(GraphEdgeVisualState visual)
        {
            Visual = visual;
            Apply(visual, 0f);
        }

        void Update()
        {
            if (Visual == GraphEdgeVisualState.Candidate)
                Apply(Visual, Time.time);
        }

        void Apply(GraphEdgeVisualState visual, float time)
        {
            if (_line == null)
                return;

            Color color;
            var width = _baseWidth;
            switch (visual)
            {
                case GraphEdgeVisualState.Candidate:
                    var wave = Mathf.Abs(Mathf.Sin(time * 5.5f));
                    color = Color.Lerp(DanceFloorPalette.Candidate, DanceFloorPalette.CandidateEdge, 0.55f + 0.4f * wave);
                    color.a = 0.78f + 0.22f * wave;
                    width = _baseWidth * (1.35f + 0.4f * wave);
                    break;
                case GraphEdgeVisualState.Wrong:
                    color = DanceFloorPalette.Wrong;
                    color.a = 0.95f;
                    width = _baseWidth * 1.55f;
                    break;
                default:
                    color = IdleTint;
                    break;
            }

            _line.startWidth = width;
            _line.endWidth = width * 0.85f;
            _line.startColor = color;
            _line.endColor = color;
            if (_material != null)
            {
                _material.color = color;
                if (_material.HasProperty("_BaseColor"))
                    _material.SetColor("_BaseColor", color);
                if (_material.HasProperty("_Color"))
                    _material.SetColor("_Color", color);
            }
        }
    }
}
