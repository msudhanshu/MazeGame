using UnityEngine;

namespace Game.Unity.Themes.Experimental
{
    /// <summary>
    /// Hard-clips every child particle system to the local VFX box so nothing crosses
    /// the grid boundary.
    /// </summary>
    public sealed class MosaicParticleCage : MonoBehaviour
    {
        Vector3 _extents;
        ParticleSystem[] _systems;
        ParticleSystem.Particle[] _buffer;

        public Vector3 Extents => _extents;

        public void Initialise(Vector3 extents)
        {
            _extents = extents;
            _systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public int ConfineAll()
        {
            if (_systems == null)
                _systems = GetComponentsInChildren<ParticleSystem>(true);

            var moved = 0;
            for (var s = 0; s < _systems.Length; s++)
                moved += Confine(_systems[s]);
            return moved;
        }

        public bool ContainsLocal(Vector3 local)
        {
            return Mathf.Abs(local.x) <= _extents.x + 0.0001f
                && Mathf.Abs(local.y) <= _extents.y + 0.0001f
                && Mathf.Abs(local.z) <= _extents.z + 0.0001f;
        }

        int Confine(ParticleSystem system)
        {
            if (system == null)
                return 0;

            var count = system.particleCount;
            if (count == 0)
                return 0;

            if (_buffer == null || _buffer.Length < count)
                _buffer = new ParticleSystem.Particle[Mathf.Max(count, 32)];

            var alive = system.GetParticles(_buffer);
            var moved = 0;
            for (var i = 0; i < alive; i++)
            {
                var position = _buffer[i].position;
                var velocity = _buffer[i].velocity;
                var clamped = new Vector3(
                    Mathf.Clamp(position.x, -_extents.x, _extents.x),
                    Mathf.Clamp(position.y, -_extents.y, _extents.y),
                    Mathf.Clamp(position.z, -_extents.z, _extents.z));

                if (clamped != position)
                {
                    Bounce(ref velocity.x, position.x, clamped.x);
                    Bounce(ref velocity.y, position.y, clamped.y);
                    Bounce(ref velocity.z, position.z, clamped.z);
                    _buffer[i].position = clamped;
                    _buffer[i].velocity = velocity;
                    moved++;
                }
            }

            if (moved > 0)
                system.SetParticles(_buffer, alive);
            return moved;
        }

        static void Bounce(ref float speed, float before, float after)
        {
            if (after > before)
                speed = Mathf.Abs(speed);
            else if (after < before)
                speed = -Mathf.Abs(speed);
        }

        void LateUpdate() => ConfineAll();
    }
}
