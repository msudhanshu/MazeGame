using System;
using System.Collections.Generic;

namespace Game.Core.Memory
{
    public sealed class MemoryLevelSpec
    {
        public MemoryLevelSpec(
            string levelId,
            IReadOnlyList<string> enabledGenreIds,
            int maxSameEntryPerWalk = 1,
            int maxSameGenrePerWalk = 0,
            bool avoidPreviousGenres = true,
            int photoWeight = 3,
            int reliefWeight = 1,
            int object3dWeight = 1,
            bool decorateOppositeFaces = true,
            float oppositeFaceChance = 1f)
        {
            if (string.IsNullOrEmpty(levelId))
                throw new ArgumentException("Level id is required.", nameof(levelId));
            if (maxSameEntryPerWalk < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSameEntryPerWalk));
            if (maxSameGenrePerWalk < 0)
                throw new ArgumentOutOfRangeException(nameof(maxSameGenrePerWalk));
            if (photoWeight < 0 || reliefWeight < 0 || object3dWeight < 0)
                throw new ArgumentOutOfRangeException("Display weights must be non-negative.");
            if (oppositeFaceChance < 0f || oppositeFaceChance > 1f)
                throw new ArgumentOutOfRangeException(nameof(oppositeFaceChance));

            LevelId = levelId;
            EnabledGenreIds = enabledGenreIds ?? Array.Empty<string>();
            MaxSameEntryPerWalk = maxSameEntryPerWalk;
            MaxSameGenrePerWalk = maxSameGenrePerWalk;
            AvoidPreviousGenres = avoidPreviousGenres;
            PhotoWeight = photoWeight;
            ReliefWeight = reliefWeight;
            Object3dWeight = object3dWeight;
            DecorateOppositeFaces = decorateOppositeFaces;
            OppositeFaceChance = oppositeFaceChance;
        }

        public string LevelId { get; }
        public IReadOnlyList<string> EnabledGenreIds { get; }
        public int MaxSameEntryPerWalk { get; }
        public int MaxSameGenrePerWalk { get; }
        public bool AvoidPreviousGenres { get; }
        public int PhotoWeight { get; }
        public int ReliefWeight { get; }
        public int Object3dWeight { get; }
        public bool DecorateOppositeFaces { get; }
        public float OppositeFaceChance { get; }
    }
}
