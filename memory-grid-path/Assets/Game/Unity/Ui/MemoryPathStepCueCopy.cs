using UnityEngine;

namespace Game.Unity.Ui
{
    /// <summary>Short step-feedback lines for memory slips and recoveries.</summary>
    public static class MemoryPathStepCueCopy
    {
        static readonly string[] ForgotLines =
        {
            "You already walked this!",
            "Memory slip — that turn was yours",
            "Focus — you've been here before",
            "That path was fresh last walk",
            "Oops — you knew this junction"
        };

        static readonly string[] RecoveredLines =
        {
            "Nice recall!",
            "You remembered that turn",
            "Got it this time",
            "Memory locked in",
            "Sharp — that miss is gone"
        };

        public static string RandomForgot() => Pick(ForgotLines);

        public static string RandomRecovered() => Pick(RecoveredLines);

        static string Pick(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return "";
            return lines[Random.Range(0, lines.Length)];
        }
    }
}
