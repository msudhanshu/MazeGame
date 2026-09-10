using Nixin.Graph.Core;

namespace Game.Unity.Graph
{
    public interface IGraphNodeView
    {
        GraphNodeId NodeId { get; }
        GraphNodeVisualState State { get; }

        void SetState(GraphNodeVisualState state);
        void SetVisible(bool visible);
        void Destroy();
    }
}
