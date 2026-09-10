using System;
using System.Collections.Generic;
using Nixin.Grid.Core;
using Nixin.Memory.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Adapts a hidden path into the recall prompts the shared memory kernel understands:
    /// one prompt per step, where the choices are the neighbouring tiles and exactly one of
    /// them continues the path.
    /// </summary>
    public static class GridPrompts
    {
        public static TokenId ToToken(GridCoord coord) => new TokenId(coord.ToString());

        public static GridCoord ToCoord(TokenId token)
        {
            if (!GridCoord.TryParse(token.Value, out var coord))
                throw new ArgumentException($"Token '{token.Value}' is not a grid coordinate.", nameof(token));

            return coord;
        }

        /// <param name="allowStepBack">
        /// When false the tile the player just came from is not offered. Leave this true for
        /// play: a corner cell only has two neighbours, and hiding the previous one leaves a
        /// single choice, which the recall kernel rejects.
        /// </param>
        /// <param name="blocked">
        /// Off-path tiles greyed out as hints. They are never offered as choices.
        /// </param>
        public static IReadOnlyList<Prompt> FromPath(
            GridPath path,
            bool allowStepBack = true,
            ISet<GridCoord> blocked = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var prompts = new List<Prompt>(path.StepCount);
            for (var step = 0; step < path.StepCount; step++)
            {
                var current = path.Cells[step];
                var next = path.Cells[step + 1];
                var neighbours = GridNeighbors.Of(path.Size, current, path.AllowDiagonal);

                var choices = new List<TokenId>(neighbours.Count);
                for (var i = 0; i < neighbours.Count; i++)
                {
                    var neighbour = neighbours[i];
                    if (!allowStepBack && step > 0 && neighbour == path.Cells[step - 1])
                        continue;
                    if (blocked != null && blocked.Contains(neighbour))
                        continue;

                    choices.Add(ToToken(neighbour));
                }

                if (choices.Count < 2)
                {
                    throw new ArgumentException(
                        $"Step {step} at {current} only has {choices.Count} playable neighbour(s); blocked hints are too aggressive.");
                }

                // The path is self-avoiding, so the cell itself is a unique prompt id.
                prompts.Add(new Prompt(
                    new PromptId(current.ToString()),
                    choices,
                    new[] { ToToken(next) }));
            }

            return prompts;
        }
    }
}
