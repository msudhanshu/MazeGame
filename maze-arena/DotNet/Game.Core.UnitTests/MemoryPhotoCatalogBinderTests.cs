using System;
using Game.Core.Memory;
using NUnit.Framework;

namespace Game.Core.Tests
{
    [TestFixture]
    public class MemoryPhotoCatalogBinderTests
    {
        [Test]
        public void MatchingFilenamesLeaveExistingEntriesUnchanged()
        {
            var catalog = DemoCatalog();
            var bound = MemoryPhotoCatalogBinder.Bind(catalog, new[] { "oak", "river" });

            Assert.That(bound.Entries.Count, Is.EqualTo(catalog.Entries.Count));
            Assert.That(bound.Entries[0].Id, Is.EqualTo("oak"));
            Assert.That(bound.Entries[0].GenreId, Is.EqualTo("nature"));
            Assert.That(bound.Entries[0].FrameVariantId, Is.EqualTo("plain"));
            Assert.That(bound.Entries[1].Kind, Is.EqualTo(MemoryDisplayKind.Photo));
        }

        [Test]
        public void LeftoverFilenamesBecomeExtraPhotoEntries()
        {
            var catalog = DemoCatalog();
            var bound = MemoryPhotoCatalogBinder.Bind(catalog, new[] { "oak", "monalisa", "starrynight" });

            Assert.That(bound.Entries.Count, Is.EqualTo(4));
            Assert.That(bound.Entries[2].Id, Is.EqualTo("monalisa"));
            Assert.That(bound.Entries[2].Kind, Is.EqualTo(MemoryDisplayKind.Photo));
            Assert.That(bound.Entries[2].GenreId, Is.EqualTo("art"));
            Assert.That(bound.Entries[2].FrameVariantId, Is.EqualTo(MemoryPhotoCatalogBinder.ExtraFrameVariantId));
            Assert.That(bound.Entries[3].Id, Is.EqualTo("starrynight"));
        }

        [Test]
        public void EmptyOrNullFileListReturnsTheSameCatalog()
        {
            var catalog = DemoCatalog();
            Assert.That(MemoryPhotoCatalogBinder.Bind(catalog, Array.Empty<string>()), Is.SameAs(catalog));
            Assert.Throws<ArgumentNullException>(() => MemoryPhotoCatalogBinder.Bind(null, new[] { "oak" }));
        }

        static MemoryCatalogSnapshot DemoCatalog()
        {
            return new MemoryCatalogSnapshot(
                new[]
                {
                    new MemoryGenreSnapshot("nature", "Nature"),
                    new MemoryGenreSnapshot("art", "Art")
                },
                new[]
                {
                    new MemoryEntrySnapshot("oak", "nature", MemoryDisplayKind.Photo, "plain"),
                    new MemoryEntrySnapshot("river", "nature", MemoryDisplayKind.Photo, "frame")
                });
        }
    }
}
