using Game.Core.State;
using Nixin.Game.Core;
using UnityEngine;

namespace Game.Unity.Save
{
    /// <summary>
    /// Journey Hub save. Separate key from <see cref="PlayerPrefsProgressRepository"/> so the
    /// original GridPathPlay scene keeps its own progress.
    /// </summary>
    public sealed class PlayerPrefsJourneyRepository : IPlayerRepository<JourneyProgress>
    {
        public const string Key = "memory-grid-path.journey";

        public JourneyProgress Load()
        {
            return JourneySaveFormat.Parse(PlayerPrefs.GetString(Key, string.Empty));
        }

        public void Save(JourneyProgress state)
        {
            if (state == null)
                return;

            PlayerPrefs.SetString(Key, JourneySaveFormat.Serialize(state));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
