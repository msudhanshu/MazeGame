using System;
using System.Collections.Generic;
using Nixin.Graph.Core;
using Nixin.Memory.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Adapts a hidden graph path into recall prompts: choices are neighboring nodes.
    /// </summary>
    public static class GraphPrompts
    {
        public static TokenId ToToken(GraphNodeId node) => new TokenId(node.Value);

        public static GraphNodeId ToNode(TokenId token)
        {
            if (string.IsNullOrEmpty(token.Value))
                throw new ArgumentException("Token cannot be empty.", nameof(token));

            return new GraphNodeId(token.Value);
        }

        public static IReadOnlyList<Prompt> FromPath(
            GraphPath path,
            bool allowStepBack = true,
            ISet<GraphNodeId> blocked = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var topology = path.Topology;
            var prompts = new List<Prompt>(path.StepCount);

            for (var step = 0; step < path.StepCount; step++)
            {
                var current = path.Nodes[step];
                var next = path.Nodes[step + 1];
                var neighbours = topology.Neighbors(current);

                var choices = new List<TokenId>(neighbours.Count);
                for (var i = 0; i < neighbours.Count; i++)
                {
                    var neighbour = neighbours[i];
                    if (!allowStepBack && step > 0 && neighbour == path.Nodes[step - 1])
                        continue;
                    if (blocked != null && blocked.Contains(neighbour))
                        continue;

                    choices.Add(ToToken(neighbour));
                }

                if (choices.Count < 2)
                {
                    throw new ArgumentException(
                        $"Step {step} at {current.Value} only has {choices.Count} playable neighbour(s).");
                }

                prompts.Add(new Prompt(
                    new PromptId(current.Value),
                    choices,
                    new[] { ToToken(next) }));
            }

            return prompts;
        }
    }
}
