using Nixin.Fue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Unity.Tests
{
    public sealed class FueCenterCardTests
    {
        [Test]
        public void BuildsAnOkButtonBelowTheBody()
        {
            var parent = new GameObject("Hud", typeof(RectTransform), typeof(Canvas));
            try
            {
                var card = FueCenterCard.Create(parent.transform);
                Assert.That(card.transform.Find("Close"), Is.Null);

                var ok = card.transform.Find("Card/" + FueCenterCard.OkName);
                Assert.That(ok, Is.Not.Null);
                Assert.That(ok.GetComponent<Button>(), Is.Not.Null);
                Assert.That(ok.GetComponentInChildren<Text>(true).text, Is.EqualTo(FueCenterCard.OkLabel));

                var rect = ok.GetComponent<RectTransform>();
                Assert.That(rect.anchorMin, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(rect.anchorMax, Is.EqualTo(new Vector2(0.5f, 0f)));
                Assert.That(rect.anchoredPosition.y, Is.GreaterThan(0f));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }
    }
}
