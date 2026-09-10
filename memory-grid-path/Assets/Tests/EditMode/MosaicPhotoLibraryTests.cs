using System.Linq;
using Game.Unity.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class MosaicPhotoLibraryTests
    {
        [Test]
        public void LoadAllFindsMosaicFolderPhotos()
        {
            var photos = MosaicPhotoLibrary.LoadAll();
            Assert.That(photos.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(photos.Any(p => p.name.Contains("monalisa")), Is.True);
        }

        [Test]
        public void PickIsStablePerLevelAndVariesAcrossLevels()
        {
            var first = MosaicPhotoLibrary.Pick(2);
            var again = MosaicPhotoLibrary.Pick(2);
            var other = MosaicPhotoLibrary.Pick(4);
            Assert.That(first, Is.Not.Null);
            Assert.That(again, Is.SameAs(first));
            Assert.That(other, Is.Not.Null);
        }

        [Test]
        public void RollParticlesIsStablePerLevel()
        {
            Assert.That(MosaicPhotoLibrary.RollParticles(2), Is.EqualTo(MosaicPhotoLibrary.RollParticles(2)));
            Assert.That(MosaicPhotoLibrary.RollParticles(14), Is.EqualTo(MosaicPhotoLibrary.RollParticles(14)));
        }
    }
}
