using System;
using Nixin.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Unity.Ui
{
    public static class MemoryPathUi
    {
        public const string ResourceFolder = "Ui/";
        public const string PopupResourcePath = ResourceFolder + "MemoryPathPopup";
        public const string HomeResourcePath = ResourceFolder + "HomeScreen";
        public const string LevelSelectResourcePath = ResourceFolder + "LevelSelectScreen";
        public const string LevelDetailResourcePath = ResourceFolder + "LevelDetailScreen";
        public const string SettingsResourcePath = ResourceFolder + "SettingsScreen";
        public const string JourneyHomeResourcePath = ResourceFolder + "JourneyHomeScreen";
        public const string JourneyLevelSelectResourcePath = ResourceFolder + "JourneyLevelSelectScreen";
        public const string HudResourcePath = ResourceFolder + "GridPathHud";

        static bool _registered;

        public static UiNavigator Ensure(
            HomeScreen home = null,
            LevelSelectScreen levels = null,
            LevelDetailScreen detail = null,
            SettingsScreen settings = null,
            MemoryPathPopup popup = null)
        {
            EnsureEventSystem();
            var nav = UiNavigator.Ensure();
            ApplyPortraitCanvas(nav);
            if (_registered && UiNavigator.Current != null)
                return nav;

            nav.Register(Resolve(home, HomeResourcePath, HomeScreen.CreateTemplate), UiKind.Screen);
            nav.Register(Resolve(levels, LevelSelectResourcePath, LevelSelectScreen.CreateTemplate), UiKind.Screen);
            nav.Register(Resolve(detail, LevelDetailResourcePath, LevelDetailScreen.CreateTemplate), UiKind.Screen);
            nav.Register(Resolve(settings, SettingsResourcePath, SettingsScreen.CreateTemplate), UiKind.Screen);
            nav.Register(ResolvePopup(popup));
            nav.SetDimmerColor(MemoryPathPalette.Dimmer);
            _registered = true;
            return nav;
        }

        public static MemoryPathPopup ResolvePopup(MemoryPathPopup hint = null)
        {
            return Resolve(hint, PopupResourcePath, MemoryPathPopup.CreateTemplate);
        }

        public static T Resolve<T>(T hint, string resourcePath, Func<T> create) where T : Component
        {
            if (hint != null)
                return hint;

            var inScene = FindScene<T>();
            if (inScene != null)
                return inScene;

            var prefab = Resources.Load<T>(resourcePath);
            if (prefab != null)
                return prefab;

            return create();
        }

        public static T FindScene<T>() where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].gameObject.scene.IsValid())
                    return found[i];
            }

            return null;
        }

        public static void HideScreens()
        {
            var nav = UiNavigator.Current;
            if (nav == null)
                return;
            nav.CloseAllPopups();
            nav.Close();
        }

        public static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existing == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                go.GetComponent<InputSystemUIInputModule>().pointerBehavior = UIPointerBehavior.SingleUnifiedPointer;
                if (Application.isPlaying)
                    UnityEngine.Object.DontDestroyOnLoad(go);
                return;
            }

            var legacy = existing.GetComponent<StandaloneInputModule>();
            if (legacy != null)
                UnityEngine.Object.DestroyImmediate(legacy);

            var module = existing.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = existing.gameObject.AddComponent<InputSystemUIInputModule>();
            module.pointerBehavior = UIPointerBehavior.SingleUnifiedPointer;
        }

        public static void ApplyPortraitCanvas(UiNavigator nav)
        {
            if (nav == null)
                return;

            var scaler = nav.GetComponentInChildren<CanvasScaler>(true);
            if (scaler == null)
                return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }
    }
}
