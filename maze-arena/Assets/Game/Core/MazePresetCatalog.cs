using System;
using System.Collections.Generic;
using Nixin.Maze.Core;

namespace Game.Core
{
    public sealed class MazePresetCatalog
    {
        readonly Dictionary<string, MazePreset> _byName;
        readonly IReadOnlyList<MazePreset> _all;

        public MazePresetCatalog(IReadOnlyList<MazePreset> presets)
        {
            if (presets == null || presets.Count == 0)
                throw new ArgumentException("At least one maze preset is required.", nameof(presets));

            _byName = new Dictionary<string, MazePreset>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < presets.Count; i++)
            {
                var preset = presets[i];
                if (preset == null)
                    throw new ArgumentException("Preset list cannot contain null entries.", nameof(presets));
                if (_byName.ContainsKey(preset.Name))
                    throw new ArgumentException($"Duplicate preset name '{preset.Name}'.", nameof(presets));
                _byName[preset.Name] = preset;
            }

            _all = presets;
        }

        public IReadOnlyList<MazePreset> All => _all;

        public MazePreset Get(string name)
        {
            if (!_byName.TryGetValue(name, out var preset))
                throw new ArgumentException($"Unknown maze preset '{name}'.", nameof(name));
            return preset;
        }

        public static MazePresetCatalog Default()
        {
            return new MazePresetCatalog(new[]
            {
                new MazePreset("Courtyard", 6, 6, MazeDifficulty.Easy, 0.55f, 1),
                new MazePreset("Garden", 8, 8, MazeDifficulty.Medium, 0.25f, 7),
                new MazePreset("Keep", 12, 12, MazeDifficulty.Hard, 0.08f, 21),
                new MazePreset("Labyrinth", 16, 16, MazeDifficulty.Brutal, 0f, 42)
            });
        }
    }
}
