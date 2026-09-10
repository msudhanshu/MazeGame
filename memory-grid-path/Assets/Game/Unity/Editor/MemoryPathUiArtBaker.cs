using System.IO;
using Game.Unity.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Editor
{
    /// <summary>Writes generated UI slices and gradients so prefabs keep their look without runtime restyle.</summary>
    public static class MemoryPathUiArtBaker
    {
        public const string Folder = MemoryPathUiPrefabBuilder.Folder + "/Art";

        public static void Ensure(bool force)
        {
            Directory.CreateDirectory(Folder);
            WriteSlice("SliceRounded", 256, 36, force);
            WriteSlice("SliceCircle", 256, 128, force);
            WriteGradient(
                "GradHomeBg",
                true,
                MemoryPathPalette.MenuTo,
                MemoryPathPalette.MenuMid,
                MemoryPathPalette.MenuFrom,
                force);
            WriteGradient(
                "GradHomePlay",
                true,
                MemoryPathPalette.HomePlayFrom,
                Color.Lerp(MemoryPathPalette.HomePlayFrom, MemoryPathPalette.HomePlayTo, 0.5f),
                MemoryPathPalette.HomePlayTo,
                force);
            WriteGradient(
                "GradSelectBg",
                false,
                MemoryPathPalette.SelectTo,
                MemoryPathPalette.SelectMid,
                MemoryPathPalette.SelectFrom,
                force);
            WriteGradient(
                "GradSettingsBg",
                false,
                MemoryPathPalette.SelectTo,
                MemoryPathPalette.SelectMid,
                MemoryPathPalette.SettingsFrom,
                force);
            WriteGradient(
                "GradDetailBg",
                false,
                MemoryPathPalette.DetailTo,
                MemoryPathPalette.DetailMid,
                MemoryPathPalette.DetailFrom,
                force);
            WriteGradient(
                "GradDetailBanner",
                false,
                MemoryPathPalette.BannerFrom,
                MemoryPathPalette.BannerVia,
                MemoryPathPalette.BannerTo,
                force);
            WriteGradient(
                "GradDetailStart",
                false,
                MemoryPathPalette.BannerTo,
                Color.Lerp(MemoryPathPalette.BannerTo, MemoryPathPalette.BannerVia, 0.5f),
                MemoryPathPalette.BannerVia,
                force);
            AssetDatabase.SaveAssets();
            UiDraw.ClearGeneratedCache();
        }

        public static void Persist(GameObject root)
        {
            if (root == null)
                return;

            var rounded = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath("SliceRounded"));
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath("SliceCircle"));
            var images = root.GetComponentsInChildren<Image>(true);
            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];
                if (image == null || !IsTransient(image.sprite))
                    continue;

                image.sprite = UiDraw.UsesCircleSprite(image.gameObject.name) ? circle : rounded;
                if (image.sprite != null)
                    image.type = Image.Type.Sliced;
            }

            var raws = root.GetComponentsInChildren<RawImage>(true);
            for (var i = 0; i < raws.Length; i++)
            {
                var raw = raws[i];
                if (raw == null || !IsTransient(raw.texture))
                    continue;

                var saved = PersistTexture(raw.texture, TexturePath(root, raw));
                if (saved != null)
                    raw.texture = saved;
            }
        }

        static void WriteSlice(string name, int size, int radius, bool force)
        {
            var path = SpritePath(name);
            if (!force && File.Exists(path))
                return;

            var sprite = UiDraw.MakeSlicedSprite(size, radius);
            File.WriteAllBytes(path, sprite.texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = new Vector4(radius, radius, radius, radius);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void WriteGradient(string name, bool vertical, Color a, Color b, Color c, bool force)
        {
            var path = Folder + "/" + name + ".png";
            if (!force && File.Exists(path))
                return;

            var tex = vertical ? UiDraw.VerticalGradient(a, b, c) : UiDraw.HorizontalGradient(a, b, c);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Texture2D PersistTexture(Texture texture, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
                return existing;

            var src = texture as Texture2D;
            if (src == null || !src.isReadable)
                return src;

            File.WriteAllBytes(path, src.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static bool IsTransient(UnityEngine.Object obj)
        {
            if (obj == null)
                return true;
            if ((obj.hideFlags & HideFlags.DontSave) != 0)
                return true;
            return string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
        }

        static string SpritePath(string name) => Folder + "/" + name + ".png";

        static string TexturePath(GameObject root, RawImage raw)
        {
            var relative = raw.transform == root.transform
                ? root.name
                : raw.transform.name;
            var current = raw.transform;
            while (current.parent != null && current.parent != root.transform)
            {
                current = current.parent;
                relative = current.name + "_" + relative;
            }

            return Folder + "/" + root.name + "_" + relative + ".png";
        }
    }
}
