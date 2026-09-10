using Game.Core.State;
using Nixin.Game.Core;
using UnityEngine;

namespace Game.Unity.Save
{
    /// <summary>
    /// Local save behind the shared repository interface. The only place in the game that
    /// knows PlayerPrefs exists, so swapping in a cloud store later touches nothing else.
    /// </summary>
    public sealed class PlayerPrefsProgressRepository : IPlayerRepository<PlayerProgress>
    {
        const string Key = "memory-grid-path.progress";

        public PlayerProgress Load()
        {
            return ProgressSaveFormat.Parse(PlayerPrefs.GetString(Key, string.Empty));
        }

        public void Save(PlayerProgress state)
        {
            if (state == null)
                return;

            PlayerPrefs.SetString(Key, ProgressSaveFormat.Serialize(state));
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
