using Game.Core;
using Game.Unity;
using Nixin.Rail.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(MazeGameHost))]
    public sealed class MazeGameHostEditor : UnityEditor.Editor
    {
        static string[] _schemeNames;
        static string[] _stickNames;
        static string[] _waypointNames;
        static string[] _intermediateNames;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Arena"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("EyeCamera"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Orbit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Hud"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Run"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Memory"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Walker"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Walk", EditorStyles.boldLabel);
            DrawScheme();
            var scheme = (ArenaControlScheme)serializedObject.FindProperty("Scheme").enumValueIndex;
            if (scheme == ArenaControlScheme.RailWaypoint || scheme == ArenaControlScheme.RailJoystick)
                DrawLookStick();

            if (scheme == ArenaControlScheme.RailWaypoint)
            {
                DrawWaypointStyle();
                DrawIntermediateWaypoints();
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("AllowLookDown"),
                    new GUIContent(
                        "Allow Look Down",
                        "When on, you can tilt your head down. When off, look is fixed at Default Look Down."));
                var allowDown = serializedObject.FindProperty("AllowLookDown").boolValue;
                var style = (WaypointStyle)serializedObject.FindProperty("WaypointStyle").enumValueIndex;
                if (allowDown && style == WaypointStyle.FloorTile)
                {
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("MaxLookDown"),
                        new GUIContent(
                            "Max Look Down",
                            "How far you can tilt your head down toward floor chips. 0° is the horizon."));
                }
                else if (!allowDown)
                {
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("DefaultLookDown"),
                        new GUIContent(
                            "Default Look Down",
                            "Fixed look-down angle when Allow Look Down is off. Keeps nearby floor chips in view. 0° is straight ahead."));
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("SwipeToStop"),
                        new GUIContent(
                            "Swipe To Stop",
                            "A fast swipe down while walking parks you at least one cell short of the target so you can look at wall photos. Then tap a chip to walk again. Off while Allow Look Down is on, because that drag already tilts the head."));
                }
                else
                {
                    EditorGUILayout.LabelField("Look stays on the horizon for space blobs.", EditorStyles.miniLabel);
                }

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("RailMoveSpeed"),
                    new GUIContent("Rail Move Speed", "How fast you glide to a tapped cell."));
            }

            if (scheme == ArenaControlScheme.RailJoystick)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("JoystickMoveSpeed"),
                    new GUIContent("Joystick Move Speed", "Slow walk speed along the corridor you face."));
            }

            if (scheme == ArenaControlScheme.RailWaypoint || scheme == ArenaControlScheme.RailJoystick)
            {
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("LookDegreesPerSecond"),
                    new GUIContent("Look Degrees Per Second", "How fast look turns from the stick, drag, or A/D keys."));
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("KeyboardWasd"),
                    new GUIContent(
                        "Keyboard WASD",
                        "Laptop. A/D or arrows look. W/S walks the hall you face if you use them. Tap chips and touch look still work."));
            }

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("LimitLookYaw"),
                new GUIContent(
                    "Limit Look Yaw",
                    "When on, look cannot spin a full circle. It stops at 180° left or right of spawn facing. A fade shows on the screen edge. Turn off to allow a full spin."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Play", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("ShowDebugTools"),
                new GUIContent(
                    "Show Debug Tools",
                    "Shows the sandbox panel, orbit camera, and 2D map in Play. Leave off for player builds."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("UseCampaign"),
                new GUIContent(
                    "Use Campaign",
                    "When on, Play uses the campaign level list (Courtyard, Alley, …). Size and difficulty come from that list. When off, this scene’s MazeArena width, height, difficulty, and seed are used."));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("StartWalking"),
                new GUIContent(
                    "Start Walking",
                    "When on, Play starts in first-person walk. When off, you start in orbit view (you can still walk later from debug tools)."));

            serializedObject.ApplyModifiedProperties();
        }

        void DrawScheme()
        {
            var prop = serializedObject.FindProperty("Scheme");
            prop.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent("Walk Scheme", "How the player moves and looks. Gaze walk and Arrow look are listed but not built yet."),
                prop.enumValueIndex,
                SchemeNames());
            var scheme = (ArenaControlScheme)prop.enumValueIndex;
            EditorGUILayout.LabelField(ArenaControlSchemes.Description(scheme), EditorStyles.wordWrappedMiniLabel);
        }

        void DrawLookStick()
        {
            var prop = serializedObject.FindProperty("LookStick");
            prop.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent("Look Stick", "Fixed bottom: pad always on. Appear on drag: shows where you drag. Hidden drag: same drag, no art."),
                prop.enumValueIndex,
                StickNames());
        }

        void DrawWaypointStyle()
        {
            var prop = serializedObject.FindProperty("WaypointStyle");
            prop.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent("Waypoint Style", "Floor tile: chips on the floor (look down). Space blob: standing pins (look stays level)."),
                prop.enumValueIndex,
                WaypointNames());
        }

        void DrawIntermediateWaypoints()
        {
            var prop = serializedObject.FindProperty("IntermediateWaypoint");
            prop.enumValueIndex = EditorGUILayout.Popup(
                new GUIContent(
                    "Intermediate Waypoints",
                    "Required: tap the next cell. Skip: tap any chip until the next turn. Remove: hide straight-hall chips and only show the far turn."),
                prop.enumValueIndex,
                IntermediateNames());
            var mode = (IntermediateWaypointMode)prop.enumValueIndex;
            EditorGUILayout.LabelField(IntermediateWaypoints.Description(mode), EditorStyles.wordWrappedMiniLabel);
        }

        static string[] IntermediateNames()
        {
            if (_intermediateNames != null)
                return _intermediateNames;
            var all = IntermediateWaypoints.All;
            _intermediateNames = new string[all.Length];
            for (var i = 0; i < all.Length; i++)
                _intermediateNames[i] = IntermediateWaypoints.Name(all[i]);
            return _intermediateNames;
        }

        static string[] SchemeNames()
        {
            if (_schemeNames != null)
                return _schemeNames;
            var all = ArenaControlSchemes.All;
            _schemeNames = new string[all.Length];
            for (var i = 0; i < all.Length; i++)
                _schemeNames[i] = ArenaControlSchemes.Name(all[i]);
            return _schemeNames;
        }

        static string[] StickNames()
        {
            if (_stickNames != null)
                return _stickNames;
            _stickNames = new[]
            {
                LookStickModes.Name(LookStickMode.FixedBottom),
                LookStickModes.Name(LookStickMode.AppearOnDrag),
                LookStickModes.Name(LookStickMode.HiddenDrag)
            };
            return _stickNames;
        }

        static string[] WaypointNames()
        {
            if (_waypointNames != null)
                return _waypointNames;
            _waypointNames = new[] { "Floor tile", "Space blob" };
            return _waypointNames;
        }
    }
}
