using System;
using Nixin.Grid.Core;
using Nixin.Maze.Core;

namespace Game.Core
{
    public sealed class CampaignLevel
    {
        public CampaignLevel(string name, int width, int height, MazeDifficulty difficulty, float braidFactor)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Level name is required.", nameof(name));
            if (width < 2)
                throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 2.");
            if (height < 2)
                throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 2.");
            if (braidFactor < 0f || braidFactor > 1f)
                throw new ArgumentOutOfRangeException(nameof(braidFactor), "Braid factor must be in [0, 1].");

            Name = name;
            Width = width;
            Height = height;
            Difficulty = difficulty;
            BraidFactor = braidFactor;
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public MazeDifficulty Difficulty { get; }
        public float BraidFactor { get; }
        public int CellCount => Width * Height;

        public MazeSpec ToSpec()
        {
            var size = new GridSize(Width, Height);
            return new MazeSpec(
                size,
                Difficulty,
                BraidFactor,
                OpeningPlacement.RandomOpposite,
                MazeDifficultyBands.For(Difficulty, size));
        }
    }
}
