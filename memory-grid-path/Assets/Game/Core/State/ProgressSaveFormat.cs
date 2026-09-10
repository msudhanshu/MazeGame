using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Game.Core.State
{
    /// <summary>
    /// Text blob for local progress. v1 is the old highest|scores form; v2 adds career and skip;
    /// v3 adds highest cleared so a finished last level still counts.
    /// </summary>
    public static class ProgressSaveFormat
    {
        const string Version2Prefix = "v2|";
        const string Version3Prefix = "v3|";

        public static string Serialize(PlayerProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            var builder = new StringBuilder();
            builder.Append(Version3Prefix);
            builder.Append(progress.HighestUnlockedLevel.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(progress.CareerScore.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(progress.SkipCharges.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(progress.HighGradeStreak.ToString(CultureInfo.InvariantCulture)).Append('|');
            builder.Append(progress.HighestClearedLevel.ToString(CultureInfo.InvariantCulture)).Append('|');

            var first = true;
            foreach (var entry in progress.BestScores)
            {
                if (!first)
                    builder.Append(',');

                builder.Append(entry.Key.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(entry.Value.ToString(CultureInfo.InvariantCulture));
                first = false;
            }

            return builder.ToString();
        }

        public static PlayerProgress Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new PlayerProgress();

            if (raw.StartsWith(Version3Prefix, StringComparison.Ordinal))
                return ParseV3(raw.Substring(Version3Prefix.Length));

            if (raw.StartsWith(Version2Prefix, StringComparison.Ordinal))
                return ParseV2(raw.Substring(Version2Prefix.Length));

            return ParseV1(raw);
        }

        static PlayerProgress ParseV1(string raw)
        {
            var parts = raw.Split('|');
            if (!TryInt(parts[0], out var highest) || highest < 1)
                return new PlayerProgress();

            return new PlayerProgress(highest, ParseScores(parts.Length > 1 ? parts[1] : string.Empty));
        }

        static PlayerProgress ParseV2(string body)
        {
            var parts = body.Split('|');
            if (parts.Length < 5)
                return new PlayerProgress();

            if (!TryInt(parts[0], out var highest) || highest < 1)
                return new PlayerProgress();
            if (!TryInt(parts[1], out var career) || career < 0)
                return new PlayerProgress();
            if (!TryInt(parts[2], out var charges) || charges < 0)
                return new PlayerProgress();
            if (!TryInt(parts[3], out var streak) || streak < 0)
                return new PlayerProgress();

            return new PlayerProgress(highest, ParseScores(parts[4]), career, charges, streak);
        }

        static PlayerProgress ParseV3(string body)
        {
            var parts = body.Split('|');
            if (parts.Length < 6)
                return new PlayerProgress();

            if (!TryInt(parts[0], out var highest) || highest < 1)
                return new PlayerProgress();
            if (!TryInt(parts[1], out var career) || career < 0)
                return new PlayerProgress();
            if (!TryInt(parts[2], out var charges) || charges < 0)
                return new PlayerProgress();
            if (!TryInt(parts[3], out var streak) || streak < 0)
                return new PlayerProgress();
            if (!TryInt(parts[4], out var cleared) || cleared < 0)
                return new PlayerProgress();

            return new PlayerProgress(highest, ParseScores(parts[5]), career, charges, streak, cleared);
        }

        static Dictionary<int, int> ParseScores(string blob)
        {
            var scores = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(blob))
                return scores;

            foreach (var entry in blob.Split(','))
            {
                var pair = entry.Split(':');
                if (pair.Length != 2)
                    continue;
                if (!TryInt(pair[0], out var level))
                    continue;
                if (!TryInt(pair[1], out var score))
                    continue;

                scores[level] = score;
            }

            return scores;
        }

        static bool TryInt(string text, out int value) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }
}
