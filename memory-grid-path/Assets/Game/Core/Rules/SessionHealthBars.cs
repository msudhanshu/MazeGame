using System;

namespace Game.Core.Rules
{
    public enum HealthSegmentKind
    {
        Spent,
        Remaining
    }

    /// <summary>
    /// Session lives are bars; each bar is the walk-life segments for one walk.
    /// Spent walks go dark, the current walk shows hearts still left, and unused
    /// walks stay lit so the player can see how much of the session remains.
    /// </summary>
    public static class SessionHealthBars
    {
        public static HealthSegmentKind Segment(
            int barIndex,
            int segmentIndex,
            int currentRun,
            int livesLeft,
            int livesPerRun,
            int runsPerSession)
        {
            if (barIndex < 0 || barIndex >= runsPerSession)
                throw new ArgumentOutOfRangeException(nameof(barIndex));
            if (segmentIndex < 0 || segmentIndex >= livesPerRun)
                throw new ArgumentOutOfRangeException(nameof(segmentIndex));
            if (currentRun < 1 || currentRun > runsPerSession)
                throw new ArgumentOutOfRangeException(nameof(currentRun));
            if (livesLeft < 0 || livesLeft > livesPerRun)
                throw new ArgumentOutOfRangeException(nameof(livesLeft));
            if (livesPerRun < 1)
                throw new ArgumentOutOfRangeException(nameof(livesPerRun));
            if (runsPerSession < 1)
                throw new ArgumentOutOfRangeException(nameof(runsPerSession));

            var walk = barIndex + 1;
            if (walk < currentRun)
                return HealthSegmentKind.Spent;
            if (walk > currentRun)
                return HealthSegmentKind.Remaining;

            return segmentIndex < livesLeft ? HealthSegmentKind.Remaining : HealthSegmentKind.Spent;
        }
    }
}
