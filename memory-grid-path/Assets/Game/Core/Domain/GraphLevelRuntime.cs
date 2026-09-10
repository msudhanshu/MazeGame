using Nixin.Game.Core;
using Nixin.Graph.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>Builds a runtime graph path from topology + shape + seed.</summary>
    public static class GraphLevelRuntime
    {
        public static GraphPath CreatePath(
            GraphTopology topology,
            GraphNodeId start,
            GraphNodeId goal,
            PathShapeSpec shape,
            int seed) =>
            GraphPathGenerator.Generate(topology, start, goal, shape, new XorShiftRandom(seed));
    }
}
