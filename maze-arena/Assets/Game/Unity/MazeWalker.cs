using Game.Core;
using Nixin.Locomotion;
using Nixin.Maze;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Maze-specific first-person walker. Locomotion comes from <see cref="FirstPersonController"/>.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class MazeWalker : MonoBehaviour
    {
        public MazeArena Arena;
        public Camera EyeCamera;
        public OrbitCameraRig Orbit;
        public Transform Eye;

        FirstPersonController _locomotion;
        Renderer[] _bodyRenderers;
        bool _walking;
        Transform _cameraHome;
        Vector3 _cameraLocalPos;
        Quaternion _cameraLocalRot;

        public bool Walking => _walking;

        public const float ControllerRadius = 0.38f;
        public const float ControllerSkinWidth = 0.08f;

        void Awake()
        {
            _locomotion = GetComponent<FirstPersonController>();
            _bodyRenderers = GetComponentsInChildren<Renderer>(true);
            if (Eye == null)
            {
                var eyeGo = transform.Find("Eye");
                Eye = eyeGo != null ? eyeGo : transform;
            }

            if (_locomotion != null)
                _locomotion.Eye = Eye;
        }

        public void PlaceAtEntry()
        {
            if (_locomotion == null)
                _locomotion = GetComponent<FirstPersonController>();
            if (Arena == null || Arena.Layout == null || Arena.Dimensions == null || _locomotion == null)
                return;

            var dims = Arena.Dimensions;
            var pose = AvatarSpawn.AtEntry(
                Arena.Layout.Entry,
                dims.CellSize,
                dims.Origin.x,
                dims.Origin.y,
                dims.Origin.z);

            _locomotion.Teleport(new Vector3(pose.X, pose.Y, pose.Z), pose.YawDegrees);
        }

        public void SetWalking(bool walking)
        {
            if (walking == _walking)
                return;

            _walking = walking;
            if (_locomotion == null)
                _locomotion = GetComponent<FirstPersonController>();

            if (_locomotion != null)
                _locomotion.SetActive(walking);

            if (walking)
                AttachCamera();
            else
                DetachCamera();
        }

        void AttachCamera()
        {
            if (EyeCamera == null)
                return;

            var camTransform = EyeCamera.transform;
            _cameraHome = camTransform.parent;
            _cameraLocalPos = camTransform.localPosition;
            _cameraLocalRot = camTransform.localRotation;
            if (Orbit != null)
                Orbit.enabled = false;

            camTransform.SetParent(Eye != null ? Eye : transform, false);
            camTransform.localPosition = Vector3.zero;
            camTransform.localRotation = Quaternion.identity;
            SetBodyVisible(false);
        }

        void DetachCamera()
        {
            if (EyeCamera != null)
            {
                EyeCamera.transform.SetParent(_cameraHome, false);
                EyeCamera.transform.localPosition = _cameraLocalPos;
                EyeCamera.transform.localRotation = _cameraLocalRot;
            }

            if (Orbit != null)
                Orbit.enabled = true;
            SetBodyVisible(true);
        }

        void OnDisable()
        {
            if (_walking)
            {
                _walking = false;
                if (_locomotion != null)
                    _locomotion.SetActive(false);
                DetachCamera();
            }
        }

        void SetBodyVisible(bool visible)
        {
            if (_bodyRenderers == null)
                return;
            for (var i = 0; i < _bodyRenderers.Length; i++)
            {
                if (_bodyRenderers[i] != null)
                    _bodyRenderers[i].enabled = visible;
            }
        }

        public static MazeWalker Create(MazeArena arena, Camera camera, OrbitCameraRig orbit)
        {
            var prefab = Resources.Load<GameObject>("PlayerAvatar");
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
                go.name = "PlayerAvatar";
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = "PlayerAvatar";
                Object.DestroyImmediate(go.GetComponent<Collider>());
                var wall = Resources.Load<GameObject>("NixinMaze/Wall");
                var mesh = wall != null ? wall.transform.Find("Mesh") : null;
                var src = mesh != null ? mesh.GetComponent<Renderer>() : null;
                var dst = go.GetComponent<Renderer>();
                if (src != null && dst != null)
                    dst.sharedMaterial = src.sharedMaterial;
            }

            var controller = go.GetComponent<CharacterController>() ?? go.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = ControllerRadius;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.skinWidth = ControllerSkinWidth;
            controller.minMoveDistance = 0.001f;

            var eye = go.transform.Find("Eye");
            if (eye == null)
            {
                var eyeGo = new GameObject("Eye");
                eyeGo.transform.SetParent(go.transform, false);
                eyeGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
                eye = eyeGo.transform;
            }

            if (go.GetComponent<FirstPersonController>() == null)
                go.AddComponent<FirstPersonController>();

            var walker = go.GetComponent<MazeWalker>() ?? go.AddComponent<MazeWalker>();
            walker.Arena = arena;
            walker.EyeCamera = camera;
            walker.Orbit = orbit;
            walker.Eye = eye;
            return walker;
        }
    }
}
