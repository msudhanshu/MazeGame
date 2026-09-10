using System.Collections.Generic;
using Nixin.Graph.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// UI-facing move choices. The rules may keep the immediate parent as a hidden fallback
    /// choice, but the board should not highlight or invite the player to go straight back.
    /// </summary>
    public static class PathOptionFilter
    {
        public static IReadOnlyList<GridCoord> VisibleGridOptions(
            IReadOnlyList<GridCoord> walked,
            IReadOnlyList<GridCoord> options)
        {
            if (options == null || options.Count == 0)
                return System.Array.Empty<GridCoord>();
            if (walked == null || walked.Count < 2)
                return options;

            var parent = walked[walked.Count - 2];
            var filtered = new List<GridCoord>(options.Count);
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] != parent)
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
            if (walked == null || walked.Count < 2)
                return options;

            var parent = walked[walked.Count - 2];
            var filtered = new List<GraphNodeId>(options.Count);
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] != parent)
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
