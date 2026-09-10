using System;
using Game.Core.Memory;
using UnityEngine;

namespace Game.Unity.Memory
{
    [CreateAssetMenu(fileName = "MemoryLevelProfile", menuName = "Nixin Studio/Maze Arena/Memory Level Profile")]
    public sealed class MemoryLevelProfile : ScriptableObject
    {
        [SerializeField] string levelId = "level-1";
        [SerializeField] string[] enabledGenreIds = { "nature", "landmarks", "objects", "painting", "art" };
        [SerializeField] int maxSameEntryPerWalk = 1;
        [SerializeField] int maxSameGenrePerWalk;
        [SerializeField] bool avoidPreviousGenres = true;
        [SerializeField] int photoWeight = 3;
        [SerializeField] int reliefWeight = 1;
        [SerializeField] int object3dWeight = 1;
        [SerializeField] bool decorateOppositeFaces = true;
        [Range(0f, 1f)] [SerializeField] float oppositeFaceChance = 1f;

        public bool DecorateOppositeFaces
        {
            get => decorateOppositeFaces;
            set => decorateOppositeFaces = value;
        }

        public void Configure(string id, string[] genreIds)
        {
            levelId = id;
            enabledGenreIds = genreIds ?? Array.Empty<string>();
        }

        public MemoryLevelSpec ToSpec()
        {
            return new MemoryLevelSpec(
                levelId,
                enabledGenreIds,
                maxSameEntryPerWalk,
                maxSameGenrePerWalk,
                avoidPreviousGenres,
                photoWeight,
                reliefWeight,
                object3dWeight,
                decorateOppositeFaces,
                oppositeFaceChance);
        }
    }
}
