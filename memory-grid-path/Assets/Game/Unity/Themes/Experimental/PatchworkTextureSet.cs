using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// A reusable pool of tile textures for patchwork arenas. Create one asset per theme
    /// (buildings, grass, stone, …) and assign it on <see cref="ArenaVisualSettings"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "PatchworkTextureSet", menuName = "Nixin Studio/Memory Grid Path/Patchwork Texture Set")]
    public sealed class PatchworkTextureSet : ScriptableObject
    {
        [SerializeField] string _themeId = "default";
        [SerializeField] Texture2D[] _textures = System.Array.Empty<Texture2D>();

        public string ThemeId => _themeId;
        public Texture2D[] Textures => _textures ?? System.Array.Empty<Texture2D>();
        public bool HasTextures => Textures.Length > 0;
    }
}
