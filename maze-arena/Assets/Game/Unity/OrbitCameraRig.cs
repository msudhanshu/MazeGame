using UnityEngine;

namespace Game.Unity
{
    public sealed class OrbitCameraRig : MonoBehaviour
    {
        public Transform Target;
        public float Distance = 28f;
        public float MinDistance = 8f;
        public float MaxDistance = 80f;
        public float Yaw = 40f;
        public float Pitch = 55f;
        public float RotateSpeed = 120f;
        public float ZoomSpeed = 8f;

        void LateUpdate()
        {
            if (Input.GetMouseButton(1))
            {
                Yaw += Input.GetAxis("Mouse X") * RotateSpeed * Time.deltaTime;
                Pitch -= Input.GetAxis("Mouse Y") * RotateSpeed * Time.deltaTime;
                Pitch = Mathf.Clamp(Pitch, 15f, 85f);
            }

            Distance = Mathf.Clamp(Distance - Input.GetAxis("Mouse ScrollWheel") * ZoomSpeed * 10f, MinDistance, MaxDistance);

            var lookAt = Target != null ? Target.position : Vector3.zero;
            var rotation = Quaternion.Euler(Pitch, Yaw, 0f);
            transform.position = lookAt + rotation * new Vector3(0f, 0f, -Distance);
            transform.LookAt(lookAt);
        }

        public void Frame(Vector3 center, float size)
        {
            if (Target == null)
            {
                var pivot = new GameObject("OrbitPivot");
                Target = pivot.transform;
            }

            Target.position = center;
            Distance = Mathf.Clamp(size * 1.6f, MinDistance, MaxDistance);
        }
    }
}
