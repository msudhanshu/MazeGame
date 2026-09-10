using System.Collections.Generic;
using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Merges patchwork texture sets and inline lists from <see cref="ArenaVisualSettings"/>.
    /// </summary>
    public static class PatchworkTexturePool
    {
        public static Texture2D[] Resolve(ArenaVisualSettings settings)
        {
            if (settings == null)
                return System.Array.Empty<Texture2D>();

            var merged = new List<Texture2D>();
            AppendUnique(merged, settings.InlinePatchworkTextures);

            if (settings.PrimaryPatchworkSet != null)
                AppendUnique(merged, settings.PrimaryPatchworkSet.Textures);

            var extraSets = settings.ExtraPatchworkSets;
            if (extraSets != null)
            {
                for (var i = 0; i < extraSets.Length; i++)
                {
                    if (extraSets[i] != null)
                        AppendUnique(merged, extraSets[i].Textures);
                }
            }

            return merged.ToArray();
        }

        static void AppendUnique(List<Texture2D> merged, Texture2D[] textures)
        {
            if (textures == null)
                return;

            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (texture == null || merged.Contains(texture))
                    continue;

                merged.Add(texture);
            }
        }
    }
}
