using Game.Unity.Data;
using Nixin.Graph.Core;
using UnityEngine;

namespace Game.Unity.Graph
{
    public interface IGraphNodeViewFactory
    {
        string ThemeId { get; }

        IGraphNodeView CreateNode(GraphNodeId nodeId, Vector3 worldPosition, Transform parent);

        void ApplyEnvironment(Camera camera, GraphBoardLayout layout, Transform parent);
    }
}
