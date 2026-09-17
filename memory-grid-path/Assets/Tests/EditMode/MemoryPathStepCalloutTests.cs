using Game.Unity.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class MemoryPathStepCalloutTests
    {
        [Test]
        public void EnsureAddsCanvasGroupThatDoesNotBlockRaycasts()
        {
            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var callout = MemoryPathStepCallout.Ensure(parent.transform);
                Assert.That(callout, Is.Not.Null);
                var group = callout.GetComponent<CanvasGroup>();
                Assert.That(group, Is.Not.Null);
                Assert.That(group.blocksRaycasts, Is.False);
                Assert.That(group.interactable, Is.False);
                Assert.That(MemoryPathStepCallout.Ensure(parent.transform), Is.SameAs(callout));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
