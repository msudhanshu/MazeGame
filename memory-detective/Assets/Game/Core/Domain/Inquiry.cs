using Nixin.Memory.Core;

namespace Game.Core.Domain
{
    public sealed class Inquiry
    {
        public Inquiry(string question, Prompt prompt)
        {
            Question = question ?? string.Empty;
            Prompt = prompt;
        }

        public string Question { get; }
        public Prompt Prompt { get; }
    }
}
