using System;
using System.Collections.Generic;
using Nixin.Memory.Core;

namespace Game.Core.Domain
{
    public sealed class CaseFile
    {
        public CaseFile(string title, IReadOnlyList<string> clues, IReadOnlyList<Inquiry> inquiries)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Case title is required.", nameof(title));
            if (clues == null || clues.Count == 0)
                throw new ArgumentException("A case needs at least one clue.", nameof(clues));
            if (inquiries == null || inquiries.Count == 0)
                throw new ArgumentException("A case needs at least one inquiry.", nameof(inquiries));

            Title = title;
            Clues = clues;
            Inquiries = inquiries;

            var prompts = new Prompt[inquiries.Count];
            for (var i = 0; i < inquiries.Count; i++)
            {
                if (inquiries[i] == null)
                    throw new ArgumentException("Inquiry cannot be null.", nameof(inquiries));
                if (string.IsNullOrWhiteSpace(inquiries[i].Question))
                    throw new ArgumentException("Inquiry question is required.", nameof(inquiries));
                prompts[i] = inquiries[i].Prompt;
            }

            Prompts = prompts;
        }

        public string Title { get; }
        public IReadOnlyList<string> Clues { get; }
        public IReadOnlyList<Inquiry> Inquiries { get; }
        public IReadOnlyList<Prompt> Prompts { get; }
    }
}
