using Game.Core;
using Nixin.Grid.Core;
using Nixin.Maze;
using Nixin.Rail;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>
    /// Maze binder for <see cref="RailMotor"/>: builds a <see cref="CorridorRail"/> from the arena
    /// and keeps floor chips on grid cells.
    /// </summary>
    public sealed class RailLocomotion : MonoBehaviour
    {
        MazeWalker _walker;
        ArenaControls _controls;
        YawLookStick _look;
        FloorWaypoints _waypoints;
        RailMotor _motor;
        CorridorRail _corridor;

        public RailTravel Travel => _motor != null ? _motor.Travel : null;
        public bool Active => _motor != null && _motor.Active;
        public float Yaw => _motor != null ? _motor.Yaw : 0f;
        public CorridorRail Corridor => _corridor;

        public void SetYaw(float yawDegrees)
        {
            if (_motor != null)
                _motor.SetYaw(yawDegrees);
        }

        void Awake()
        {
            BindComponents();
        }

        void BindComponents()
        {
            if (_walker == null)
                _walker = GetComponent<MazeWalker>();
            if (_controls == null)
                _controls = GetComponent<ArenaControls>();
            if (_look == null)
                _look = GetComponent<YawLookStick>();
            if (_waypoints == null)
                _waypoints = GetComponent<FloorWaypoints>();
            if (_motor == null)
                _motor = GetComponent<RailMotor>() ?? gameObject.AddComponent<RailMotor>();

            _motor.FollowRailHeight = false;
            if (_walker != null)
            {
                _motor.Eye = _walker.Eye;
                _motor.EyeCamera = _walker.EyeCamera;
            }
        }

        public void Bind(MazeArena arena, float moveSpeed)
        {
            BindComponents();
            if (arena == null || arena.Layout == null || arena.Dimensions == null)
            {
                _corridor = null;
                if (_motor != null)
                    _motor.Bind(null, _controls, _look, _waypoints, moveSpeed);
                return;
            }

            var dims = arena.Dimensions;
            _corridor = new CorridorRail(
                arena.Layout.Grid,
                dims.CellSize,
                dims.Origin.x,
                dims.Origin.z,
                dims.Origin.y);
            if (_waypoints != null)
                _waypoints.Arena = arena;
            _motor.Bind(_corridor, _controls, _look, _waypoints, moveSpeed);
        }

        public void PlaceAt(GridCoord cell, float yawDegrees, float y)
        {
            BindComponents();
            if (_motor == null || _corridor == null)
                return;
            if (_walker != null)
            {
                _motor.Eye = _walker.Eye;
                _motor.EyeCamera = _walker.EyeCamera;
            }

            _motor.PlaceAt(_corridor.Id(cell), yawDegrees, y);
        }

        public void SetActive(bool active)
        {
            BindComponents();
            if (_motor != null)
                _motor.SetActive(active);
        }
    }
}
