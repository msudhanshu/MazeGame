namespace Game.Core
{
    /// <summary>
    /// Walk schemes. B and C are named in the UI but not implemented yet.
    /// </summary>
    public enum ArenaControlScheme
    {
        DualSticks = 0,
        GazeWalk = 1,
        ArrowLook = 2,
        RailWaypoint = 3,
        RailJoystick = 4
    }

    public static class ArenaControlSchemes
    {
        public static readonly ArenaControlScheme[] All =
        {
            ArenaControlScheme.DualSticks,
            ArenaControlScheme.GazeWalk,
            ArenaControlScheme.ArrowLook,
            ArenaControlScheme.RailWaypoint,
            ArenaControlScheme.RailJoystick
        };

        public static bool IsImplemented(ArenaControlScheme scheme)
        {
            return scheme == ArenaControlScheme.DualSticks
                   || scheme == ArenaControlScheme.RailWaypoint
                   || scheme == ArenaControlScheme.RailJoystick;
        }

        public static bool UsesRail(ArenaControlScheme scheme)
        {
            var resolved = Resolve(scheme);
            return resolved == ArenaControlScheme.RailWaypoint
                   || resolved == ArenaControlScheme.RailJoystick;
        }

        public static ArenaControlScheme Resolve(ArenaControlScheme requested)
        {
            return IsImplemented(requested) ? requested : ArenaControlScheme.RailWaypoint;
        }

        public static string Name(ArenaControlScheme scheme)
        {
            switch (scheme)
            {
                case ArenaControlScheme.DualSticks:
                    return "Dual sticks";
                case ArenaControlScheme.GazeWalk:
                    return "Gaze walk";
                case ArenaControlScheme.ArrowLook:
                    return "Arrow look";
                case ArenaControlScheme.RailWaypoint:
                    return "Rail waypoint";
                case ArenaControlScheme.RailJoystick:
                    return "Rail joystick";
                default:
                    return scheme.ToString();
            }
        }

        public static string Description(ArenaControlScheme scheme)
        {
            switch (scheme)
            {
                case ArenaControlScheme.DualSticks:
                    return "Free walk. Phone: left stick moves, right stick looks. Laptop: WASD and mouse.";
                case ArenaControlScheme.GazeWalk:
                    return "Not built yet. Would walk the way you look. Play uses Rail waypoint instead.";
                case ArenaControlScheme.ArrowLook:
                    return "Not built yet. Would walk the way you look, with look buttons. Play uses Rail waypoint instead.";
                case ArenaControlScheme.RailWaypoint:
                    return "On the corridor groove. Tap a chip to glide there; Skip lets you jump a straight hall. Look is separate from walking. Laptop: A/D looks, W/S walks if you use them.";
                case ArenaControlScheme.RailJoystick:
                    return "On the corridor groove. Left/right looks; up/down walks the hall you face. Laptop: A/D looks, W/S walks.";
                default:
                    return "";
            }
        }
    }
}
