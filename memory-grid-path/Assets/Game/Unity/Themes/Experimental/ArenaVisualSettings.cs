using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Designer-facing arena look and camera. Classic dance floor is the default; mosaic and
    /// patchwork are opt-in experimental styles.
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaVisualSettings", menuName = "Nixin Studio/Memory Grid Path/Arena Visual Settings")]
    public sealed class ArenaVisualSettings : ScriptableObject
    {
        [Header("Arena style")]
        [SerializeField] ArenaVisualType _visualType = ArenaVisualType.ClassicDanceFloor;

        [Header("Camera")]
        [SerializeField] ArenaCameraMode _cameraMode = ArenaCameraMode.StaticTopDown;
        [Tooltip("Scout Arena only. PanMoveMode keeps north-up follow. RotationMoveMode yaws with the path.")]
        [SerializeField] ScoutMoveMode _scoutMoveMode = ScoutMoveMode.PanMoveMode;
        [Tooltip("Follow-camera zoom. Smaller is closer. 0.65 fills a phone with one tile; 1.6 frames a few tiles.")]
        [Range(0.45f, 6f)]
        [SerializeField] float _followOrthographicSize = 1.8f;
        [SerializeField] float _followSmoothing = 10f;
        [Tooltip("When on, Follow Orthographic Size is used even if Camera Mode is Static Top Down.")]
        [SerializeField] bool _lockOrthographicSize;

        [Header("Classic dance floor")]
        [SerializeField] float _classicTileSize = 1f;
        [Tooltip("Small visible spacing between classic tiles.")]
        [SerializeField] float _classicGap = 0.02f;

        [Header("Seamless arenas (mosaic + patchwork)")]
        [SerializeField] float _seamlessTileSize = 1f;

        [Header("Mosaic image")]
        [SerializeField] Texture2D _mosaicTexture;
        [SerializeField] bool _flipMosaicVertical;
        [Range(0f, 0.06f)]
        [SerializeField] float _mosaicGroutInset;
        [Tooltip("Visible world gap between mosaic tiles. 0 means they touch.")]
        [SerializeField] float _mosaicGap = 0.02f;
        [Tooltip("Built-in star field under the glass when no custom VFX prefab is assigned.")]
        [SerializeField] bool _mosaicBackgroundParticles;
        [Tooltip("Optional VFX prefab under the glass (Occa fireworks copies live in Resources/MosaicVfx/Fireworks).")]
        [SerializeField] GameObject _mosaicVfxPrefab;

        [Header("Patchwork tiles")]
        [Tooltip("Main texture pool used when Visual Type = Patchwork Tiles.")]
        [SerializeField] PatchworkTextureSet _patchworkTextureSet;
        [Tooltip("Optional extra pools merged into the patchwork pick list.")]
        [SerializeField] PatchworkTextureSet[] _extraPatchworkSets = System.Array.Empty<PatchworkTextureSet>();
        [Tooltip("Optional inline textures merged after the sets above.")]
        [SerializeField] Texture2D[] _patchworkTextures = System.Array.Empty<Texture2D>();
        [SerializeField] int _patchworkSeed = 1337;

        public ArenaVisualType VisualType => _visualType;
        public ArenaCameraMode CameraMode => _cameraMode;
        public ScoutMoveMode ScoutMoveMode => _scoutMoveMode;
        public float FollowOrthographicSize => Mathf.Max(0.45f, _followOrthographicSize);
        public float FollowSmoothing => _followSmoothing;
        public bool LockOrthographicSize => _lockOrthographicSize;

        public float ClassicTileSize => _classicTileSize;
        public float ClassicGap => _classicGap;
        public float SeamlessTileSize => _seamlessTileSize;

        public Texture2D MosaicTexture => _mosaicTexture;
        public bool FlipMosaicVertical => _flipMosaicVertical;
        public float MosaicGroutInset => _mosaicGroutInset;
        public float MosaicGap => Mathf.Max(0f, _mosaicGap);
        public bool MosaicBackgroundParticles => _mosaicBackgroundParticles;
        public GameObject MosaicVfxPrefab => _mosaicVfxPrefab;

        public Texture2D[] PatchworkTextures => PatchworkTexturePool.Resolve(this);
        public int PatchworkSeed => _patchworkSeed;

        public PatchworkTextureSet PrimaryPatchworkSet => _patchworkTextureSet;
        public PatchworkTextureSet[] ExtraPatchworkSets => _extraPatchworkSets ?? System.Array.Empty<PatchworkTextureSet>();
        public Texture2D[] InlinePatchworkTextures => _patchworkTextures ?? System.Array.Empty<Texture2D>();

        public bool HasPatchworkTextures => PatchworkTextures.Length > 0;

        public void SetMosaicBackgroundParticles(bool enabled) => _mosaicBackgroundParticles = enabled;

        public void SetMosaicVfxPrefab(GameObject prefab) => _mosaicVfxPrefab = prefab;

        /// <summary>Runtime-only settings for patchwork when no asset is configured (e.g. test lab).</summary>
        public static ArenaVisualSettings CreatePatchworkOverride(Texture2D[] textures, int seed = 1337)
        {
            var settings = CreateInstance<ArenaVisualSettings>();
            settings.ApplyPatchworkOverride(textures, seed);
            return settings;
        }

        public static ArenaVisualSettings CreatePatchworkOverride(PatchworkTextureSet textureSet, int seed = 1337)
        {
            var settings = CreateInstance<ArenaVisualSettings>();
            settings.ApplyPatchworkOverride(textureSet, seed);
            return settings;
        }

        public void ApplyPatchworkOverride(Texture2D[] textures, int seed)
        {
            _visualType = ArenaVisualType.PatchworkTiles;
            _patchworkTextureSet = null;
            _extraPatchworkSets = System.Array.Empty<PatchworkTextureSet>();
            _patchworkTextures = textures ?? System.Array.Empty<Texture2D>();
            _patchworkSeed = seed;
        }

        public void ApplyPatchworkOverride(PatchworkTextureSet textureSet, int seed)
        {
            _visualType = ArenaVisualType.PatchworkTiles;
            _patchworkTextureSet = textureSet;
            _extraPatchworkSets = System.Array.Empty<PatchworkTextureSet>();
            _patchworkTextures = System.Array.Empty<Texture2D>();
            _patchworkSeed = seed;
        }

        public static ArenaVisualSettings CreateClassicOverride(
            ArenaCameraMode cameraMode = ArenaCameraMode.StaticTopDown,
            float followSize = 1.8f,
            float smoothing = 10f,
            bool lockOrthographicSize = false,
            ScoutMoveMode scoutMoveMode = ScoutMoveMode.PanMoveMode)
        {
            var settings = CreateInstance<ArenaVisualSettings>();
            settings._visualType = ArenaVisualType.ClassicDanceFloor;
            settings.ApplyCamera(cameraMode, followSize, smoothing, lockOrthographicSize);
            settings.ApplyScoutMoveMode(scoutMoveMode);
            return settings;
        }

        public static ArenaVisualSettings CreateMosaicOverride(
            Texture2D texture,
            ArenaCameraMode cameraMode = ArenaCameraMode.StaticTopDown,
            GameObject vfxPrefab = null,
            bool backgroundParticles = false)
        {
            var settings = CreateInstance<ArenaVisualSettings>();
            settings._visualType = ArenaVisualType.MosaicImage;
            settings._mosaicTexture = texture;
            settings._mosaicBackgroundParticles = backgroundParticles;
            settings._mosaicVfxPrefab = vfxPrefab;
            settings.ApplyCamera(cameraMode);
            return settings;
        }

        public void ApplyCamera(
            ArenaCameraMode cameraMode,
            float followSize = 1.8f,
            float smoothing = 10f,
            bool lockOrthographicSize = false)
        {
            _cameraMode = cameraMode;
            _followOrthographicSize = Mathf.Max(0.45f, followSize);
            _followSmoothing = Mathf.Max(0.1f, smoothing);
            _lockOrthographicSize = lockOrthographicSize;
        }

        public void ApplyScoutMoveMode(ScoutMoveMode scoutMoveMode) =>
            _scoutMoveMode = scoutMoveMode;

        public float TileSize =>
            _visualType == ArenaVisualType.ClassicDanceFloor ? _classicTileSize : _seamlessTileSize;

        public float TileGap =>
            _visualType == ArenaVisualType.ClassicDanceFloor ? _classicGap
            : _visualType == ArenaVisualType.MosaicImage ? MosaicGap
            : 0f;
    }
}
