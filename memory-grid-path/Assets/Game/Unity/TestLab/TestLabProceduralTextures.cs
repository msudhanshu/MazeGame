using UnityEngine;

namespace Game.Unity.TestLab
{
    /// <summary>
    /// Procedural textures for test-lab scenarios when no designer asset is assigned.
    /// </summary>
    public static class TestLabProceduralTextures
    {
        public static Texture2D CreateCoordinateGridTexture(int width, int height, int pixelsPerCell = 64)
        {
            var texWidth = Mathf.Max(1, width * pixelsPerCell);
            var texHeight = Mathf.Max(1, height * pixelsPerCell);
            var texture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                name = "TestLab_CoordinateGrid",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[texWidth * texHeight];
            for (var y = 0; y < texHeight; y++)
            {
                for (var x = 0; x < texWidth; x++)
                {
                    var cellX = x / pixelsPerCell;
                    var cellY = y / pixelsPerCell;
                    var hue = ((cellX * 17) + (cellY * 53)) % 360 / 360f;
                    var rgb = Color.HSVToRGB(hue, 0.55f, 0.85f);

                    var edge = x % pixelsPerCell == 0
                               || y % pixelsPerCell == 0
                               || x % pixelsPerCell == pixelsPerCell - 1
                               || y % pixelsPerCell == pixelsPerCell - 1;
                    if (edge)
                        rgb *= 0.35f;

                    pixels[y * texWidth + x] = rgb;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        public static Texture2D[] CreatePatchworkSet(int count = 6, int pixelSize = 64)
        {
            var textures = new Texture2D[count];
            for (var i = 0; i < count; i++)
            {
                var texture = new Texture2D(pixelSize, pixelSize, TextureFormat.RGBA32, false)
                {
                    name = "TestLab_Patch_" + i,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                var hue = i / (float)count;
                var baseColor = Color.HSVToRGB(hue, 0.45f, 0.9f);
                var accent = Color.HSVToRGB((hue + 0.12f) % 1f, 0.35f, 0.75f);
                var pixels = new Color32[pixelSize * pixelSize];

                for (var y = 0; y < pixelSize; y++)
                {
                    for (var x = 0; x < pixelSize; x++)
                    {
                        var checker = ((x / 8) + (y / 8)) % 2 == 0;
                        pixels[y * pixelSize + x] = checker ? baseColor : accent;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                textures[i] = texture;
            }

            return textures;
        }
    }
}
