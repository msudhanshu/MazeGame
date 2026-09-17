using System.Collections.Generic;
using Nixin.Graph.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// UI-facing move choices. Paths do not self-cross, so already-walked tiles/nodes are
    /// hidden from highlights and arrows. Rules may still keep them as a fallback.
    /// </summary>
    public static class PathOptionFilter
    {
        public static IReadOnlyList<GridCoord> VisibleGridOptions(
            IReadOnlyList<GridCoord> walked,
            IReadOnlyList<GridCoord> options)
        {
            if (options == null || options.Count == 0)
                return System.Array.Empty<GridCoord>();

            var filtered = new List<GridCoord>(options.Count);
            for (var i = 0; i < options.Count; i++)
            {
                if (!Contains(walked, options[i]))
                    filtered.Add(options[i]);
            }

            return filtered;
        }

        public static bool Contains(IReadOnlyList<GridCoord> options, GridCoord target)
        {
            if (options == null)
                return false;

            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] == target)
                    return true;
            }

            return false;
        }

        public static IReadOnlyList<GraphNodeId> VisibleGraphOptions(
            IReadOnlyList<GraphNodeId> walked,
            IReadOnlyList<GraphNodeId> options)
        {
            if (options == null || options.Count == 0)
                return System.Array.Empty<GraphNodeId>();

            var filtered = new List<GraphNodeId>(options.Count);
            for (var i = 0; i < options.Count; i++)
            {
                if (!Contains(walked, options[i]))
                    filtered.Add(options[i]);
            }

            return filtered;
        }

        public static bool Contains(IReadOnlyList<GraphNodeId> options, GraphNodeId target)
        {
            if (options == null)
                return false;

            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] == target)
                    return true;
            }

            return false;
        }
    }
}
