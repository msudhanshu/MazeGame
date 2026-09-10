using Game.Unity.Audio;
using Game.Unity.Ui;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class MemoryPathAudioTests
    {
        [Test]
        public void CatalogStoresClipsAndClampsVolume()
        {
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            var clip = AudioClip.Create("success", 32, 1, 44100, false);
            try
            {
                catalog.Apply(MemoryPathCue.Success, clip, 1.8f);
                var slot = catalog.Slot(MemoryPathCue.Success);
                Assert.That(slot.Clip, Is.SameAs(clip));
                Assert.That(slot.ClampedVolume, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlaySkipsWhenSoundEffectsAreOff()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                catalog.Apply(MemoryPathCue.Fail, AudioClip.Create("fail", 32, 1, 44100, false), 1f);
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = false;
                Assert.That(MemoryPathAudio.Play(MemoryPathCue.Fail), Is.False);

                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.Play(MemoryPathCue.Fail), Is.True);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlaySkipsMissingClips()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.Play(MemoryPathCue.UiClick), Is.False);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayLongFailSkipsWhenSoundEffectsAreOff()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            try
            {
                MemoryPathAudio.ResetForTests();
                PlayerSettingsStore.SoundEffects = false;
                Assert.That(MemoryPathAudio.PlayLongFail(), Is.False);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
            }
        }

        [Test]
        public void PlayLongFailBuildsAHeldClip()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlayLongFail(), Is.True);
                Assert.That(MemoryPathAudio.LongFailSeconds, Is.GreaterThan(1.2f));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayLongFailUsesCatalogSlotWhenProvided()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            var clip = AudioClip.Create("fail-clip", 32, 1, 44100, false);
            try
            {
                MemoryPathAudio.ResetForTests();
                catalog.Apply(MemoryPathCue.Fail, clip, 0.8f);
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlayLongFail(), Is.True);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlaySuccessSkipsWhenSoundEffectsAreOff()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            try
            {
                MemoryPathAudio.ResetForTests();
                PlayerSettingsStore.SoundEffects = false;
                var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
                MemoryPathAudio.UseCatalog(catalog);
                Assert.That(MemoryPathAudio.PlaySuccess(), Is.False);
                Object.DestroyImmediate(catalog);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
            }
        }

        [Test]
        public void PlaySuccessBuildsAHeldClip()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlaySuccess(), Is.True);
                Assert.That(MemoryPathAudio.SuccessSeconds, Is.GreaterThan(1.8f));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayWalkFailSkipsWhenSoundEffectsAreOff()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            try
            {
                MemoryPathAudio.ResetForTests();
                PlayerSettingsStore.SoundEffects = false;
                var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
                MemoryPathAudio.UseCatalog(catalog);
                Assert.That(MemoryPathAudio.PlayWalkFail(), Is.False);
                Object.DestroyImmediate(catalog);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
            }
        }

        [Test]
        public void PlayWalkFailBuildsHeldClip()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlayWalkFail(), Is.True);
                Assert.That(MemoryPathAudio.WalkFailSeconds, Is.GreaterThan(0.6f));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayMistakeUsesShortMistakeSting()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlayMistake(), Is.True);
                Assert.That(MemoryPathAudio.MistakeSeconds, Is.GreaterThan(0.1f));
                Assert.That(MemoryPathAudio.MistakeSeconds, Is.LessThan(0.35f));
                Assert.That(MemoryPathAudio.MistakeSeconds, Is.LessThan(MemoryPathAudio.LongFailSeconds));
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayMistakeUsesCatalogSlotWhenProvided()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            var clip = AudioClip.Create("mistake-clip", 32, 1, 44100, false);
            try
            {
                MemoryPathAudio.ResetForTests();
                catalog.Apply(MemoryPathCue.Mistake, clip, 0.5f);
                MemoryPathAudio.UseCatalog(catalog);
                PlayerSettingsStore.SoundEffects = true;
                Assert.That(MemoryPathAudio.PlayMistake(), Is.True);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void PlayMistakeSkipsWhenSoundEffectsAreOff()
        {
            var previous = PlayerSettingsStore.SoundEffects;
            try
            {
                MemoryPathAudio.ResetForTests();
                PlayerSettingsStore.SoundEffects = false;
                Assert.That(MemoryPathAudio.PlayMistake(), Is.False);
            }
            finally
            {
                PlayerSettingsStore.SoundEffects = previous;
                MemoryPathAudio.ResetForTests();
            }
        }

        [Test]
        public void EnsureCreatesAudioHost()
        {
            var catalog = ScriptableObject.CreateInstance<MemoryPathAudioCatalog>();
            try
            {
                MemoryPathAudio.ResetForTests();
                MemoryPathAudio.UseCatalog(catalog);
                MemoryPathAudio.Ensure();
                var host = GameObject.Find("MemoryPathAudio");
                Assert.That(host, Is.Not.Null);
                Assert.That(host.GetComponent<AudioSource>(), Is.Not.Null);
            }
            finally
            {
                MemoryPathAudio.ResetForTests();
                Object.DestroyImmediate(catalog);
            }
        }
    }
}
