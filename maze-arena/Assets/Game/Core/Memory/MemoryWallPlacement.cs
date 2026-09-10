using Nixin.Maze.Core;

namespace Game.Core.Memory
{
    public sealed class MemoryWallPlacement
    {
        public MemoryWallPlacement(
            MazeEdge edge,
            string entryId,
            string genreId,
            MemoryDisplayKind kind,
            string frameVariantId,
            WallFace face = WallFace.Canonical)
        {
            Edge = edge.Normalized();
            EntryId = entryId;
            GenreId = genreId;
            Kind = kind;
            FrameVariantId = frameVariantId ?? string.Empty;
            Face = face;
        }

        public MazeEdge Edge { get; }
        public WallFace Face { get; }
        public string EntryId { get; }
        public string GenreId { get; }
        public MemoryDisplayKind Kind { get; }
        public string FrameVariantId { get; }
    }
}
