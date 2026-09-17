using System;
using Game.Core.Domain;
using Game.Core.State;
using Game.Unity.Themes.Experimental;
using UnityEngine;

namespace Game.Unity.Data
{
    public enum JourneyBoardKind
    {
        Grid = 0,
        Graph = 1
    }

    [Serializable]
    public sealed class JourneyLevelEntry
    {
        public JourneyBoardKind Kind = JourneyBoardKind.Grid;
        public ArenaVisualType VisualType = ArenaVisualType.ClassicDanceFloor;
        public LevelRow Grid;
        public GraphLevelDefinition GraphLevel;
        public Texture2D MosaicTexture;
        [Tooltip("Mosaic only. Built-in star field under the glass when no custom VFX prefab is set.")]
        public bool MosaicBackgroundParticles;
        [Tooltip("Mosaic only. Drag a firework / particle prefab. Leave empty for the built-in field when particles are on.")]
        public GameObject MosaicVfxPrefab;
        public PatchworkTextureSet PatchworkSet;
        public Sprite Thumbnail;
        public Texture2D ThumbnailTexture;
        [Tooltip("0 = use the mode Follow Orthographic Size. Set 0.45–6 to override this level. Smaller is closer.")]
        public float FollowOrthographicSize;
        [Tooltip("0 = use the mode Walker Scale. Set above 0 to override this level. 1 is full size.")]
        public float WalkerScale;

        public bool IsGrid => Kind == JourneyBoardKind.Grid;
        public bool IsGraph => Kind == JourneyBoardKind.Graph;

        public LevelSpec GridSpec => Grid.ToSpec();

        public Texture ThumbnailSource
        {
            get
            {
                if (Thumbnail != null)
                    return Thumbnail.texture;
                if (ThumbnailTexture != null)
                    return ThumbnailTexture;
                if (MosaicTexture != null)
                    return MosaicTexture;
                if (GraphLevel != null && GraphLevel.Background != null)
                    return GraphLevel.Background;
                return null;
            }
        }
    }

    [Serializable]
    public sealed class JourneyModeDefinition
    {
        public GameModeId ModeId;
        public string DisplayName = "Arena";
        public Sprite Icon;
        public ArenaCameraMode CameraMode = ArenaCameraMode.StaticTopDown;
        [Tooltip("Scout Arena only. PanMoveMode is the existing north-up follow camera. RotationMoveMode yaws the camera and avatar with the path.")]
        public ScoutMoveMode ScoutMoveMode = ScoutMoveMode.PanMoveMode;
        [Tooltip("Follow-camera zoom when Camera Mode is Follow Walker. Smaller is closer.")]
        [Range(0.45f, 6f)]
        public float FollowOrthographicSize = 1.8f;
        [Tooltip("How quickly the follow camera catches the walker.")]
        public float FollowSmoothing = 10f;
        [Tooltip("Walker size. 1 is full size; Scout looks better around 0.5.")]
        public float WalkerScale = 1f;
        public JourneyLevelEntry[] Levels = Array.Empty<JourneyLevelEntry>();

        public int Count => Levels != null ? Levels.Length : 0;

        public float ResolvedFollowOrthographicSize =>
            FollowOrthographicSize >= 0.45f ? FollowOrthographicSize : 1.8f;

        public float ResolvedFollowSmoothing =>
            FollowSmoothing > 0f ? FollowSmoothing : 10f;

        public float ResolvedWalkerScale =>
            WalkerScale > 0.05f ? WalkerScale : 1f;

        public float FollowSizeFor(JourneyLevelEntry entry) =>
            entry != null && entry.FollowOrthographicSize >= 0.45f
                ? entry.FollowOrthographicSize
                : ResolvedFollowOrthographicSize;

        public bool LocksOrthographicSize(JourneyLevelEntry entry) =>
            entry != null && entry.FollowOrthographicSize >= 0.45f;

        public float WalkerScaleFor(JourneyLevelEntry entry) =>
            entry != null && entry.WalkerScale > 0.05f
                ? entry.WalkerScale
                : ResolvedWalkerScale;

        public JourneyLevelEntry Get(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > Count)
                throw new ArgumentOutOfRangeException(nameof(levelNumber));
            return Levels[levelNumber - 1];
        }
    }
}
