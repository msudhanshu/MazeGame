using System.Collections.Generic;
using Nixin.Graph.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Which authored edges should be drawn: lines from the current node, plus the walked route.
    /// </summary>
    public static class GraphEdgeReveal
    {
        public static bool IsVisible(
            GraphNodeId a,
            GraphNodeId b,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options)
        {
            if (a == current || b == current)
                return Contains(options, a == current ? b : a);

            if (walked == null)
                return false;

            for (var i = 1; i < walked.Count; i++)
            {
                var from = walked[i - 1];
                var to = walked[i];
                if ((from == a && to == b) || (from == b && to == a))
                    return true;
            }

            return false;
        }

        static bool Contains(IReadOnlyList<GraphNodeId> list, GraphNodeId node)
        {
            if (list == null)
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == node)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Nodes that should be drawn: the walked route, the current junction, and legal next moves.
    /// </summary>
    public static class GraphNodeReveal
    {
        public static bool IsVisible(
            GraphNodeId node,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options)
        {
            if (node == current)
                return true;

            if (Contains(walked, node) || Contains(options, node))
                return true;

            return false;
        }

        /// <summary>
        /// Junction spheres for legal next moves only. The walker already marks the
        /// current node, so that circle stays hidden.
        /// </summary>
        public static bool IsCircleVisible(
            GraphNodeId node,
            GraphNodeId current,
            IReadOnlyList<GraphNodeId> options)
        {
            if (node == current)
                return false;

            return Contains(options, node);
        }

        static bool Contains(IReadOnlyList<GraphNodeId> list, GraphNodeId node)
        {
            if (list == null)
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == node)
                    return true;
            }

            return false;
        }
    }
}
