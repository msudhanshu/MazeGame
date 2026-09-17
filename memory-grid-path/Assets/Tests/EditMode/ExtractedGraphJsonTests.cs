using Game.Unity.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    [TestFixture]
    public class ExtractedGraphJsonTests
    {
        const string SampleJson = @"{
  ""displayName"": ""From Mask"",
  ""imageWidth"": 100,
  ""imageHeight"": 50,
  ""nodes"": [
    { ""id"": ""n0"", ""x"": 0.1, ""y"": 0.2, ""kind"": ""graph"" },
    { ""id"": ""n1"", ""x"": 0.9, ""y"": 0.8, ""kind"": ""graph"" },
    { ""id"": ""n2"", ""x"": 0.5, ""y"": 0.5, ""kind"": ""graph"" },
    { ""id"": ""i0"", ""x"": 0.3, ""y"": 0.35, ""kind"": ""intermediate"" }
  ],
  ""edges"": [
    {
      ""nodeA"": ""n0"",
      ""nodeB"": ""n2"",
      ""controlPoints"": [
        { ""x"": 0.3, ""y"": 0.35, ""tangentDegrees"": 12.5, ""tangentLength"": 0.04, ""kind"": ""intermediate"" }
      ]
    },
    {
      ""nodeA"": ""n2"",
      ""nodeB"": ""n1"",
      ""controlPoints"": []
    }
  ]
}";

        [Test]
        public void ApplyFillsSnapshotUvsAndKeepsBackground()
        {
            Assert.That(ExtractedGraphJson.TryParse(SampleJson, out var file, out var error), Is.True, error);

            var background = new Texture2D(2, 2);
            var snapshot = new GraphLevelSnapshot
            {
                Background = background,
                WorldWidth = 12f,
                DisplayName = "Old"
            };

            try
            {
                ExtractedGraphJson.Apply(snapshot, file);

                Assert.That(snapshot.DisplayName, Is.EqualTo("From Mask"));
                Assert.That(snapshot.Background, Is.SameAs(background));
                Assert.That(snapshot.ImageAspect, Is.EqualTo(2f).Within(0.001f));
                Assert.That(snapshot.Nodes.Count, Is.EqualTo(3));
                Assert.That(snapshot.Nodes.Exists(node => node.Id == "i0"), Is.False);
                Assert.That(snapshot.Edges.Count, Is.EqualTo(2));
                Assert.That(snapshot.Edges[0].ControlPoints.Count, Is.EqualTo(1));
                Assert.That(snapshot.Edges[0].ControlPoints[0].TangentDegrees, Is.EqualTo(12.5f).Within(0.01f));
                Assert.That(snapshot.Edges[0].ControlPoints[0].TangentLength, Is.EqualTo(0.04f).Within(0.0001f));

                for (var i = 0; i < snapshot.Nodes.Count; i++)
                {
                    Assert.That(snapshot.Nodes[i].NormalizedPosition.x, Is.InRange(0f, 1f));
                    Assert.That(snapshot.Nodes[i].NormalizedPosition.y, Is.InRange(0f, 1f));
                }

                Assert.That(snapshot.StartNodeId, Is.Not.EqualTo(snapshot.GoalNodeId));
            }
            finally
            {
                Object.DestroyImmediate(background);
            }
        }

        [Test]
        public void EmptyJsonFailsParse()
        {
            Assert.That(ExtractedGraphJson.TryParse("{}", out _, out var error), Is.False);
            Assert.That(error, Does.Contain("nodes"));
        }
    }
}
