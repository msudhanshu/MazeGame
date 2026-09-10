using Game.Core.Memory;
using Nixin.Maze.Core;
using UnityEngine;

namespace Game.Unity.Memory
{
    /// <summary>
    /// Identifies which catalog photo sits on this wall face so gameplay can look it up.
    /// </summary>
    public sealed class MemoryDisplayTag : MonoBehaviour
    {
        public string EntryId;
        public string GenreId;
        public MemoryDisplayKind Kind;
        public string FrameVariantId;
        public WallFace Face;
        public string EdgeId;

        public void Bind(MemoryWallPlacement placement)
        {
            EntryId = placement.EntryId;
            GenreId = placement.GenreId;
            Kind = placement.Kind;
            FrameVariantId = placement.FrameVariantId;
            Face = placement.Face;
            EdgeId = placement.Edge.ToString();
        }
    }
}
