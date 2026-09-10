using System;
using System.Globalization;
using System.Text;

namespace Game.Core.State
{
    /// <summary>
    /// Local journey blob. Each mode's PlayerProgress is Base64 so nested pipes stay intact.
    /// </summary>
    public static class JourneySaveFormat
    {
        const string Prefix = "j1|";

        public static string Serialize(JourneyProgress journey)
        {
            if (journey == null)
                throw new ArgumentNullException(nameof(journey));

            return Prefix
                   + ModeName(journey.LastSelectedMode) + "|"
                   + journey.LastPlayedLevel(GameModeId.TileArena).ToString(CultureInfo.InvariantCulture) + "|"
                   + journey.LastPlayedLevel(GameModeId.GraphArena).ToString(CultureInfo.InvariantCulture) + "|"
                   + journey.LastPlayedLevel(GameModeId.ScoutArena).ToString(CultureInfo.InvariantCulture) + "|"
                   + Encode(ProgressSaveFormat.Serialize(journey.For(GameModeId.TileArena))) + "|"
                   + Encode(ProgressSaveFormat.Serialize(journey.For(GameModeId.GraphArena))) + "|"
                   + Encode(ProgressSaveFormat.Serialize(journey.For(GameModeId.ScoutArena)));
        }

        public static JourneyProgress Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw) || !raw.StartsWith(Prefix, StringComparison.Ordinal))
                return new JourneyProgress();

            var parts = raw.Substring(Prefix.Length).Split('|');
            if (parts.Length < 7)
                return new JourneyProgress();

            var lastMode = ParseMode(parts[0]);
            if (!TryInt(parts[1], out var lastTile))
                lastTile = 1;
            if (!TryInt(parts[2], out var lastGraph))
                lastGraph = 1;
            if (!TryInt(parts[3], out var lastScout))
                lastScout = 1;

            return new JourneyProgress(
                ProgressSaveFormat.Parse(Decode(parts[4])),
                ProgressSaveFormat.Parse(Decode(parts[5])),
                ProgressSaveFormat.Parse(Decode(parts[6])),
                lastMode,
                lastTile,
                lastGraph,
                lastScout);
        }

        static string ModeName(GameModeId mode)
        {
            switch (mode)
            {
                case GameModeId.GraphArena:
                    return "GraphArena";
                case GameModeId.ScoutArena:
                    return "ScoutArena";
                default:
                    return "TileArena";
            }
        }

        static GameModeId ParseMode(string name)
        {
            if (string.Equals(name, "GraphArena", StringComparison.Ordinal))
                return GameModeId.GraphArena;
            if (string.Equals(name, "ScoutArena", StringComparison.Ordinal))
                return GameModeId.ScoutArena;
            return GameModeId.TileArena;
        }

        static string Encode(string text) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(text ?? string.Empty));

        static string Decode(string blob)
        {
            if (string.IsNullOrEmpty(blob))
                return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(blob));
            }
            catch (FormatException)
            {
                return string.Empty;
            }
        }

        static bool TryInt(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
