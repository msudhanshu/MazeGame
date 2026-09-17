using System.Collections.Generic;
using Game.Unity.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Unity.Audio
{
    /// <summary>Plays catalog one-shots and respects the settings Sound Effects toggle.</summary>
    public static class MemoryPathAudio
    {
        static MemoryPathAudioCatalog _catalog;
        static AudioSource _source;
        static GameObject _host;
        static AudioClip _longFail;
        static AudioClip _walkFail;
        static AudioClip _mistakeSting;
        static AudioClip _successSting;

        public static MemoryPathAudioCatalog Catalog
        {
            get
            {
                Ensure();
                return _catalog;
            }
        }

        public static void UseCatalog(MemoryPathAudioCatalog catalog)
        {
            _catalog = catalog;
        }

        public static void ResetForTests()
        {
            if (_host != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_host);
                else
                    Object.DestroyImmediate(_host);
            }

            _host = null;
            _source = null;
            _catalog = null;
            _longFail = null;
            _walkFail = null;
            _mistakeSting = null;
            _successSting = null;
        }

        public static bool Play(MemoryPathCue cue)
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null || _catalog == null)
                return false;

            var slot = _catalog.Slot(cue);
            if (slot.Clip == null)
                return false;

            _source.PlayOneShot(slot.Clip, slot.ClampedVolume);
            return true;
        }

        public static bool PlayCorrectStep(int stepIndex)
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            var pitch = Mathf.Pow(1.059463f, Mathf.Clamp(stepIndex, 0, 14));
            _source.pitch = pitch;
            var played = Play(MemoryPathCue.CorrectStep);
            if (!played)
            {
                _source.PlayOneShot(BuildSuccessSound("correct-step", 0.18f), 0.45f);
                played = true;
            }

            _source.pitch = 1f;
            return played;
        }

        public static bool PlayRevealChime()
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            _source.pitch = 1.35f;
            var played = Play(MemoryPathCue.CorrectStep);
            _source.pitch = 1f;
            if (played)
                return true;

            _source.PlayOneShot(BuildSuccessSound("reveal-chime", 0.22f), 0.5f);
            return true;
        }

        static bool TryPlayCatalogCue(MemoryPathCue cue, float volumeMultiplier)
        {
            if (_catalog == null || _source == null)
                return false;
            var slot = _catalog.Slot(cue);
            if (slot.Clip == null)
                return false;
            var volume = Mathf.Clamp01(slot.ClampedVolume * volumeMultiplier);
            _source.PlayOneShot(slot.Clip, volume);
            return true;
        }

        public const float MistakeSeconds = 0.22f;
        public const float WalkFailSeconds = 0.9f;
        public const float LongFailSeconds = 3.0f;
        public const float SuccessSeconds = 2.4f;

        public static bool PlayMistake()
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            if (TryPlayCatalogCue(MemoryPathCue.Mistake, 1.25f))
                return true;

            if (_mistakeSting == null)
                _mistakeSting = BuildMistakeSting("mistake-sting", MistakeSeconds);

            _source.PlayOneShot(_mistakeSting, 0.75f);
            return true;
        }

        public static bool PlayWalkFail()
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            if (_walkFail == null)
                _walkFail = BuildWalkFailSting("walk-fail", WalkFailSeconds);

            _source.PlayOneShot(_walkFail, 0.85f);
            return true;
        }

        public static bool PlayLongFail()
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            if (_longFail == null)
                _longFail = BuildBigFailSound("session-fail", LongFailSeconds);

            _source.PlayOneShot(_longFail, 1f);
            return true;
        }

        public static bool PlaySuccess()
        {
            if (!PlayerSettingsStore.SoundEffects)
                return false;

            Ensure();
            if (_source == null)
                return false;

            if (_successSting == null)
                _successSting = BuildSuccessSound("level-success", SuccessSeconds);

            _source.PlayOneShot(_successSting, 1f);
            return true;
        }

        static AudioClip BuildMistakeSting(string name, float seconds)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];
            var phase = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var u = t / seconds;
                var freq = Mathf.Lerp(330f, 140f, Mathf.Sqrt(u));
                phase += 2f * Mathf.PI * freq / sampleRate;

                var attack = t < 0.005f ? t / 0.005f : 1f;
                var decay = Mathf.Pow(1f - u, 1.3f);
                var env = attack * decay;

                var sine = Mathf.Sin(phase);
                var harmonic = Mathf.Sin(2f * phase) * 0.3f;
                var sub = Mathf.Sin(0.5f * phase) * 0.2f;
                var punch = Mathf.Exp(-t * 100f) * 0.3f * Mathf.Sin(2f * Mathf.PI * 400f * t);

                var sample = (sine + harmonic + sub + punch) * 0.8f * env;
                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip BuildWalkFailSting(string name, float seconds)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];

            float phase = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var u = t / seconds;

                // A more serious step-skip: lower sweep with a small transient.
                var freq = Mathf.Lerp(420f, 110f, Mathf.Pow(u, 1.15f));
                phase += 2f * Mathf.PI * freq / sampleRate;

                var attack = t < 0.006f ? t / 0.006f : 1f;
                var decay = Mathf.Pow(1f - u, 1.35f);
                var env = attack * decay;

                var sine = Mathf.Sin(phase) * 0.75f;
                var harmonic = Mathf.Sin(2f * phase) * 0.22f;
                var sub = Mathf.Sin(0.5f * phase) * 0.18f;

                var transient = Mathf.Exp(-t * 160f) * Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.25f;
                var sample = (sine + harmonic + sub + transient) * env;

                data[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip BuildBigFailSound(string name, float seconds)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];

            // The pattern was authored around 2.0s; scale time so increasing LongFailSeconds
            // also increases audible length (instead of adding silence).
            var patternScale = seconds / 2.0f;
            var noteFreqs = new[] { 311.13f, 277.18f, 261.63f, 130.81f };
            var noteStarts = new[] { 0.0f, 0.32f, 0.65f, 1.0f };
            var noteDurs = new[] { 0.38f, 0.38f, 0.42f, 1.0f };

            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var sample = 0f;

                for (var n = 0; n < noteFreqs.Length; n++)
                {
                    var nStart = noteStarts[n] * patternScale;
                    var nDur = noteDurs[n] * patternScale;
                    if (t < nStart || t > nStart + nDur)
                        continue;

                    var localT = t - nStart;
                    var localU = localT / nDur;
                    var f = noteFreqs[n];

                    if (n == 3)
                        f = Mathf.Lerp(f, 65f, Mathf.Pow(localU, 1.5f));

                    var attack = localT < 0.012f ? localT / 0.012f : 1f;
                    var release = Mathf.Pow(1f - localU, n == 3 ? 1.2f : 1.8f);
                    var noteEnv = attack * release;

                    var p = 2f * Mathf.PI * f * localT;
                    var primary = Mathf.Sin(p);
                    var fifth = Mathf.Sin(p * 1.498f) * 0.28f;
                    var sub = Mathf.Sin(p * 0.5f) * 0.35f;
                    var warmSaw = (Mathf.PingPong(p / Mathf.PI, 1f) - 0.5f) * 0.2f;

                    sample += (primary + fifth + sub + warmSaw) * noteEnv * (n == 3 ? 0.75f : 0.6f);
                }

                var masterEnv = t > seconds - 0.05f
                    ? (seconds - t) / 0.05f
                    : 1f;

                data[i] = Mathf.Clamp(sample * masterEnv, -1f, 1f);
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip BuildSuccessSound(string name, float seconds)
        {
            const int sampleRate = 22050;
            var samples = Mathf.CeilToInt(sampleRate * seconds);
            var data = new float[samples];

            // Gentle upward "ding" with a longer tail.
            var patternScale = seconds / 2.2f;
            var noteFreqs = new[] { 392f, 523.25f, 659.25f, 880f }; // G4 C5 E5 A5
            var noteStarts = new[] { 0.0f, 0.35f, 0.75f, 1.15f };
            var noteDurs = new[] { 0.35f, 0.4f, 0.45f, 0.7f };

            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var sample = 0f;

                for (var n = 0; n < noteFreqs.Length; n++)
                {
                    var nStart = noteStarts[n] * patternScale;
                    var nDur = noteDurs[n] * patternScale;
                    if (t < nStart || t > nStart + nDur)
                        continue;

                    var localT = t - nStart;
                    var localU = localT / nDur;
                    var f = noteFreqs[n];

                    var attack = localT < 0.01f ? localT / 0.01f : 1f;
                    var releasePower = n == 3 ? 2.2f : 1.8f;
                    var env = attack * Mathf.Pow(1f - localU, releasePower);

                    var p = 2f * Mathf.PI * f * localT;
                    var sine = Mathf.Sin(p);
                    var harmonic = Mathf.Sin(p * 2f) * 0.22f;
                    var sub = Mathf.Sin(p * 0.5f) * 0.12f;

                    sample += (sine + harmonic + sub) * env * (n == 3 ? 0.8f : 0.65f);
                }

                // Persistent shimmer tail (keeps success feeling "longer").
                var tailEnv = Mathf.Exp(-t * 1.25f);
                var shimmer = Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.12f * tailEnv;

                var masterEnv = t > seconds - 0.08f
                    ? (seconds - t) / 0.08f
                    : 1f;

                data[i] = Mathf.Clamp((sample + shimmer) * masterEnv, -1f, 1f);
            }

            var clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static void Ensure()
        {
            if (_catalog == null)
                _catalog = LoadCatalog();

            if (_host != null)
                return;

            _host = new GameObject("MemoryPathAudio");
            _host.hideFlags = HideFlags.DontSave;
            if (Application.isPlaying)
                Object.DontDestroyOnLoad(_host);
            _source = _host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _host.AddComponent<UiClickAudioListener>();
        }

        static MemoryPathAudioCatalog LoadCatalog()
        {
#if UNITY_EDITOR
            var fromDisk = UnityEditor.AssetDatabase.LoadAssetAtPath<MemoryPathAudioCatalog>(
                MemoryPathAudioCatalog.EditorAssetPath);
            if (fromDisk != null)
                return fromDisk;
#endif
            return Resources.Load<MemoryPathAudioCatalog>(MemoryPathAudioCatalog.ResourceName);
        }
    }

    sealed class UiClickAudioListener : MonoBehaviour
    {
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        void Update()
        {
            if (!Pressed(out var position))
                return;
            if (EventSystem.current == null)
                return;

            var pointer = new PointerEventData(EventSystem.current) { position = position };
            _hits.Clear();
            EventSystem.current.RaycastAll(pointer, _hits);
            for (var i = 0; i < _hits.Count; i++)
            {
                var hit = _hits[i].gameObject;
                if (hit != null && hit.GetComponentInParent<Button>() != null)
                {
                    MemoryPathAudio.Play(MemoryPathCue.UiClick);
                    return;
                }
            }
        }

        static bool Pressed(out Vector2 position)
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                position = mouse.position.ReadValue();
                return true;
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame)
            {
                position = touch.primaryTouch.position.ReadValue();
                return true;
            }

            position = default;
            return false;
        }
    }
}
