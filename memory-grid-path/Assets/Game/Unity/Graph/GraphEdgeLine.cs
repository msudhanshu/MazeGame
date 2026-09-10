using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public sealed class GraphEdgeLine : MonoBehaviour
    {
        public GraphNodeId NodeA { get; private set; }
        public GraphNodeId NodeB { get; private set; }
        public Vector3[] WorldPoints { get; private set; }

        public bool Visible => gameObject.activeSelf;

        public void Initialise(GraphNodeId a, GraphNodeId b, Vector3[] worldPoints)
        {
            NodeA = a;
            NodeB = b;
            WorldPoints = worldPoints ?? System.Array.Empty<Vector3>();
        }

        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
