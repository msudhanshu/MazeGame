using Game.Core;
using Nixin.Rail.Core;
using UnityEngine;

namespace Game.Unity
{
        /// <summary>
        /// Runtime walk config on the walker. Edit-Mode defaults live on <see cref="MazeGameHost"/>.
        /// </summary>
    public sealed class ArenaControls : MonoBehaviour, IRailSettings
    {
        public ArenaControlScheme Scheme = ArenaControlScheme.RailWaypoint;
        public LookStickMode LookStick = LookStickMode.AppearOnDrag;
        public WaypointStyle WaypointStyle = WaypointStyle.FloorTile;
        [Tooltip("Rail waypoint only. Required: tap the next cell. Skip: tap any chip until the next turn. Remove: hide straight-hall chips and only show the far turn.")]
        public IntermediateWaypointMode IntermediateWaypoint = IntermediateWaypointMode.Skip;
        public float RailMoveSpeed = RailTravel.DefaultMoveSpeed;
        public float JoystickMoveSpeed = RailDrive.DefaultMoveSpeed;
        public float LookDegreesPerSecond = 220f;
        [Tooltip("When on, look cannot spin a full circle. It stops at 180° left or right of spawn facing. A fade shows on the screen edge. Turn off to allow a full spin.")]
        public bool LimitLookYaw = true;
        [Tooltip("Rail waypoint only. When on, you can tilt your head down. When off, look is fixed at Default Look Down.")]
        public bool AllowLookDown = true;
        [Range(0f, 85f)]
        [Tooltip("Rail waypoint only. Fixed look-down angle when Allow Look Down is off. Keeps nearby floor chips in view. 0° is straight ahead.")]
        public float DefaultLookDown = RailLook.DefaultLookDown;
        [Range(5f, 85f)]
        [Tooltip("Rail waypoint only. How far you can tilt your head down toward floor chips. 0° is the horizon.")]
        public float MaxLookDown = RailLook.MaxLookDown;
        [Tooltip("Rail waypoint only, and only when Allow Look Down is off. A fast swipe down while walking parks you at least one cell short of the target so you can look at wall photos, then tap a chip to walk again.")]
        public bool SwipeToStop = true;
        [Tooltip("Laptop. A/D or arrows look. W/S walks the hall you face if you use them. Tap chips and touch look still work.")]
        public bool KeyboardWasd = true;

        public ArenaControlScheme ActiveScheme => ArenaControlSchemes.Resolve(Scheme);
        public bool DriveWithJoystick => ActiveScheme == ArenaControlScheme.RailJoystick;

        LookStickMode IRailSettings.LookStick => LookStick;
        WaypointStyle IRailSettings.WaypointStyle => WaypointStyle;
        IntermediateWaypointMode IRailSettings.IntermediateWaypoint => IntermediateWaypoint;
        float IRailSettings.RailMoveSpeed => RailMoveSpeed;
        float IRailSettings.JoystickMoveSpeed => JoystickMoveSpeed;
        float IRailSettings.LookDegreesPerSecond => LookDegreesPerSecond;
        bool IRailSettings.AllowLookDown => AllowLookDown;
        float IRailSettings.DefaultLookDown => DefaultLookDown;
        float IRailSettings.MaxLookDown => MaxLookDown;
        bool IRailSettings.SwipeToStop => SwipeToStop;
        bool IRailSettings.KeyboardWasd => KeyboardWasd;
    }
}
