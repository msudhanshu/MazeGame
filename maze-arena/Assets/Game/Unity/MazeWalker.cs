using Game.Core;
using Nixin.Locomotion;
using Nixin.Maze;
using Nixin.Rail;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Maze-specific first-person walker. Scheme A uses <see cref="FirstPersonController"/>;
    /// scheme D uses <see cref="RailLocomotion"/>.
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
        RailLocomotion _rail;
        CharacterController _controller;
        ArenaControls _controls;
        Renderer[] _bodyRenderers;
        bool _walking;
        Transform _cameraHome;
        Vector3 _cameraLocalPos;
        Quaternion _cameraLocalRot;

        public ArenaControls Controls
        {
            get
            {
                EnsureRefs();
                return _controls;
            }
        }
        public bool Walking => _walking;

        public const float ControllerRadius = 0.38f;
        public const float ControllerSkinWidth = 0.08f;

        void Awake()
        {
            EnsureRefs();
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
            EnsureRefs();
            if (Arena == null || Arena.Layout == null || Arena.Dimensions == null)
                return;

            var dims = Arena.Dimensions;
            var pose = AvatarSpawn.AtEntry(
                Arena.Layout.Entry,
                dims.CellSize,
                dims.Origin.x,
                dims.Origin.y,
                dims.Origin.z);

            if (_locomotion != null)
                _locomotion.Teleport(new Vector3(pose.X, pose.Y, pose.Z), pose.YawDegrees);
            else
                transform.SetPositionAndRotation(new Vector3(pose.X, pose.Y, pose.Z), Quaternion.Euler(0f, pose.YawDegrees, 0f));

            if (_rail != null)
            {
                var speed = Controls != null ? Controls.RailMoveSpeed : RailTravel.DefaultMoveSpeed;
                _rail.Bind(Arena, speed);
                _rail.PlaceAt(Arena.Layout.Entry.Cell, pose.YawDegrees, pose.Y);
            }

            if (_walking)
                ApplyLocomotion();

            var gate = GetComponent<LookYawGate>() ?? gameObject.AddComponent<LookYawGate>();
            gate.SetCenter(pose.YawDegrees);
        }

        public void SetWalking(bool walking)
        {
            if (walking == _walking)
                return;

            _walking = walking;
            EnsureRefs();

            if (walking)
            {
                AttachCamera();
                ApplyLocomotion();
            }
            else
            {
                DisableLocomotion();
                DetachCamera();
            }
        }

        public void ApplyLocomotion()
        {
            EnsureRefs();
            if (!_walking)
            {
                DisableLocomotion();
                return;
            }

            var yaw = CurrentYaw();
            var rail = Controls != null && ArenaControlSchemes.UsesRail(Controls.ActiveScheme);
            if (rail)
            {
                if (_locomotion != null)
                    _locomotion.SetActive(false);
                if (_rail != null)
                {
                    if (_rail.Travel == null && Arena != null)
                    {
                        var speed = Controls != null ? Controls.RailMoveSpeed : RailTravel.DefaultMoveSpeed;
                        _rail.Bind(Arena, speed);
                    }

                    if (_rail.Travel != null && Arena != null && Arena.Layout != null && Arena.Dimensions != null
                        && MazeGeometry.TryCellFromWorld(transform.position, Arena.Dimensions, Arena.Layout.Size, out var cell))
                    {
                        _rail.PlaceAt(cell, yaw, transform.position.y);
                    }

                    _rail.SetActive(true);
                }
            }
            else
            {
                if (_rail != null)
                    _rail.SetActive(false);
                if (_locomotion != null)
                {
                    _locomotion.Teleport(transform.position, yaw, 0f);
                    _locomotion.SetActive(true);
                }
            }

            if (_controller != null)
                _controller.enabled = !rail;
        }

        void DisableLocomotion()
        {
            if (_locomotion != null)
                _locomotion.SetActive(false);
            if (_rail != null)
                _rail.SetActive(false);
            if (_controller != null)
                _controller.enabled = true;
        }

        void EnsureRefs()
        {
            if (_controller == null)
                _controller = GetComponent<CharacterController>();
            if (_locomotion == null)
                _locomotion = GetComponent<FirstPersonController>();
            if (_controls == null)
                _controls = GetComponent<ArenaControls>() ?? gameObject.AddComponent<ArenaControls>();
            if (GetComponent<YawLookStick>() == null)
                gameObject.AddComponent<YawLookStick>();
            if (GetComponent<FloorWaypoints>() == null)
                gameObject.AddComponent<FloorWaypoints>();
            if (_rail == null)
                _rail = GetComponent<RailLocomotion>() ?? gameObject.AddComponent<RailLocomotion>();
            if (GetComponent<LookYawGate>() == null)
                gameObject.AddComponent<LookYawGate>();
        }

        float CurrentYaw()
        {
            if (_rail != null && _rail.Active)
                return _rail.Yaw;
            if (_locomotion != null && _locomotion.Active)
                return _locomotion.Yaw;
            return transform.eulerAngles.y;
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
                DisableLocomotion();
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
            if (go.GetComponent<ArenaControls>() == null)
                go.AddComponent<ArenaControls>();
            if (go.GetComponent<YawLookStick>() == null)
                go.AddComponent<YawLookStick>();
            if (go.GetComponent<FloorWaypoints>() == null)
                go.AddComponent<FloorWaypoints>();
            if (go.GetComponent<RailLocomotion>() == null)
                go.AddComponent<RailLocomotion>();
            if (go.GetComponent<LookYawGate>() == null)
                go.AddComponent<LookYawGate>();

            var walker = go.GetComponent<MazeWalker>() ?? go.AddComponent<MazeWalker>();
            walker.Arena = arena;
            walker.EyeCamera = camera;
            walker.Orbit = orbit;
            walker.Eye = eye;
            return walker;
        }
    }
}
