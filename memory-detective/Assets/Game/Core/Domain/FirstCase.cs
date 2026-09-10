using Nixin.Memory.Core;

namespace Game.Core.Domain
{
    public static class FirstCase
    {
        public static CaseFile ManorMurder()
        {
            return new CaseFile(
                "The Manor Murder",
                new[]
                {
                    "The study key hung on the maid's ring.",
                    "Wet footprints led from the garden into the hall.",
                    "The library clock was smashed at nine."
                },
                new[]
                {
                    new Inquiry(
                        "Who held the study key?",
                        new Prompt(
                            "key_holder",
                            new[] { new TokenId("maid"), new TokenId("butler"), new TokenId("cook") },
                            new[] { new TokenId("maid") })),
                    new Inquiry(
                        "Where did the intruder come from?",
                        new Prompt(
                            "entry",
                            new[] { new TokenId("garden"), new TokenId("roof"), new TokenId("cellar") },
                            new[] { new TokenId("garden") }))
                });
        }
    }
}
