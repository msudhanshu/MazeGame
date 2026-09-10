using Nixin.Ui;

namespace Game.Unity.Ui
{
    public static class JourneyUi
    {
        static bool _registered;

        public static UiNavigator Ensure(
            JourneyHomeScreen home = null,
            JourneyLevelSelectScreen levels = null,
            LevelDetailScreen detail = null,
            SettingsScreen settings = null,
            MemoryPathPopup popup = null)
        {
            MemoryPathUi.EnsureEventSystem();
            var nav = UiNavigator.Ensure();
            MemoryPathUi.ApplyPortraitCanvas(nav);
            if (_registered && UiNavigator.Current != null)
                return nav;

            nav.Register(
                MemoryPathUi.Resolve(home, MemoryPathUi.JourneyHomeResourcePath, JourneyHomeScreen.CreateTemplate),
                UiKind.Screen);
            nav.Register(
                MemoryPathUi.Resolve(levels, MemoryPathUi.JourneyLevelSelectResourcePath, JourneyLevelSelectScreen.CreateTemplate),
                UiKind.Screen);
            nav.Register(
                MemoryPathUi.Resolve(detail, MemoryPathUi.LevelDetailResourcePath, LevelDetailScreen.CreateTemplate),
                UiKind.Screen);
            nav.Register(
                MemoryPathUi.Resolve(settings, MemoryPathUi.SettingsResourcePath, SettingsScreen.CreateTemplate),
                UiKind.Screen);
            nav.Register(MemoryPathUi.ResolvePopup(popup));
            nav.SetDimmerColor(MemoryPathPalette.Dimmer);
            _registered = true;
            return nav;
        }

        public static void HideScreens()
        {
            var nav = UiNavigator.Current;
            if (nav == null)
                return;
            nav.CloseAllPopups();
            nav.Close();
        }
    }
}
