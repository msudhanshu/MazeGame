using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class MobileChromeTests
    {
        [Test]
        public void PlayerKeepsStatusBarVisibleAndRendersBehindCutout()
        {
            Assert.That(PlayerSettings.statusBarHidden, Is.False);
            Assert.That(PlayerSettings.Android.renderOutsideSafeArea, Is.True);
            Assert.That(PlayerSettings.Android.startInFullscreen, Is.True);
        }
    }
}
