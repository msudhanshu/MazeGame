using UnityEngine;

namespace Game.Unity.Ui
{
    /// <summary>Local toggles for the settings screen. Sound Effects gates MemoryPathAudio.</summary>
    public static class PlayerSettingsStore
    {
        const string SfxKey = "memory-grid-path.sfx";
        const string MusicKey = "memory-grid-path.music";
        const string HapticsKey = "memory-grid-path.haptics";
        const string PathDragKey = "memory-grid-path.path-drag";
        const string ThemeKey = "memory-grid-path.theme";
        const string TutorialSkipKey = "memory-grid-path.fue.tutorial.skips";

        public const string OceanTheme = "ocean";

        public static bool SoundEffects
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set => SetFlag(SfxKey, value);
        }

        public static bool Music
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => SetFlag(MusicKey, value);
        }

        public static bool Haptics
        {
            get => PlayerPrefs.GetInt(HapticsKey, 0) == 1;
            set => SetFlag(HapticsKey, value);
        }

        public static bool PathDrag
        {
            get => PlayerPrefs.GetInt(PathDragKey, 1) == 1;
            set => SetFlag(PathDragKey, value);
        }

        public static int TutorialSkipCount => PlayerPrefs.GetInt(TutorialSkipKey, 0);

        public static int RecordTutorialSkip()
        {
            var next = TutorialSkipCount + 1;
            PlayerPrefs.SetInt(TutorialSkipKey, next);
            PlayerPrefs.Save();
            return next;
        }

        public static string ThemeId
        {
            get
            {
                var value = PlayerPrefs.GetString(ThemeKey, OceanTheme);
                return string.IsNullOrEmpty(value) ? OceanTheme : value;
            }
            set
            {
                PlayerPrefs.SetString(ThemeKey, value ?? OceanTheme);
                PlayerPrefs.Save();
            }
        }

        static void SetFlag(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
