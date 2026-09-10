using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Game.Unity.Ui
{
    /// <summary>
    /// Color emoji for TMP. Unity 6 Essential Resources no longer ship EmojiOne.
    /// A serialized ColorEmoji.asset created via CreateFontAsset without atlas
    /// sub-assets leaves m_AtlasTextures as a destroyed/null reference and
    /// TextMeshProUGUI.Rebuild throws MissingReferenceException.
    /// Keep a runtime DynamicOS font with live atlas textures instead.
    /// </summary>
    public static class TmpEmojiFallback
    {
        static readonly string[] ColorEmojiPaths =
        {
            "/System/Library/Fonts/Apple Color Emoji.ttc",
            "/System/Library/Fonts/Supplemental/Apple Color Emoji.ttc",
            "/system/fonts/NotoColorEmoji.ttf",
            "/system/fonts/NotoColorEmojiFlags.ttf",
            "/system/fonts/SamsungColorEmoji.ttf"
        };

        static TMP_FontAsset _runtime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Ensure();
        }

        public static void Ensure()
        {
            StripBrokenFallbacks();
            if (HasLiveAtlas(_runtime))
            {
                Wire(_runtime);
                return;
            }

            var path = FirstExisting(ColorEmojiPaths);
            if (string.IsNullOrEmpty(path))
                return;

            TMP_FontAsset created;
            try
            {
                created = TMP_FontAsset.CreateFontAsset(
                    path,
                    0,
                    90,
                    9,
                    GlyphRenderMode.COLOR,
                    512,
                    512);
            }
            catch
            {
                return;
            }

            if (!HasLiveAtlas(created))
                return;

            created.name = "ColorEmojiRuntime";
            created.hideFlags = HideFlags.HideAndDontSave;
            created.atlasPopulationMode = AtlasPopulationMode.DynamicOS;
            Hide(created.material);
            var textures = created.atlasTextures;
            for (var i = 0; i < textures.Length; i++)
                Hide(textures[i]);

            _runtime = created;
            Wire(_runtime);
        }

        public static bool HasLiveAtlas(TMP_FontAsset asset)
        {
            if (asset == null)
                return false;

            var textures = asset.atlasTextures;
            if (textures == null || textures.Length == 0)
                return false;

            for (var i = 0; i < textures.Length; i++)
            {
                if (textures[i] == null)
                    return false;
            }

            return true;
        }

        public static void StripBrokenFallbacks()
        {
            if (TMP_Settings.instance == null)
                return;

            var list = TMP_Settings.emojiFallbackTextAssets;
            if (list == null || list.Count == 0)
                return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                var font = item as TMP_FontAsset;
                if (font != null && !HasLiveAtlas(font))
                    list.RemoveAt(i);
            }
        }

        static void Wire(TMP_FontAsset emoji)
        {
            if (TMP_Settings.instance == null || emoji == null)
                return;

            var list = TMP_Settings.emojiFallbackTextAssets;
            if (list == null)
            {
                list = new List<TMP_Asset>();
                TMP_Settings.emojiFallbackTextAssets = list;
            }

            if (!list.Contains(emoji))
                list.Add(emoji);
        }

        static void Hide(Object obj)
        {
            if (obj != null)
                obj.hideFlags = HideFlags.HideAndDontSave;
        }

        static string FirstExisting(string[] paths)
        {
            for (var i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i]))
                    return paths[i];
            }

            return null;
        }
    }
}
