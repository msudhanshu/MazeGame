using Game.Unity.Fue;
using Game.Unity.Ui;
using NUnit.Framework;

namespace Game.Unity.Tests
{
    public sealed class TutorialCopyTests
    {
        [Test]
        public void PlayerFacingCopyDoesNotSellLuck()
        {
            var lines = new[]
            {
                TutorialCopy.Opening,
                TutorialCopy.Watch,
                TutorialCopy.Prompt,
                TutorialCopy.KeepGoing,
                TutorialCopy.Ready,
                LevelOneCopy.Prompt,
                LevelOneCopy.Health,
                ScoutScanCopy.Hold,
                ScoutScanCopy.Release,
                JourneyHomeScreen.TitleCopy,
                JourneyHomeScreen.TagCopy,
                GridPathHud.DefaultHintSub
            };

            for (var i = 0; i < lines.Length; i++)
            {
                var lower = lines[i].ToLowerInvariant();
                Assert.That(lower, Does.Not.Contain("try your luck"));
                Assert.That(lower, Does.Not.Contain("lucky you"));
                Assert.That(lower, Does.Not.Contain("lucky"));
            }
        }

        [Test]
        public void OpeningTellsThePlayerToWatchThenWalk()
        {
            Assert.That(TutorialCopy.Opening, Does.Contain("Remember the path home"));
            Assert.That(TutorialCopy.Opening, Does.Contain("Watch"));
            Assert.That(TutorialCopy.Watch, Does.Contain("Watch"));
            Assert.That(TutorialCopy.Prompt, Does.Contain("Tap"));
            Assert.That(JourneyHomeScreen.TitleCopy, Is.EqualTo("Memory Game: Remember the Path Home"));
        }
    }
}
