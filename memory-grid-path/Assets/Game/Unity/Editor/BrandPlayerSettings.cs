using System;
using Game.Core.Domain;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Unity.Editor
{
    /// <summary>
    /// App name, Android package, launcher icon, and Nixin splash for this title.
    /// </summary>
    public static class BrandPlayerSettings
    {
        public const string AppIconPath = "Assets/Brand/AppIcon.png";
        public const string IconBackgroundPath = "Assets/Brand/IconBackground.png";
        const string SplashPortraitPath = "Packages/com.nixin.boot/Runtime/Unity/Resources/NixinBrand/SplashPortrait.jpg";
        const string SplashLandscapePath = "Packages/com.nixin.boot/Runtime/Unity/Resources/NixinBrand/SplashLandscape.jpg";
        const string LogoPath = "Packages/com.nixin.boot/Runtime/Unity/Resources/NixinBrand/LogoNeon.png";

        [MenuItem("Nixin Studio/Memory Grid Path/Apply Brand (name, icon, splash)")]
        public static void ApplyFromMenu()
        {
            Apply();
            AssetDatabase.SaveAssets();
            Debug.Log("Brand applied: " + AppIdentity.ProductName + " / " + AppIdentity.AndroidPackage);
        }

        public static void Apply()
        {
            PlayerSettings.companyName = "Nixin Studio";
            PlayerSettings.productName = AppIdentity.ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AppIdentity.AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, AppIdentity.AndroidPackage);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, AppIdentity.AndroidPackage);

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(IconBackgroundPath);
            if (icon != null)
                ApplyIcons(icon, background != null ? background : icon);
            else
                Debug.LogWarning("Brand: missing app icon at " + AppIconPath);

            ApplySplash();
            Nixin.Ui.Editor.MobileChromeSettings.Apply();
        }

        static void ApplyIcons(Texture2D icon, Texture2D background)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Application);
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);
            ApplyAndroidAdaptive(icon, background);
        }

        static void ApplyAndroidAdaptive(Texture2D foreground, Texture2D background)
        {
            var kindType = Type.GetType("UnityEditor.Android.AndroidPlatformIconKind, UnityEditor.Android.Extensions");
            var adaptive = kindType != null
                ? kindType.GetProperty("Adaptive")?.GetValue(null) as PlatformIconKind
                : null;
            if (adaptive == null)
            {
                Debug.LogWarning("Brand: Android adaptive icon API not found; legacy icon only.");
                return;
            }

            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
            for (var i = 0; i < icons.Length; i++)
                icons[i].SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptive, icons);
        }

        static void ApplySplash()
        {
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPortraitPath);
            var landscape = AssetDatabase.LoadAssetAtPath<Sprite>(SplashLandscapePath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.blurBackgroundImage = false;
            PlayerSettings.SplashScreen.overlayOpacity = 0.2f;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.03f, 0.06f, 0.16f, 1f);
            if (landscape != null)
                PlayerSettings.SplashScreen.background = landscape;
            if (portrait != null)
                PlayerSettings.SplashScreen.backgroundPortrait = portrait;
            if (logo != null)
                PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, logo) };
        }
    }
}
