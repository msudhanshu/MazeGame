using System;
using Nixin.Game.Core;
using Nixin.Grid.Core;

namespace Game.Core.Domain
{
    /// <summary>
    /// Produces the hidden route for a level. The level fixes the difficulty; the seed fixes
    /// the route, so a stored seed replays a session exactly while a new seed gives a path of
    /// the same shape that the player has not seen.
    /// </summary>
    public sealed class LevelPathFactory
    {
        readonly LevelCatalog _catalog;

        public LevelPathFactory(LevelCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public GridPath Create(int levelNumber, int seed) =>
            Create(levelNumber, new XorShiftRandom(seed));

        public GridPath Create(int levelNumber, IRandomSource random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var level = _catalog.Get(levelNumber);
            return SelfAvoidingPathGenerator.Generate(level.Size, level.Start, level.Goal, level.Shape, random);
        }
    }
}
