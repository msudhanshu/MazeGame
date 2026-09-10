using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Fits a dragged mosaic VFX prefab into the board box. Occa fireworks are one-shot
    /// Visual Effects designed for a large outdoor scene; we scale them uniformly, shrink
    /// their velocities so bursts stay under the glass, and replay them.
    /// </summary>
    public sealed class MosaicBoxedVfx : MonoBehaviour
    {
        public const float SourceSpan = 16f;
        public const float ReplaySeconds = 2.3f;

        MosaicParticleBox _box;
        VisualEffect[] _effects;
        GameObject[] _burstPrefabs;
        float _nextReplay;

        public static Vector3 UniformScale(MosaicParticleBox box)
        {
            var span = Mathf.Max(0.05f, Mathf.Min(box.Size.x, box.Size.z));
            return Vector3.one * Mathf.Clamp(span / SourceSpan, 0.05f, 0.75f);
        }

        public void Bind(MosaicParticleBox box)
        {
            _box = box;
            _burstPrefabs = ReadSpawnerPrefabs();
            _effects = GetComponentsInChildren<VisualEffect>(true);

            if (_burstPrefabs.Length > 0)
            {
                transform.localScale = Vector3.one;
                _nextReplay = Time.time;
                return;
            }

            if (_effects.Length > 0)
            {
                transform.localScale = UniformScale(box);
                for (var i = 0; i < _effects.Length; i++)
                    Tune(_effects[i], box, transform.localScale.x);
                QuietAudio();
                Replay();
                return;
            }

            transform.localScale = box.Size;
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.loop = true;
                main.playOnAwake = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                if (!systems[i].isPlaying)
                    systems[i].Play(true);
            }
        }

        void Update()
        {
            if (Time.time < _nextReplay)
                return;

            if (_burstPrefabs.Length > 0)
            {
                SpawnBurst();
                _nextReplay = Time.time + ReplaySeconds * Random.Range(0.7f, 1.2f);
                return;
            }

            if (_effects != null && _effects.Length > 0)
                Replay();
        }

        void Replay()
        {
            if (_effects == null)
                return;

            for (var i = 0; i < _effects.Length; i++)
            {
                if (_effects[i] == null)
                    continue;
                _effects[i].Reinit();
                _effects[i].Play();
            }

            _nextReplay = Time.time + ReplaySeconds;
        }

        void SpawnBurst()
        {
            var prefab = _burstPrefabs[Random.Range(0, _burstPrefabs.Length)];
            if (prefab == null)
                return;

            var go = Instantiate(prefab, transform, false);
            go.name = prefab.name;
            var pad = 0.65f;
            go.transform.localPosition = new Vector3(
                Random.Range(-_box.Extents.x * pad, _box.Extents.x * pad),
                0f,
                Random.Range(-_box.Extents.z * pad, _box.Extents.z * pad));
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = UniformScale(_box);

            var effects = go.GetComponentsInChildren<VisualEffect>(true);
            for (var i = 0; i < effects.Length; i++)
                Tune(effects[i], _box, go.transform.localScale.x);
            QuietAudio(go);

            Destroy(go, 5.5f);
        }

        static void Tune(VisualEffect vfx, MosaicParticleBox box, float scale)
        {
            if (vfx == null)
                return;

            var safeScale = Mathf.Max(0.04f, scale);
            const float rocketLife = 0.28f;
            if (vfx.HasFloat("Initial Rocket Lifetime"))
                vfx.SetFloat("Initial Rocket Lifetime", rocketLife);
            if (vfx.HasVector3("Initial Rocket Velocity"))
            {
                var rise = box.Size.y * 0.4f / (safeScale * rocketLife);
                vfx.SetVector3("Initial Rocket Velocity", new Vector3(0f, rise, 0f));
            }

            const float sparkleLife = 0.65f;
            if (vfx.HasFloat("Sparkles Lifetime"))
                vfx.SetFloat("Sparkles Lifetime", sparkleLife);
            if (vfx.HasFloat("Sparkle Spawner Lifetime"))
                vfx.SetFloat("Sparkle Spawner Lifetime", sparkleLife);
            if (vfx.HasFloat("Sparkle Spawner Speed"))
            {
                var travel = Mathf.Min(box.Size.x, box.Size.z) * 0.28f;
                vfx.SetFloat("Sparkle Spawner Speed", travel / (safeScale * sparkleLife));
            }

            if (vfx.HasFloat("Sparkles Size"))
                vfx.SetFloat("Sparkles Size", 0.045f / safeScale);
            if (vfx.HasFloat("Initial Rocket Flare Size"))
                vfx.SetFloat("Initial Rocket Flare Size", 0.06f / safeScale);
        }

        void QuietAudio() => QuietAudio(gameObject);

        static void QuietAudio(GameObject root)
        {
            var sources = root.GetComponentsInChildren<AudioSource>(true);
            for (var i = 0; i < sources.Length; i++)
                sources[i].volume = 0.18f;
        }

        GameObject[] ReadSpawnerPrefabs()
        {
            var found = new List<GameObject>();
            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType().Name != "FireworkSpawner")
                    continue;

                behaviour.enabled = false;
                var field = behaviour.GetType().GetField("visualEffects", BindingFlags.Instance | BindingFlags.Public);
                if (field?.GetValue(behaviour) is IList list)
                {
                    for (var p = 0; p < list.Count; p++)
                    {
                        if (list[p] is GameObject prefab && prefab != null)
                            found.Add(prefab);
                    }
                }
            }

            return found.ToArray();
        }
    }
}
